using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    [Authorize(Policy = HangSoHeThong.PolicyQuanLyCap)]
    public class ThemPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ThemPhongModel> _logger;

        public ThemPhongModel(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<ThemPhongModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public PHONG InputPhong { get; set; } = new PHONG();

        [BindProperty]
        public IFormFile? HinhAnhUpload { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string maPhongChuan = (InputPhong.MaPhong ?? string.Empty).Trim();
                string soPhongChuan = (InputPhong.SoPhong ?? string.Empty).Trim();

                if (await _context.PHONG.AnyAsync(p => p.MaPhong == maPhongChuan || (!string.IsNullOrEmpty(soPhongChuan) && p.SoPhong == soPhongChuan)))
                {
                    ModelState.AddModelError(string.Empty, "Mã phòng hoặc Số phòng đã tồn tại trong hệ thống!");
                    return Page();
                }

                // Kiểm tra và lưu ảnh phòng an toàn
                if (HinhAnhUpload != null && HinhAnhUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "rooms");
                    var (ok, ketQua) = await BaoMatHelper.LuuAnhAnToanAsync(HinhAnhUpload, uploadsFolder, "ROOM");
                    if (!ok)
                    {
                        ModelState.AddModelError("HinhAnhUpload", ketQua);
                        return Page();
                    }
                    InputPhong.HinhAnh = "/images/rooms/" + ketQua;
                }

                // Khởi tạo sẵn bản ghi trạng thái phòng độc lập (TTP_SoPhong)
                string maTTRieng = "TTP_" + (string.IsNullOrEmpty(soPhongChuan) ? maPhongChuan : soPhongChuan);
                if (maTTRieng.Length > 20) maTTRieng = maTTRieng.Substring(0, 20);

                if (!await _context.TRANG_THAI_PHONG.AnyAsync(t => t.MaTrangThai == maTTRieng))
                {
                    _context.TRANG_THAI_PHONG.Add(new TRANG_THAI_PHONG
                    {
                        MaTrangThai = maTTRieng,
                        TrangThaiPhong = "Trống",
                        TrangThaiVeSinh = "Sạch",
                        ThoiGianDonVeSinh = DateTime.Now
                    });
                }

                InputPhong.MaPhong = maPhongChuan;
                InputPhong.SoPhong = soPhongChuan;
                InputPhong.MaTrangThai = maTTRieng;

                _context.PHONG.Add(InputPhong);
                await _context.SaveChangesAsync();

                return RedirectToPage("/QuanLySoDoPhong/QuanLyDSPhong");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm phòng mới.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi thêm phòng.");
                return Page();
            }
        }
    }
}
