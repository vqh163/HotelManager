using HotelManager.Data;
using HotelManager.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class SoDoBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SoDoBanModel> _logger;

        public SoDoBanModel(ApplicationDbContext context, ILogger<SoDoBanModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        // Chỉ bằng true khi tài khoản có vai trò Quản lý / Giám đốc (ẩn nút Thêm/Sửa/Xóa bàn với Phục vụ)
        public bool LaQuanLy { get; set; } = false;

        public List<BanNhaHangViewModel> DanhSachBan { get; set; } = new List<BanNhaHangViewModel>();
        public List<BanNhaHangViewModel> TatCaBan { get; set; } = new List<BanNhaHangViewModel>();

        private bool KiemTraLaQuanLy()
        {
            return User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);
        }

        // Hỗ trợ kiểm tra hóa đơn có gắn mã bàn cần tìm hay không (bao gồm cả trường hợp gộp bàn "B01,B02")
        private static bool HoaDonChuaMaBan(string? chuoiMaBan, string maBan)
        {
            if (string.IsNullOrWhiteSpace(chuoiMaBan) || string.IsNullOrWhiteSpace(maBan))
                return false;

            return chuoiMaBan
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Contains(maBan.Trim());
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LaQuanLy = KiemTraLaQuanLy();

            try
            {
                var toanBoBanDb = await _context.BAN_NHA_HANG.AsNoTracking()
                    .OrderBy(b => b.TenBan)
                    .ToListAsync();

                // Truy vấn thuần LINQ các hóa đơn đang mở (chưa thanh toán / chưa ghi nợ phòng / chưa hủy)
                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                    .Where(h => (h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                              || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan
                              || string.IsNullOrEmpty(h.TrangThaiThanhToan))
                             && h.HinhThucThanhToan != "Đã Thanh Toán"
                             && h.HinhThucThanhToan != "Ghi nợ phòng"
                             && h.HinhThucThanhToan != "Đã Hủy")
                    .OrderByDescending(h => h.ThoiGianTao)
                    .ToListAsync();

                try { DatBanStore.Load(); } catch { }

                TatCaBan = toanBoBanDb.Select(b =>
                {
                    string maBanClean = (b.MaBan ?? string.Empty).Trim();
                    var hoaDonCuaBan = dsHoaDonActive.FirstOrDefault(h => HoaDonChuaMaBan(h.BAN_NHA_HANGMaBan, maBanClean));

                    string tenKhach = b.TenKhachDat ?? string.Empty;
                    if (string.IsNullOrEmpty(tenKhach) && DatBanStore.DuLieu.TryGetValue(maBanClean, out var datBan))
                    {
                        tenKhach = datBan.HoTen;
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

                if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                {
                    string kw = TuKhoaTimKiem.Trim();
                    filtered = filtered.Where(b => b.TenBan.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                                || b.MaBan.Contains(kw, StringComparison.OrdinalIgnoreCase));
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
                _logger.LogError(ex, "Lỗi tải sơ đồ bàn nhà hàng.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải sơ đồ bàn.");
                return Page();
            }
        }

        // Xác nhận bàn đã dọn dẹp xong -> chuyển về Trống
        public async Task<IActionResult> OnPostXacNhanSachAsync(string maBan)
        {
            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBan);
                if (ban != null)
                {
                    ban.TrangThai = "Trống";
                    ban.TenKhachDat = null;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xác nhận bàn sạch {MaBan}", maBan);
                TempData["ErrorMessage"] = "Lỗi khi cập nhật trạng thái bàn.";
            }
            return RedirectToPage();
        }

        // Xóa bàn (chỉ Quản lý/Giám đốc được phép; chặn xóa bàn đang phục vụ hoặc còn hóa đơn mở)
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (!KiemTraLaQuanLy())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Tài khoản của bạn không có quyền xóa bàn!";
                return RedirectToPage();
            }

            try
            {
                var dsHoaDonMo = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                    .Where(h => h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                             || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan
                             || (string.IsNullOrEmpty(h.TrangThaiThanhToan) && h.HinhThucThanhToan != "Đã Thanh Toán"))
                    .Select(h => h.BAN_NHA_HANGMaBan)
                    .ToListAsync();

                if (dsHoaDonMo.Any(mb => HoaDonChuaMaBan(mb, id)))
                {
                    TempData["ErrorMessage"] = "Không thể xóa bàn đang có hóa đơn phục vụ chưa thanh toán!";
                    return RedirectToPage();
                }

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
                    TempData["SuccessMessage"] = $"Đã xóa bàn '{ban.TenBan}' thành công.";
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa bàn {Id}", id);
                TempData["ErrorMessage"] = "Không thể xóa bàn do lỗi hệ thống.";
                return RedirectToPage();
            }
        }

        // Gửi yêu cầu thanh toán sang Quầy Thu Ngân
        public async Task<IActionResult> OnPostYeuCauThanhToanAsync(string maBan, string maHoaDon)
        {
            try
            {
                var hoaDon = await _context.HOA_DON_NHA_HANG
                    .FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDon);

                if (hoaDon != null && HoaDonChuaMaBan(hoaDon.BAN_NHA_HANGMaBan, maBan))
                {
                    hoaDon.TrangThaiThanhToan = HangSoHeThong.HoaDonNHChoThanhToan;
                    hoaDon.HinhThucThanhToan = "Chờ Thanh Toán";
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã gửi yêu cầu thanh toán (Mã HD: {maHoaDon}) tới Quầy thu ngân!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy hóa đơn hợp lệ {maHoaDon} thuộc bàn này!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi yêu cầu thanh toán {MaHD}", maHoaDon);
                TempData["ErrorMessage"] = "Lỗi hệ thống khi gửi yêu cầu thanh toán.";
            }

            return RedirectToPage();
        }

        // Chuyển bàn đang phục vụ sang một bàn trống khác (thuần LINQ + Transaction)
        public async Task<IActionResult> OnPostChuyenBanAsync(string maBanCu, string maBanMoi)
        {
            if (string.IsNullOrWhiteSpace(maBanCu) || string.IsNullOrWhiteSpace(maBanMoi) || maBanCu.Trim() == maBanMoi.Trim())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn bàn đích hợp lệ để chuyển!";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string cuClean = maBanCu.Trim();
                string moiClean = maBanMoi.Trim();

                var banCu = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == cuClean);
                var banMoi = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == moiClean);

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
                    .Where(h => h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                             || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan
                             || (string.IsNullOrEmpty(h.TrangThaiThanhToan) && h.HinhThucThanhToan != "Đã Thanh Toán"))
                    .OrderByDescending(h => h.ThoiGianTao)
                    .ToListAsync();

                var hoaDonChuyen = dsHoaDonActive.FirstOrDefault(h => HoaDonChuaMaBan(h.BAN_NHA_HANGMaBan, cuClean));

                if (hoaDonChuyen != null)
                {
                    var danhSachMaBan = (hoaDonChuyen.BAN_NHA_HANGMaBan ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => m.Trim())
                        .Select(m => m == cuClean ? moiClean : m)
                        .Distinct()
                        .ToList();

                    string chuoiMaBanMoi = string.Join(",", danhSachMaBan);
                    if (chuoiMaBanMoi.Length > 20)
                    {
                        TempData["ErrorMessage"] = "Không thể chuyển bàn vì vượt quá độ dài mã bàn cho phép.";
                        return RedirectToPage();
                    }

                    hoaDonChuyen.BAN_NHA_HANGMaBan = chuoiMaBanMoi;
                    _context.HOA_DON_NHA_HANG.Update(hoaDonChuyen);
                }

                banMoi.TrangThai = "Đang có khách";
                banCu.TrangThai = "Trống";
                banCu.TenKhachDat = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã chuyển toàn bộ hóa đơn từ bàn {banCu.TenBan} sang bàn {banMoi.TenBan} thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi chuyển bàn từ {BanCu} sang {BanMoi}", maBanCu, maBanMoi);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi chuyển bàn.";
            }

            return RedirectToPage();
        }
    }
}
