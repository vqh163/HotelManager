using Microsoft.AspNetCore.Authorization;
using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.ThuNgan
{
    [Authorize(Roles = "Thu ngân Nhà hàng, Quản lý")]
    public class LichSuHoaDonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IActionDescriptorCollectionProvider _actionProvider;

        public LichSuHoaDonModel(ApplicationDbContext context, IActionDescriptorCollectionProvider actionProvider)
        {
            _context = context;
            _actionProvider = actionProvider;
        }

        // Thuộc tính lọc & tìm kiếm lịch sử hóa đơn
        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; } = "Tất cả";

        [BindProperty(SupportsGet = true)]
        public DateTime? NgayLoc { get; set; }

        // Phân quyền hiển thị
        public bool LaQuanLy { get; set; } = false;
        public bool LaThuNgan { get; set; } = false;
        public string TenNguoiDungHienTai { get; set; } = string.Empty;

        public List<HoaDonViewModel> DanhSachLichSu { get; set; } = new List<HoaDonViewModel>();
        public List<LichSuThaoTacItem> LichSuThaoTac { get; set; } = new List<LichSuThaoTacItem>();

        private string TimDuongDanTrang(string tenTrang, string duongDanMacDinh)
        {
            var page = _actionProvider.ActionDescriptors.Items
                .OfType<PageActionDescriptor>()
                .FirstOrDefault(p => p.ViewEnginePath.EndsWith("/" + tenTrang, StringComparison.OrdinalIgnoreCase)
                                  || p.ViewEnginePath.Equals("/" + tenTrang, StringComparison.OrdinalIgnoreCase));
            return page != null ? page.ViewEnginePath : duongDanMacDinh;
        }

        // Đọc thông tin VaiTro thực tế từ bảng TAI_KHOAN bằng LINQ
        private async Task<(string vaiTro, string hoTen)> LayThongTinNguoiDungAsync()
        {
            string tenDangNhap = User?.Identity?.Name
                              ?? User?.FindFirst(ClaimTypes.Name)?.Value
                              ?? User?.FindFirst("TenDangNhap")?.Value
                              ?? string.Empty;

            string maTaiKhoan = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User?.FindFirst("MaTaiKhoan")?.Value
                             ?? string.Empty;

            if (string.IsNullOrEmpty(tenDangNhap))
            {
                try { tenDangNhap = HttpContext.Session.GetString("TenDangNhap") ?? string.Empty; } catch { }
            }

            string vaiTro = string.Empty;
            string hoTen = tenDangNhap;

            if (!string.IsNullOrEmpty(tenDangNhap) || !string.IsNullOrEmpty(maTaiKhoan))
            {
                var tkDb = await _context.TAI_KHOAN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap || t.MaTaiKhoan == maTaiKhoan);

                if (tkDb != null)
                {
                    vaiTro = (tkDb.VaiTro ?? string.Empty).Trim();
                    hoTen = tkDb.TenDangNhap ?? hoTen;
                }
            }

            if (string.IsNullOrEmpty(vaiTro))
            {
                vaiTro = User?.FindFirst(ClaimTypes.Role)?.Value
                      ?? User?.FindFirst("VaiTro")?.Value
                      ?? string.Empty;
            }

            return (vaiTro.Trim(), string.IsNullOrEmpty(hoTen) ? "Nhân viên" : hoTen.Trim());
        }

        private string GetMaBanOfHoaDon(HOA_DON_NHA_HANG hd)
        {
            string maBanCuaHd = "";
            var propMaBan = hd.GetType().GetProperty("MaBan") ?? hd.GetType().GetProperty("BAN_NHA_HANGMaBan");
            if (propMaBan != null) maBanCuaHd = propMaBan.GetValue(hd)?.ToString() ?? "";
            if (string.IsNullOrEmpty(maBanCuaHd))
            {
                try { maBanCuaHd = _context.Entry(hd).Property("MaBan").CurrentValue?.ToString() ?? ""; } catch { }
            }
            if (string.IsNullOrEmpty(maBanCuaHd))
            {
                try { maBanCuaHd = _context.Entry(hd).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? ""; } catch { }
            }
            return maBanCuaHd.Trim();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (string.IsNullOrEmpty(vaiTro)) return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));

            LaQuanLy = vaiTro.Contains("Quản lý", StringComparison.OrdinalIgnoreCase) ||
                       vaiTro.Contains("QuanLy", StringComparison.OrdinalIgnoreCase) ||
                       vaiTro.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                       vaiTro.Contains("Giám đốc", StringComparison.OrdinalIgnoreCase);

            LaThuNgan = !LaQuanLy && (vaiTro.Contains("Thu ngân", StringComparison.OrdinalIgnoreCase) ||
                                      vaiTro.Contains("ThuNgan", StringComparison.OrdinalIgnoreCase) ||
                                      vaiTro.Contains("Kế toán", StringComparison.OrdinalIgnoreCase));

            if (!LaQuanLy && !LaThuNgan) return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));

            TenNguoiDungHienTai = hoTen;

            try
            {
                ThuNganDataStore.Load();
                LichSuThaoTac = ThuNganDataStore.Logs.Take(50).ToList();

                // Chỉ truy vấn các hóa đơn đã hoàn tất hoặc đã hủy để hiển thị trong Lịch sử
                var query = _context.HOA_DON_NHA_HANG
                    .Where(h => h.HinhThucThanhToan == "Đã Thanh Toán"
                             || h.HinhThucThanhToan == "Ghi nợ phòng"
                             || h.HinhThucThanhToan == "Đã Hủy");

                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả")
                {
                    query = query.Where(h => h.HinhThucThanhToan == TrangThaiLoc);
                }

                if (NgayLoc.HasValue)
                {
                    var ngayChon = NgayLoc.Value.Date;
                    query = query.Where(h => h.ThoiGianTao.HasValue && h.ThoiGianTao.Value.Date == ngayChon);
                }

                var dsHoaDonDb = await query.OrderByDescending(h => h.ThoiGianTao).ToListAsync();
                var dsBanDb = await _context.BAN_NHA_HANG.AsNoTracking().ToListAsync();
                var dsNhanVienDb = await _context.NHAN_VIEN.AsNoTracking().ToListAsync();

                int stt = 1;
                foreach (var hd in dsHoaDonDb)
                {
                    string maBanCuaHd = GetMaBanOfHoaDon(hd);
                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBanTuongUng = dsBanDb.Where(b => danhSachMaBan.Contains(b.MaBan)).ToList();

                    string soBanHienThi = cacBanTuongUng.Any()
                        ? string.Join(", ", cacBanTuongUng.Select(b =>
                        {
                            string sb = (b.TenBan ?? "").Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                            if (sb.Length == 1 && char.IsDigit(sb[0])) sb = "0" + sb;
                            return sb;
                        }))
                        : (string.IsNullOrEmpty(maBanCuaHd) ? "--" : maBanCuaHd);

                    // Lọc theo từ khóa (Mã hóa đơn hoặc Số bàn)
                    if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                    {
                        string kw = TuKhoaTimKiem.Trim();
                        bool matchMa = (hd.MaHoaDonNhaHang ?? "").Contains(kw, StringComparison.OrdinalIgnoreCase);
                        bool matchBan = soBanHienThi.Contains(kw, StringComparison.OrdinalIgnoreCase);
                        if (!matchMa && !matchBan) continue;
                    }

                    string tenNvLap = "Phục vụ NH";
                    try
                    {
                        var propNv = hd.GetType().GetProperty("MaNV") ?? hd.GetType().GetProperty("NHAN_VIENMaNV");
                        string maNv = propNv?.GetValue(hd)?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(maNv))
                        {
                            var nv = dsNhanVienDb.FirstOrDefault(n => n.MaNV == maNv);
                            if (nv != null && !string.IsNullOrEmpty(nv.HoTen)) tenNvLap = nv.HoTen;
                        }
                    }
                    catch { }

                    string jsonMonAn = "[]";
                    try
                    {
                        var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) jsonMonAn = propJson.GetValue(hd)?.ToString() ?? "[]";
                        else jsonMonAn = _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";
                    }
                    catch { }

                    var dsMonAn = new List<ChiTietMonViewModel>();
                    try
                    {
                        if (!string.IsNullOrEmpty(jsonMonAn) && jsonMonAn != "[]")
                        {
                            using var doc = JsonDocument.Parse(jsonMonAn);
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                string tenMon = element.TryGetProperty("TenMon", out var t2) ? (t2.GetString() ?? "")
                                              : (element.TryGetProperty("tenDichVu", out var t1) ? (t1.GetString() ?? "") : "Món ăn");
                                int soLuong = element.TryGetProperty("SoLuong", out var s2) ? s2.GetInt32()
                                            : (element.TryGetProperty("soLuong", out var s1) ? s1.GetInt32() : 1);
                                decimal donGia = element.TryGetProperty("DonGia", out var d2) ? d2.GetDecimal()
                                               : (element.TryGetProperty("donGia", out var d1) ? d1.GetDecimal() : 0);
                                string ghiChu = element.TryGetProperty("GhiChu", out var g2) ? (g2.GetString() ?? "")
                                              : (element.TryGetProperty("ghiChu", out var g1) ? (g1.GetString() ?? "") : "");

                                dsMonAn.Add(new ChiTietMonViewModel
                                {
                                    TenMon = string.IsNullOrEmpty(tenMon) ? "Món ăn" : tenMon,
                                    SoLuong = soLuong,
                                    DonGia = donGia,
                                    ThanhTien = soLuong * donGia,
                                    GhiChu = ghiChu
                                });
                            }
                        }
                    }
                    catch { }

                    decimal tamTinh = dsMonAn.Sum(x => x.ThanhTien);
                    if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0;
                    decimal phiPhucVu = tamTinh * 0.05m;
                    decimal thueVAT = tamTinh * 0.10m;
                    decimal giamGia = hd.GiamGia ?? 0;
                    decimal tongCong = Math.Max(0, tamTinh + phiPhucVu + thueVAT - giamGia);

                    DanhSachLichSu.Add(new HoaDonViewModel
                    {
                        STT = stt < 10 ? "0" + stt : stt.ToString(),
                        MaHoaDonNhaHang = hd.MaHoaDonNhaHang ?? "",
                        MaBan = maBanCuaHd,
                        TenBan = $"Bàn {soBanHienThi}",
                        HinhThucThanhToan = hd.HinhThucThanhToan ?? "Đã Thanh Toán",
                        DanhSachMonJson = Uri.EscapeDataString(JsonSerializer.Serialize(dsMonAn)),
                        TamTinh = tamTinh,
                        PhiPhucVu = phiPhucVu,
                        ThueVAT = thueVAT,
                        GiamGia = giamGia,
                        TongCong = tongCong,
                        GioTao = (hd.ThoiGianTao ?? DateTime.Now).ToString("HH:mm dd/MM/yyyy"),
                        NhanVienLap = tenNvLap,
                        LaHoaDonGop = danhSachMaBan.Count > 1
                    });
                    stt++;
                }

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải lịch sử hóa đơn: " + ex.Message);
                return Page();
            }
        }
    }
}