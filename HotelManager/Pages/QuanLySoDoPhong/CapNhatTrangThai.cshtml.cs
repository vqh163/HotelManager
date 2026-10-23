using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Phân quyền chỉ cho phép Lễ tân và Buồng phòng truy cập chức năng này
    [Authorize(Roles = "Lễ tân, Buồng phòng")]
    public class CapNhatTrangThaiModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CapNhatTrangThaiModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Bắt buộc dùng BindProperty cho dữ liệu Form gửi lên
        [BindProperty]
        public TRANG_THAI_PHONG TrangThaiPhongEdit { get; set; } = new TRANG_THAI_PHONG();

        [BindProperty]
        public string MaPhong { get; set; } = string.Empty;

        // Xử lý lấy thông tin trạng thái phòng hiện tại
        public async Task<IActionResult> OnGetAsync(string maPhong)
        {
            if (string.IsNullOrEmpty(maPhong))
            {
                return NotFound();
            }

            MaPhong = maPhong;

            try
            {
                // Dùng LINQ truy xuất phòng và trạng thái phòng tương ứng
                var phong = await _context.PHONG
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.MaPhong == maPhong);

                if (phong == null || string.IsNullOrEmpty(phong.MaTrangThai))
                {
                    return NotFound();
                }

                var trangThai = await _context.TRANG_THAI_PHONG
                    .FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);

                if (trangThai == null)
                {
                    return NotFound();
                }

                TrangThaiPhongEdit = trangThai;
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi truy xuất dữ liệu: {ex.Message}");
                return Page();
            }
        }

        // Xử lý lưu thay đổi trạng thái
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Tìm record trạng thái trong DB
                var trangThaiDb = await _context.TRANG_THAI_PHONG
                    .FirstOrDefaultAsync(t => t.MaTrangThai == TrangThaiPhongEdit.MaTrangThai);

                if (trangThaiDb == null)
                {
                    return NotFound();
                }

                // Cập nhật các trường
                trangThaiDb.TrangThaiPhong = TrangThaiPhongEdit.TrangThaiPhong;
                trangThaiDb.TrangThaiVeSinh = TrangThaiPhongEdit.TrangThaiVeSinh;

                // Lưu thay đổi vào CSDL
                await _context.SaveChangesAsync();

                // Thành công thì quay về trang Sơ đồ phòng
                return RedirectToPage("/QuanLySoDoPhong/Index");
            }
            catch (Exception ex)
            {
                // Bắt lỗi an toàn
                ModelState.AddModelError(string.Empty, $"Đã xảy ra lỗi khi cập nhật: {ex.Message}");
                return Page();
            }
        }
    }
}