using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class ChiTietMonViewModel
    {
        public string TenMon { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class HoaDonViewModel
    {
        public string STT { get; set; } = string.Empty;
        public string MaHoaDonNhaHang { get; set; } = string.Empty;
        public string MaBan { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;
        public string HinhThucThanhToan { get; set; } = string.Empty;
        public string DanhSachMonJson { get; set; } = string.Empty;
        public decimal TamTinh { get; set; }
        public decimal PhiPhucVu { get; set; }
        public decimal ThueVAT { get; set; }
        public decimal TongCong { get; set; }
    }

    // Bắt buộc đăng nhập; việc kiểm tra phân quyền chi tiết được xử lý tầng bên trong để báo lỗi thân thiện
    [Authorize]
    public class QuayThuNganModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuayThuNganModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Bộ lọc trạng thái hóa đơn tại Quầy thu ngân
        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        // Biến kiểm tra quyền Quản lý để ẩn/hiện các chức năng đặc thù trên giao diện nếu cần
        public bool LaQuanLy { get; set; } = false;

        public List<HoaDonViewModel> DanhSachHoaDon { get; set; } = new List<HoaDonViewModel>();

        // Truy vấn trực tiếp VaiTro từ Cookie Claims và đối chiếu bảng TAI_KHOAN trong CSDL bằng LINQ
        private async Task<string> LayVaiTroThucTeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return string.Empty;

            string tenDangNhap = User.Identity?.Name ?? string.Empty;
            string maTaiKhoan = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            if (!string.IsNullOrEmpty(tenDangNhap) || !string.IsNullOrEmpty(maTaiKhoan))
            {
                var tkDb = await _context.TAI_KHOAN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap || t.MaTaiKhoan == maTaiKhoan);

                if (tkDb != null && !string.IsNullOrEmpty(tkDb.VaiTro))
                {
                    return tkDb.VaiTro.Trim();
                }
            }

            string vaiTroClaim = User.FindFirst(ClaimTypes.Role)?.Value
                                  ?? User.FindFirst("VaiTro")?.Value
                                  ?? string.Empty;

            return vaiTroClaim.Trim();
        }

        // Kiểm tra chính xác quyền Quản lý
        private async Task<bool> KiemTraQuyenQuanLyAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return false;

            string vaiTro = await LayVaiTroThucTeAsync();

            if (vaiTro.Contains("Phục vụ", StringComparison.OrdinalIgnoreCase) ||
                vaiTro.Contains("PhucVu", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return vaiTro.Equals("Quản lý", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Equals("QuanLy", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Equals("Giám đốc", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Equals("Quản trị viên hệ thống", StringComparison.OrdinalIgnoreCase) ||
                   User.IsInRole("Quản lý") ||
                   User.IsInRole("QuanLy") ||
                   User.IsInRole("Admin");
        }

        // Kiểm tra quyền truy cập Quầy Thu Ngân (Chỉ cho phép Quản lý hoặc Thu ngân)
        private async Task<bool> KiemTraQuyenTruyCapThuNganAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return false;

            string vaiTro = await LayVaiTroThucTeAsync();
            if (string.IsNullOrEmpty(vaiTro)) return false;

            return await KiemTraQuyenQuanLyAsync() ||
                   vaiTro.Contains("Thu ngân", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("ThuNgan", StringComparison.OrdinalIgnoreCase) ||
                   User.IsInRole("Thu ngân") ||
                   User.IsInRole("ThuNgan");
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
            if (User?.Identity?.IsAuthenticated != true)
                return RedirectToPage("/TaiKhoan/DangNhap");

            // Kiểm tra phân quyền: Nếu không phải Quản lý hoặc Thu ngân -> Chặn truy cập
            if (!await KiemTraQuyenTruyCapThuNganAsync())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Tài khoản của bạn không có quyền truy cập Quầy thu ngân!";
                return RedirectToPage("/NhaHang/PhucVu/SoDoBan");
            }

            try
            {
                LaQuanLy = await KiemTraQuyenQuanLyAsync();

                var dsHoaDonDb = await _context.HOA_DON_NHA_HANG.OrderByDescending(h => h.ThoiGianTao).ToListAsync();
                var dsBanDb = await _context.BAN_NHA_HANG.ToListAsync();

                // Lọc theo trạng thái hóa đơn nếu Thu ngân chọn bộ lọc
                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả")
                {
                    dsHoaDonDb = dsHoaDonDb.Where(h => (h.HinhThucThanhToan ?? "Chưa thanh toán") == TrangThaiLoc).ToList();
                }

                int stt = 1;
                foreach (var hd in dsHoaDonDb)
                {
                    string maBanCuaHd = GetMaBanOfHoaDon(hd);
                    if (string.IsNullOrEmpty(maBanCuaHd)) continue;

                    // Hỗ trợ hiển thị trường hợp hóa đơn đã gộp từ nhiều bàn (VD: "B01,B02")
                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBanTuongUng = dsBanDb.Where(b => danhSachMaBan.Contains(b.MaBan)).ToList();
                    if (!cacBanTuongUng.Any()) continue;

                    var danhSachSoBan = cacBanTuongUng.Select(b =>
                    {
                        string sb = (b.TenBan ?? "").Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                        if (sb.Length == 1 && char.IsDigit(sb[0])) sb = "0" + sb;
                        return sb;
                    }).ToList();

                    string soBanHienThi = string.Join(", ", danhSachSoBan);
                    string sttFormat = stt < 10 ? "0" + stt : stt.ToString();
                    stt++;

                    string jsonMonAn = "[]";
                    try
                    {
                        var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) jsonMonAn = propJson.GetValue(hd)?.ToString() ?? "[]";
                        else jsonMonAn = _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";
                    }
                    catch { }

                    var dsMonAnThuNgan = new List<ChiTietMonViewModel>();
                    try
                    {
                        if (!string.IsNullOrEmpty(jsonMonAn) && jsonMonAn != "[]")
                        {
                            using (var doc = JsonDocument.Parse(jsonMonAn))
                            {
                                foreach (var element in doc.RootElement.EnumerateArray())
                                {
                                    string tenMon = "";
                                    if (element.TryGetProperty("tenDichVu", out var t1)) tenMon = t1.GetString() ?? "";
                                    else if (element.TryGetProperty("TenMon", out var t2)) tenMon = t2.GetString() ?? "";

                                    int soLuong = 1;
                                    if (element.TryGetProperty("soLuong", out var s1)) soLuong = s1.GetInt32();
                                    else if (element.TryGetProperty("SoLuong", out var s2)) soLuong = s2.GetInt32();

                                    decimal donGia = 0;
                                    if (element.TryGetProperty("donGia", out var d1)) donGia = d1.GetDecimal();
                                    else if (element.TryGetProperty("DonGia", out var d2)) donGia = d2.GetDecimal();

                                    string ghiChu = "";
                                    if (element.TryGetProperty("ghiChu", out var g1)) ghiChu = g1.GetString() ?? "";
                                    else if (element.TryGetProperty("GhiChu", out var g2)) ghiChu = g2.GetString() ?? "";

                                    dsMonAnThuNgan.Add(new ChiTietMonViewModel
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
                    }
                    catch { }

                    string finalJson = Uri.EscapeDataString(JsonSerializer.Serialize(dsMonAnThuNgan));

                    decimal tamTinh = dsMonAnThuNgan.Sum(x => x.ThanhTien);
                    if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0;

                    decimal phiPhucVu = tamTinh * 0.05m;
                    decimal thueVAT = tamTinh * 0.10m;
                    decimal tongCong = tamTinh + phiPhucVu + thueVAT - (hd.GiamGia ?? 0);
                    if (tongCong < 0) tongCong = 0;

                    DanhSachHoaDon.Add(new HoaDonViewModel
                    {
                        STT = sttFormat,
                        MaHoaDonNhaHang = hd.MaHoaDonNhaHang ?? "",
                        MaBan = maBanCuaHd,
                        TenBan = $"Bàn: {soBanHienThi}",
                        HinhThucThanhToan = hd.HinhThucThanhToan ?? "Chưa thanh toán",
                        DanhSachMonJson = finalJson,
                        TamTinh = tamTinh,
                        PhiPhucVu = phiPhucVu,
                        ThueVAT = thueVAT,
                        TongCong = tongCong
                    });
                }

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải dữ liệu: " + ex.Message);
                return Page();
            }
        }

        // XỬ LÝ GỘP BILL LINH HOẠT TẠI QUẦY THU NGÂN
        public async Task<IActionResult> OnPostGopBillAsync(List<string> maHoaDonGopList)
        {
            if (User?.Identity?.IsAuthenticated != true) return RedirectToPage("/TaiKhoan/DangNhap");

            if (!await KiemTraQuyenTruyCapThuNganAsync())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Tài khoản không có quyền thực hiện thao tác gộp bill!";
                return RedirectToPage();
            }

            try
            {
                if (maHoaDonGopList == null || maHoaDonGopList.Count < 2)
                {
                    TempData["ErrorMessage"] = "Vui lòng tích chọn ít nhất 2 hóa đơn chờ thanh toán để Gộp bill!";
                    return RedirectToPage();
                }

                var dsHoaDonCanGop = await _context.HOA_DON_NHA_HANG
                    .Where(h => maHoaDonGopList.Contains(h.MaHoaDonNhaHang) && h.HinhThucThanhToan != "Đã Thanh Toán")
                    .OrderBy(h => h.ThoiGianTao)
                    .ToListAsync();

                if (dsHoaDonCanGop.Count < 2)
                {
                    TempData["ErrorMessage"] = "Các hóa đơn được chọn không hợp lệ hoặc đã thanh toán!";
                    return RedirectToPage();
                }

                var billChinh = dsHoaDonCanGop.First();
                var cacBillPhu = dsHoaDonCanGop.Skip(1).ToList();

                var danhSachMaBanGop = new List<string>();
                string maBanChinh = GetMaBanOfHoaDon(billChinh);
                if (!string.IsNullOrEmpty(maBanChinh)) danhSachMaBanGop.AddRange(maBanChinh.Split(',', StringSplitOptions.RemoveEmptyEntries));

                var mergedJsonElements = new List<JsonElement>();
                string GetJsonOfHd(HOA_DON_NHA_HANG hd)
                {
                    try
                    {
                        var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) return propJson.GetValue(hd)?.ToString() ?? "[]";
                        return _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";
                    }
                    catch { return "[]"; }
                }

                try
                {
                    string jsonChinh = GetJsonOfHd(billChinh);
                    if (!string.IsNullOrWhiteSpace(jsonChinh) && jsonChinh != "[]")
                    {
                        using var doc = JsonDocument.Parse(jsonChinh);
                        foreach (var el in doc.RootElement.EnumerateArray())
                            mergedJsonElements.Add(el.Clone());
                    }
                }
                catch { }

                var dsChiTietDb = await _context.CHI_TIET_DICH_VU.ToListAsync();

                foreach (var billPhu in cacBillPhu)
                {
                    billChinh.TongTienMonAn = (billChinh.TongTienMonAn ?? 0) + (billPhu.TongTienMonAn ?? 0);
                    billChinh.GiamGia = (billChinh.GiamGia ?? 0) + (billPhu.GiamGia ?? 0);
                    billChinh.ThanhTien = (billChinh.ThanhTien ?? 0) + (billPhu.ThanhTien ?? 0);

                    string maBanPhu = GetMaBanOfHoaDon(billPhu);
                    if (!string.IsNullOrEmpty(maBanPhu))
                    {
                        foreach (var mb in maBanPhu.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!danhSachMaBanGop.Contains(mb.Trim()))
                                danhSachMaBanGop.Add(mb.Trim());
                        }
                    }

                    try
                    {
                        string jsonPhu = GetJsonOfHd(billPhu);
                        if (!string.IsNullOrWhiteSpace(jsonPhu) && jsonPhu != "[]")
                        {
                            using var docPhu = JsonDocument.Parse(jsonPhu);
                            foreach (var el in docPhu.RootElement.EnumerateArray())
                                mergedJsonElements.Add(el.Clone());
                        }
                    }
                    catch { }

                    foreach (var ct in dsChiTietDb)
                    {
                        string maHdCt = "";
                        try { maHdCt = _context.Entry(ct).Property("MaHoaDonNhaHang").CurrentValue?.ToString() ?? ""; } catch { }
                        if (string.IsNullOrEmpty(maHdCt))
                        {
                            try { maHdCt = _context.Entry(ct).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue?.ToString() ?? ""; } catch { }
                        }

                        if (maHdCt == billPhu.MaHoaDonNhaHang)
                        {
                            try { _context.Entry(ct).Property("MaHoaDonNhaHang").CurrentValue = billChinh.MaHoaDonNhaHang; } catch { }
                            try { _context.Entry(ct).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue = billChinh.MaHoaDonNhaHang; } catch { }
                        }
                    }

                    _context.HOA_DON_NHA_HANG.Remove(billPhu);
                }

                string chuoiMaBanGop = string.Join(",", danhSachMaBanGop);
                var propMb = billChinh.GetType().GetProperty("MaBan") ?? billChinh.GetType().GetProperty("BAN_NHA_HANGMaBan");
                if (propMb != null) propMb.SetValue(billChinh, chuoiMaBanGop);
                try { _context.Entry(billChinh).Property("MaBan").CurrentValue = chuoiMaBanGop; } catch { }
                try { _context.Entry(billChinh).Property("BAN_NHA_HANGMaBan").CurrentValue = chuoiMaBanGop; } catch { }

                try
                {
                    string mergedJsonStr = JsonSerializer.Serialize(mergedJsonElements);
                    var propJson = billChinh.GetType().GetProperty("DanhSachMonJson");
                    if (propJson != null) propJson.SetValue(billChinh, mergedJsonStr);
                    else _context.Entry(billChinh).Property("DanhSachMonJson").CurrentValue = mergedJsonStr;
                }
                catch { }

                billChinh.HinhThucThanhToan = "Chờ Thanh Toán";

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã gộp {dsHoaDonCanGop.Count} hóa đơn thành 1 Bill thống nhất (Mã HD: {billChinh.MaHoaDonNhaHang})!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi gộp bill: " + ex.Message;
            }

            return RedirectToPage();
        }

        // XỬ LÝ THANH TOÁN & CHỐT HÓA ĐƠN
        public async Task<IActionResult> OnPostThanhToanAsync(string maHoaDonToPay)
        {
            if (User?.Identity?.IsAuthenticated != true) return RedirectToPage("/TaiKhoan/DangNhap");

            if (!await KiemTraQuyenTruyCapThuNganAsync())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Tài khoản không có quyền thực hiện thanh toán hóa đơn!";
                return RedirectToPage();
            }

            try
            {
                if (string.IsNullOrEmpty(maHoaDonToPay)) return RedirectToPage();

                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonToPay);
                if (hd != null)
                {
                    hd.HinhThucThanhToan = "Đã Thanh Toán";

                    string maBanCuaHd = GetMaBanOfHoaDon(hd);
                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(x => x.Trim())
                                                  .ToList();

                    var cacBanLienQuan = await _context.BAN_NHA_HANG
                        .Where(b => danhSachMaBan.Contains(b.MaBan))
                        .ToListAsync();

                    // Đồng loạt chuyển toàn bộ các bàn liên quan (bao gồm cả các bàn đã gộp) sang trạng thái "Đang dọn dẹp"
                    foreach (var ban in cacBanLienQuan)
                    {
                        ban.TrangThai = "Đang dọn dẹp";
                        if (ban.GetType().GetProperty("MaHoaDonHienTai") != null)
                        {
                            ban.GetType().GetProperty("MaHoaDonHienTai")?.SetValue(ban, null);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Chốt hóa đơn {maHoaDonToPay} thành công! Đã chuyển {cacBanLienQuan.Count} bàn liên quan sang trạng thái Đang dọn dẹp.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xử lý thanh toán: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}