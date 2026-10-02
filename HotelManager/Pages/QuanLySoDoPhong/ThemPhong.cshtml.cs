using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.IO;
using System.Threading.Tasks;
// TODO: Đảm bảo sử dụng đúng namespace chứa ApplicationDbContext của bạn
// using HotelManager.Data; 

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class ThemPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ThemPhongModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public PHONG InputPhong { get; set; } = new PHONG();

        // Thêm dấu ? để khắc phục CS8618
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
                // Xử lý lưu hình ảnh
                if (HinhAnhUpload != null && HinhAnhUpload.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhAnhUpload.FileName);
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "rooms");
                    Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhUpload.CopyToAsync(fileStream);
                    }
                    InputPhong.HinhAnh = "/images/rooms/" + fileName;
                }

                _context.PHONG.Add(InputPhong);
                await _context.SaveChangesAsync();

                return RedirectToPage("/QuanLySoDoPhong/TimPhong");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống: {ex.Message}");
                return Page();
            }
        }
    }
}