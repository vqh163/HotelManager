using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.ThuNgan
{
    [Authorize(Roles = "Thu ngân Nhà hàng,Quản lý,Giám đốc")]
    public class LichSuHoaDonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LichSuHoaDonModel> _logger;

        public LichSuHoaDonModel(ApplicationDbContext context, ILogger<LichSuHoaDonModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; } = "Tất cả";

        [BindProperty(SupportsGet = true)]
        public DateTime? NgayLoc { get; set; }

        public bool LaQuanLy { get; set; } = false;
        public bool LaThuNgan { get; set; } = false;
        public string TenNguoiDungHienTai { get; set; } = string.Empty;

        public List<HoaDonViewModel> DanhSachLichSu { get; set; } = new List<HoaDonViewModel>();
        public List<LichSuThaoTacItem> LichSuThaoTac { get; set; } = new List<LichSuThaoTacItem>();

        public async Task<IActionResult> OnGetAsync()
        {
            LaQuanLy = User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);
            LaThuNgan = !LaQuanLy && (User.IsInRole(HangSoHeThong.VaiTroThuNgan) || User.IsInRole(HangSoHeThong.VaiTroKeToan));
            TenNguoiDungHienTai = User.Identity?.Name ?? "Nhân viên";

            if (!LaQuanLy && !LaThuNgan) return RedirectToPage("/Auth/DangNhap");

            try
            {
                ThuNganDataStore.Load();
                LichSuThaoTac = ThuNganDataStore.Logs.Take(50).ToList();

                // Chỉ truy vấn các hóa đơn đã hoàn tất, ghi nợ phòng hoặc đã hủy bằng LINQ
                var query = _context.HOA_DON_NHA_HANG.AsNoTracking()
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
                    string maBanCuaHd = (hd.BAN_NHA_HANGMaBan ?? string.Empty).Trim();
                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBanTuongUng = dsBanDb.Where(b => danhSachMaBan.Contains(b.MaBan)).ToList();

                    string soBanHienThi = cacBanTuongUng.Any()
                        ? string.Join(", ", cacBanTuongUng.Select(b =>
                        {
                            string sb = (b.TenBan ?? b.MaBan).Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                            if (sb.Length == 1 && char.IsDigit(sb[0])) sb = "0" + sb;
                            return sb;
                        }))
                        : (string.IsNullOrEmpty(maBanCuaHd) ? "--" : maBanCuaHd);

                    if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                    {
                        string kw = TuKhoaTimKiem.Trim();
                        bool matchMa = (hd.MaHoaDonNhaHang ?? string.Empty).Contains(kw, StringComparison.OrdinalIgnoreCase);
                        bool matchBan = soBanHienThi.Contains(kw, StringComparison.OrdinalIgnoreCase);
                        if (!matchMa && !matchBan) continue;
                    }

                    string tenNvLap = "Phục vụ NH";
                    if (!string.IsNullOrWhiteSpace(hd.NHAN_VIENMaNV))
                    {
                        var nv = dsNhanVienDb.FirstOrDefault(n => n.MaNV == hd.NHAN_VIENMaNV);
                        if (nv != null && !string.IsNullOrWhiteSpace(nv.HoTen)) tenNvLap = nv.HoTen;
                    }

                    var dsMonAn = new List<ChiTietMonViewModel>();
                    string jsonMonAn = hd.DanhSachMonJson ?? "[]";
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(jsonMonAn) && jsonMonAn != "[]")
                        {
                            using var doc = JsonDocument.Parse(jsonMonAn);
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                string tenMon = element.TryGetProperty("TenMon", out var t2) ? (t2.GetString() ?? "")
                                              : (element.TryGetProperty("tenDichVu", out var t1) ? (t1.GetString() ?? "") : "Món ăn");
                                int soLuong = element.TryGetProperty("SoLuong", out var s2) ? s2.GetInt32()
                                            : (element.TryGetProperty("soLuong", out var s1) ? s1.GetInt32() : 1);
                                decimal donGia = element.TryGetProperty("DonGia", out var d2) ? d2.GetDecimal()
                                               : (element.TryGetProperty("donGia", out var d1) ? d1.GetDecimal() : 0m);
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
                    if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0m;
                    decimal phiPhucVu = Math.Round(tamTinh * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                    decimal thueVAT = Math.Round(tamTinh * HangSoHeThong.TyLeThueVAT, 0);
                    decimal giamGia = hd.GiamGia ?? 0m;
                    decimal tongCong = Math.Max(0, tamTinh + phiPhucVu + thueVAT - giamGia);

                    DanhSachLichSu.Add(new HoaDonViewModel
                    {
                        STT = stt < 10 ? "0" + stt : stt.ToString(),
                        MaHoaDonNhaHang = hd.MaHoaDonNhaHang ?? string.Empty,
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
                _logger.LogError(ex, "Lỗi tải lịch sử hóa đơn nhà hàng.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi tải lịch sử hóa đơn.");
                return Page();
            }
        }
    }
}
