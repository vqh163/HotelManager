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

        public List<BanNhaHangViewModel> DanhSachBan { get; set; } = new List<BanNhaHangViewModel>();

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.BAN_NHA_HANG.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                {
                    string kw = TuKhoaTimKiem.Trim();
                    query = query.Where(b => (b.TenBan != null && b.TenBan.Contains(kw)) || b.MaBan.Contains(kw));
                }

                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả bàn")
                    query = query.Where(b => b.TrangThai == TrangThaiLoc);

                var dsBanDb = await query.OrderBy(b => b.TenBan).ToListAsync();

                // Truy vấn thuần LINQ các hóa đơn đang mở (chưa thanh toán / chưa ghi nợ phòng)
                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                    .Where(h => h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                             || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan
                             || (string.IsNullOrEmpty(h.TrangThaiThanhToan) && h.HinhThucThanhToan != "Đã Thanh Toán"))
                    .OrderByDescending(h => h.ThoiGianTao)
                    .ToListAsync();

                DanhSachBan = dsBanDb.Select(b =>
                {
                    var hoaDonCuaBan = dsHoaDonActive.FirstOrDefault(h => h.BAN_NHA_HANGMaBan == b.MaBan);

                    return new BanNhaHangViewModel
                    {
                        MaBan = b.MaBan,
                        TenBan = b.TenBan ?? string.Empty,
                        SucChua = b.SucChua?.ToString() ?? "0",
                        TrangThai = b.TrangThai ?? "Trống",
                        MaHoaDonNhaHang = (b.TrangThai == "Đang có khách" && hoaDonCuaBan != null) ? hoaDonCuaBan.MaHoaDonNhaHang : null,
                        TenKhachDat = (b.TrangThai == "Đã đặt trước") ? (b.TenKhachDat ?? string.Empty) : string.Empty
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải sơ đồ bàn nhà hàng.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải sơ đồ bàn.");
            }
        }

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

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            try
            {
                // Chặn xóa bàn nếu đang có hóa đơn chưa thanh toán
                bool conHoaDonMo = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                    .AnyAsync(h => h.BAN_NHA_HANGMaBan == id
                        && (h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                            || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan));
                if (conHoaDonMo)
                {
                    TempData["ErrorMessage"] = "Không thể xóa bàn đang có hóa đơn phục vụ chưa thanh toán!";
                    return RedirectToPage();
                }

                var ban = await _context.BAN_NHA_HANG.FindAsync(id);
                if (ban != null)
                {
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

        public async Task<IActionResult> OnPostYeuCauThanhToanAsync(string maBan, string maHoaDon)
        {
            try
            {
                var hoaDon = await _context.HOA_DON_NHA_HANG
                    .FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDon && h.BAN_NHA_HANGMaBan == maBan);

                if (hoaDon != null)
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
    }
}
