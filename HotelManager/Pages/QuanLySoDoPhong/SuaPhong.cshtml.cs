using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    [Authorize(Policy = HangSoHeThong.PolicyQuanLyCap)]
    public class SuaPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SuaPhongModel> _logger;

        public SuaPhongModel(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<SuaPhongModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public PHONG InputPhong { get; set; } = default!;

        [BindProperty]
        public IFormFile? HinhAnhUpload { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("/QuanLySoDoPhong/QuanLyDSPhong");
            }

            try
            {
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null)
                {
                    return RedirectToPage("/QuanLySoDoPhong/QuanLyDSPhong");
                }

                InputPhong = phong;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải thông tin phòng {Id}", id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải thông tin phòng.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var phongInDb = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == InputPhong.MaPhong);
                if (phongInDb == null)
                {
                    ModelState.AddModelError(string.Empty, "Không tìm thấy phòng để cập nhật.");
                    return Page();
                }

                string soPhongMoi = (InputPhong.SoPhong ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(soPhongMoi) &&
                    await _context.PHONG.AnyAsync(p => p.MaPhong != phongInDb.MaPhong && p.SoPhong == soPhongMoi))
                {
                    ModelState.AddModelError("InputPhong.SoPhong", "Số phòng này đã được sử dụng cho phòng khác!");
                    return Page();
                }

                phongInDb.SoPhong = soPhongMoi;
                phongInDb.LoaiPhong = InputPhong.LoaiPhong;
                phongInDb.SoGiuong = InputPhong.SoGiuong;
                phongInDb.SucChuaToiDa = InputPhong.SucChuaToiDa;
                phongInDb.GiaTheoNgay = InputPhong.GiaTheoNgay;
                phongInDb.MoTa = InputPhong.MoTa;

                if (HinhAnhUpload != null && HinhAnhUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "rooms");
                    var (ok, ketQua) = await BaoMatHelper.LuuAnhAnToanAsync(HinhAnhUpload, uploadsFolder, "ROOM");
                    if (!ok)
                    {
                        ModelState.AddModelError("HinhAnhUpload", ketQua);
                        return Page();
                    }
                    phongInDb.HinhAnh = "/images/rooms/" + ketQua;
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("/QuanLySoDoPhong/QuanLyDSPhong");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật phòng {MaPhong}", InputPhong.MaPhong);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi lưu thay đổi phòng.");
                return Page();
            }
        }
    }
}
