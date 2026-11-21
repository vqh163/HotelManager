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
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class BanNhaHangViewModel
    {
        public string MaBan { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;
        public string SucChua { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? MaHoaDonNhaHang { get; set; }
        public string TenKhachDat { get; set; } = string.Empty;
        public decimal TongTienTamTinh { get; set; } = 0;
    }

    // Bắt buộc đăng nhập; tự động chuyển về trang đăng nhập nếu truy cập trái phép
    [Authorize]
    public class SoDoBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SoDoBanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        // Chỉ bằng true khi tài khoản đăng nhập thật trong DB có VaiTro là Quản lý
        public bool LaQuanLy { get; set; } = false;

        public List<BanNhaHangViewModel> DanhSachBan { get; set; } = new List<BanNhaHangViewModel>();
        public List<BanNhaHangViewModel> TatCaBan { get; set; } = new List<BanNhaHangViewModel>();

        // Truy vấn trực tiếp VaiTro từ Cookie Claims và đối chiếu bảng TAI_KHOAN trong CSDL bằng LINQ
        private async Task<string> LayVaiTroThucTeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return string.Empty;

            // 1. Ưu tiên kiểm tra trực tiếp trong bảng TAI_KHOAN của SQL Server theo TenDangNhap hoặc MaTaiKhoan
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

            // 2. Nếu không tìm thấy qua tên, lấy từ Claim đã lưu lúc đăng nhập
            string vaiTroClaim = User.FindFirst(ClaimTypes.Role)?.Value
                              ?? User.FindFirst("VaiTro")?.Value
                              ?? string.Empty;

            return vaiTroClaim.Trim();
        }

        // Kiểm tra chính xác quyền Quản lý (Loại trừ hoàn toàn Phục vụ Nhà hàng)
        private async Task<bool> KiemTraQuyenQuanLyAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return false;

            string vaiTro = await LayVaiTroThucTeAsync();

            // Nếu tài khoản là Phục vụ Nhà hàng / Phục vụ thì bắt buộc trả về false
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

        // Kiểm tra tài khoản có quyền truy cập trang Sơ đồ bàn hay không (Quản lý, Phục vụ Nhà hàng, Thu ngân Nhà hàng)
        private async Task<bool> KiemTraQuyenTruyCapSoDoBanAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return false;

            string vaiTro = await LayVaiTroThucTeAsync();
            if (string.IsNullOrEmpty(vaiTro)) return false;

            return await KiemTraQuyenQuanLyAsync() ||
                   vaiTro.Contains("Phục vụ", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("PhucVu", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Thu ngân", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("ThuNgan", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Chuyển hướng ngay về trang đăng nhập nếu chưa xác thực hoặc truy cập trái phép
            if (User?.Identity?.IsAuthenticated != true || !await KiemTraQuyenTruyCapSoDoBanAsync())
            {
                return RedirectToPage("/TaiKhoan/DangNhap");
            }

            try
            {
                // Xác định quyền Quản lý thực tế từ CSDL: Quản lý = true, Phục vụ Nhà hàng = false
                LaQuanLy = await KiemTraQuyenQuanLyAsync();

                var toanBoBanDb = await _context.BAN_NHA_HANG.OrderBy(b => b.TenBan).ToListAsync();

                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG
                    .Where(h => h.HinhThucThanhToan != "Đã Thanh Toán")
                    .OrderByDescending(x => x.ThoiGianTao)
                    .ToListAsync();

                TatCaBan = toanBoBanDb.Select(b =>
                {
                    string maBanClean = (b.MaBan ?? string.Empty).Trim();

                    var hoaDonCuaBan = dsHoaDonActive.FirstOrDefault(x =>
                    {
                        string maBanHd = "";
                        var prop = x.GetType().GetProperty("MaBan") ?? x.GetType().GetProperty("BAN_NHA_HANGMaBan");
                        if (prop != null) maBanHd = prop.GetValue(x)?.ToString() ?? "";
                        if (string.IsNullOrEmpty(maBanHd))
                        {
                            try { maBanHd = _context.Entry(x).Property("MaBan").CurrentValue?.ToString() ?? ""; } catch { }
                        }
                        if (string.IsNullOrEmpty(maBanHd))
                        {
                            try { maBanHd = _context.Entry(x).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? ""; } catch { }
                        }

                        // [VỊ TRÍ 1 - ĐÃ CẬP NHẬT]: Tách theo dấu phẩy để tất cả các bàn gộp (VD: B04,B08,B09) đều map đúng hóa đơn chung
                        var danhSachMaBanTrongHd = maBanHd.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(m => m.Trim());
                        return danhSachMaBanTrongHd.Contains(maBanClean);
                    });

                    string tenKhach = b.GetType().GetProperty("TenKhachDat")?.GetValue(b)?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(tenKhach))
                    {
                        try
                        {
                            DatBanStore.Load();
                            if (DatBanStore.DuLieu.ContainsKey(maBanClean))
                            {
                                tenKhach = DatBanStore.DuLieu[maBanClean].HoTen;
                            }
                        }
                        catch { }
                    }

                    decimal tamTinh = 0;
                    if (b.TrangThai == "Đang có khách" && hoaDonCuaBan != null)
                    {
                        tamTinh = (hoaDonCuaBan.ThanhTien ?? 0) > 0
                                ? (hoaDonCuaBan.ThanhTien ?? 0)
                                : (hoaDonCuaBan.TongTienMonAn ?? 0);
                    }

                    return new BanNhaHangViewModel
                    {
                        MaBan = b.MaBan,
                        TenBan = b.TenBan ?? string.Empty,
                        SucChua = b.SucChua?.ToString() ?? "0",
                        TrangThai = b.TrangThai ?? "Trống",
                        MaHoaDonNhaHang = (b.TrangThai == "Đang có khách" && hoaDonCuaBan != null) ? hoaDonCuaBan.MaHoaDonNhaHang : null,
                        TenKhachDat = (b.TrangThai == "Đã đặt trước") ? tenKhach : string.Empty,
                        TongTienTamTinh = tamTinh
                    };
                }).ToList();

                var filtered = TatCaBan.AsEnumerable();

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    filtered = filtered.Where(b => b.TenBan.Contains(TuKhoaTimKiem, StringComparison.OrdinalIgnoreCase) ||
                                                   b.MaBan.Contains(TuKhoaTimKiem, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả bàn")
                {
                    filtered = filtered.Where(b => b.TrangThai == TrangThaiLoc);
                }

                DanhSachBan = filtered.ToList();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải sơ đồ bàn: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostXacNhanSachAsync(string maBan)
        {
            if (User?.Identity?.IsAuthenticated != true || !await KiemTraQuyenTruyCapSoDoBanAsync())
                return RedirectToPage("/TaiKhoan/DangNhap");

            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBan);
                if (ban != null)
                {
                    ban.TrangThai = "Trống";
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật trạng thái bàn: " + ex.Message);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (User?.Identity?.IsAuthenticated != true) return RedirectToPage("/TaiKhoan/DangNhap");

            // Chặn chặt chẽ ở tầng Backend: Chỉ Quản lý mới được phép xóa bàn, Phục vụ Nhà hàng bị từ chối
            if (!await KiemTraQuyenQuanLyAsync())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Tài khoản Phục vụ nhà hàng không có quyền xóa bàn!";
                return RedirectToPage();
            }

            try
            {
                var ban = await _context.BAN_NHA_HANG.FindAsync(id);
                if (ban != null)
                {
                    if (ban.TrangThai != "Trống")
                    {
                        TempData["ErrorMessage"] = $"Không thể xóa bàn {ban.TenBan} vì bàn đang ở trạng thái '{ban.TrangThai}'!";
                        return RedirectToPage();
                    }

                    _context.BAN_NHA_HANG.Remove(ban);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã xóa bàn {ban.TenBan} thành công!";
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xóa bàn: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostYeuCauThanhToanAsync(string maBan, string maHoaDon)
        {
            if (User?.Identity?.IsAuthenticated != true || !await KiemTraQuyenTruyCapSoDoBanAsync())
                return RedirectToPage("/TaiKhoan/DangNhap");

            try
            {
                var hoaDon = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDon);

                if (hoaDon != null)
                {
                    hoaDon.HinhThucThanhToan = "Chờ Thanh Toán";
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã gửi yêu cầu thanh toán (Mã HD: {maHoaDon}) tới Quầy thu ngân!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi: Không tìm thấy hóa đơn {maHoaDon} trong hệ thống. Vui lòng kiểm tra lại!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống khi gửi yêu cầu thanh toán: " + ex.Message;
            }

            return RedirectToPage();
        }

        // CHUYỂN BÀN (Giữ nguyên trong menu 3 chấm)
        public async Task<IActionResult> OnPostChuyenBanAsync(string maBanCu, string maBanMoi)
        {
            if (User?.Identity?.IsAuthenticated != true || !await KiemTraQuyenTruyCapSoDoBanAsync())
                return RedirectToPage("/TaiKhoan/DangNhap");

            try
            {
                if (string.IsNullOrEmpty(maBanCu) || string.IsNullOrEmpty(maBanMoi) || maBanCu == maBanMoi)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn bàn đích hợp lệ để chuyển!";
                    return RedirectToPage();
                }

                var banCu = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBanCu);
                var banMoi = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBanMoi);

                if (banCu == null || banMoi == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin bàn trong hệ thống!";
                    return RedirectToPage();
                }

                if (banMoi.TrangThai != "Trống")
                {
                    TempData["ErrorMessage"] = $"Bàn đích ({banMoi.TenBan}) hiện không trống. Vui lòng chọn bàn trống!";
                    return RedirectToPage();
                }

                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG
                    .Where(h => h.HinhThucThanhToan != "Đã Thanh Toán")
                    .OrderByDescending(x => x.ThoiGianTao)
                    .ToListAsync();

                var hoaDonChuyen = dsHoaDonActive.FirstOrDefault(x =>
                {
                    string mb = "";
                    var prop = x.GetType().GetProperty("MaBan") ?? x.GetType().GetProperty("BAN_NHA_HANGMaBan");
                    if (prop != null) mb = prop.GetValue(x)?.ToString() ?? "";
                    if (string.IsNullOrEmpty(mb))
                    {
                        try { mb = _context.Entry(x).Property("MaBan").CurrentValue?.ToString() ?? ""; } catch { }
                    }
                    if (string.IsNullOrEmpty(mb))
                    {
                        try { mb = _context.Entry(x).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? ""; } catch { }
                    }
                    return mb.Trim() == maBanCu.Trim();
                });

                if (hoaDonChuyen != null)
                {
                    string maBanMoiClean = maBanMoi.Trim();
                    var prop = hoaDonChuyen.GetType().GetProperty("MaBan") ?? hoaDonChuyen.GetType().GetProperty("BAN_NHA_HANGMaBan");
                    if (prop != null) prop.SetValue(hoaDonChuyen, maBanMoiClean);
                    try { _context.Entry(hoaDonChuyen).Property("MaBan").CurrentValue = maBanMoiClean; } catch { }
                    try { _context.Entry(hoaDonChuyen).Property("BAN_NHA_HANGMaBan").CurrentValue = maBanMoiClean; } catch { }

                    if (banMoi.GetType().GetProperty("MaHoaDonHienTai") != null)
                    {
                        banMoi.GetType().GetProperty("MaHoaDonHienTai")?.SetValue(banMoi, hoaDonChuyen.MaHoaDonNhaHang);
                    }
                }

                if (banCu.GetType().GetProperty("MaHoaDonHienTai") != null)
                {
                    banCu.GetType().GetProperty("MaHoaDonHienTai")?.SetValue(banCu, null);
                }

                banMoi.TrangThai = "Đang có khách";
                banCu.TrangThai = "Trống";

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã chuyển toàn bộ hóa đơn từ bàn {banCu.TenBan} sang bàn {banMoi.TenBan} thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi chuyển bàn: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}