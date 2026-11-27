using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Phân quyền chỉ Quản lý / Giám đốc được quản lý danh mục phòng
    [Authorize(Policy = HangSoHeThong.PolicyQuanLyCap)]
    public class QuanLyDSPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuanLyDSPhongModel> _logger;

        public QuanLyDSPhongModel(ApplicationDbContext context, ILogger<QuanLyDSPhongModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<PHONG> DanhSachPhong { get; set; } = new List<PHONG>();

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? NgayTraCuu { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                DateTime targetDate = (NgayTraCuu ?? DateTime.Now.Date).Date;
                NgayTraCuu = targetDate;

                var query = _context.PHONG.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                {
                    string kw = TuKhoaTimKiem.Trim();
                    query = query.Where(p =>
                        (p.SoPhong != null && p.SoPhong.Contains(kw)) ||
                        (p.LoaiPhong != null && p.LoaiPhong.Contains(kw)));
                }

                var rooms = await query.OrderBy(p => p.SoPhong).ToListAsync();

                // 1. Tối ưu 1 truy vấn duy nhất lấy danh sách trạng thái vật lý (chống N+1 query)
                var trangThaiDict = await _context.TRANG_THAI_PHONG.AsNoTracking()
                    .ToDictionaryAsync(t => t.MaTrangThai, t => t.TrangThaiPhong ?? string.Empty);

                // 2. Tối ưu 1 truy vấn duy nhất lấy danh sách phòng đã đặt/đang ở trong ngày tra cứu
                DateTime startOfDay = targetDate;
                DateTime endOfDay = targetDate.AddDays(1);
                var bookedRoomIds = (await (from dp in _context.DAT_PHONG.AsNoTracking()
                                            join ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking() on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                            where !HangSoHeThong.TrangThaiDaKetThuc.Contains(dp.TrangThaiDatPhong ?? "")
                                               && dp.NgayNhan < endOfDay
                                               && dp.NgayTra > startOfDay
                                               && ct.PHONGMaPhong != null
                                            select ct.PHONGMaPhong!).Distinct().ToListAsync()).ToHashSet();

                // 3. Xác định mã trạng thái hiển thị đồng bộ giữa TRANG_THAI_PHONG ("Bảo trì") và đơn đặt phòng
                foreach (var phong in rooms)
                {
                    bool dangBaoTri = phong.MaTrangThai == "TT04"
                        || (!string.IsNullOrEmpty(phong.MaTrangThai)
                            && trangThaiDict.TryGetValue(phong.MaTrangThai, out var ttText)
                            && ttText.Equals("Bảo trì", StringComparison.OrdinalIgnoreCase));

                    if (dangBaoTri)
                        phong.MaTrangThai = "TT04";
                    else if (bookedRoomIds.Contains(phong.MaPhong))
                        phong.MaTrangThai = "TT02";
                    else
                        phong.MaTrangThai = "TT01";
                }

                if (!string.IsNullOrEmpty(TrangThaiLoc))
                {
                    rooms = rooms.Where(p => p.MaTrangThai == TrangThaiLoc).ToList();
                }

                DanhSachPhong = rooms;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách phòng.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải danh sách phòng.");
                return Page();
            }
        }

        // Chuyển sang POST chống CSRF & đồng bộ trực tiếp vào bảng TRANG_THAI_PHONG
        public async Task<IActionResult> OnPostKhoaAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage();

            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return RedirectToPage();

                string maTTRieng = "TTP_" + (phong.SoPhong ?? phong.MaPhong).Trim();
                if (maTTRieng.Length > 20) maTTRieng = maTTRieng.Substring(0, 20);

                var tt = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == maTTRieng);
                if (tt == null)
                {
                    tt = new TRANG_THAI_PHONG
                    {
                        MaTrangThai = maTTRieng,
                        TrangThaiPhong = phong.MaTrangThai == "TT04" ? "Bảo trì" : "Trống",
                        TrangThaiVeSinh = "Sạch",
                        ThoiGianDonVeSinh = DateTime.Now
                    };
                    _context.TRANG_THAI_PHONG.Add(tt);
                }

                bool dangBaoTri = string.Equals(tt.TrangThaiPhong?.Trim(), "Bảo trì", StringComparison.OrdinalIgnoreCase);
                tt.TrangThaiPhong = dangBaoTri ? "Trống" : "Bảo trì";
                phong.MaTrangThai = maTTRieng;

                await _context.SaveChangesAsync();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khóa/mở khóa phòng {Id}", id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi thay đổi trạng thái bảo trì phòng.");
                await OnGetAsync();
                return Page();
            }
        }

        // Chuyển sang POST chống CSRF & kiểm tra đơn đặt phòng thực tế trước khi xóa
        public async Task<IActionResult> OnPostXoaAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage();

            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong != null)
                {
                    // Kiểm tra bằng LINQ xem phòng còn bất kỳ đơn đặt/lưu trú đang hiệu lực nào không
                    bool conDonHoatDong = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                                 join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                 where ct.PHONGMaPhong == id
                                                    && !HangSoHeThong.TrangThaiDaKetThuc.Contains(dp.TrangThaiDatPhong ?? "")
                                                 select dp.MaDatPhong).AnyAsync();

                    if (conDonHoatDong)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể xóa phòng đang có khách lưu trú hoặc còn đơn đặt phòng chưa hoàn tất!");
                        await OnGetAsync();
                        return Page();
                    }

                    _context.PHONG.Remove(phong);
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa phòng {Id}", id);
                ModelState.AddModelError(string.Empty, "Không thể xóa phòng do lỗi hệ thống.");
                await OnGetAsync();
                return Page();
            }
        }
    }
}
