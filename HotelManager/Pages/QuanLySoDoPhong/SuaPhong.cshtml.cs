using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Threading.Tasks;
// Khai báo đúng namespace chứa DbContext và Models của Nhóm 4
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Bắt buộc phân quyền: Chỉ tài khoản có VaiTro "Quản lý" mới được phép sửa phòng
    [Authorize(Roles = "Quản lý")]
    public class SuaPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SuaPhongModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public PHONG InputPhong { get; set; } = default!;

        // Khai báo cho phép null (CS8618) vì khi sửa, người dùng có thể không đổi ảnh mới
        [BindProperty]
        public IFormFile? HinhAnhUpload { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                // SỬA LỖI: Trỏ đúng về file QuanLyDSPhong
                return RedirectToPage("/QuanLySoDoPhong/QuanLyDSPhong");
            }

            try
            {
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null)
                {
                    // SỬA LỖI: Trỏ đúng về file QuanLyDSPhong
                    return RedirectToPage("/QuanLySoDoPhong/QuanLyDSPhong");
                }

                InputPhong = phong;
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống khi tải dữ liệu: {ex.Message}");
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

                phongInDb.SoPhong = InputPhong.SoPhong;
                phongInDb.LoaiPhong = InputPhong.LoaiPhong;
                phongInDb.SoGiuong = InputPhong.SoGiuong;
                phongInDb.SucChuaToiDa = InputPhong.SucChuaToiDa;
                phongInDb.GiaTheoNgay = InputPhong.GiaTheoNgay;
                phongInDb.MoTa = InputPhong.MoTa;

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

                    phongInDb.HinhAnh = "/images/rooms/" + fileName;
                }

                await _context.SaveChangesAsync();

                // SỬA LỖI CHÍNH Ở ĐÂY: Trỏ đúng về file QuanLyDSPhong sau khi lưu thành công
                return RedirectToPage("/QuanLySoDoPhong/QuanLyDSPhong");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống khi lưu thay đổi: {ex.Message}");
                return Page();
            }
        }
    }
}