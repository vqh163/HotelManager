using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Thư viện bắt buộc để phân quyền
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Yêu cầu người dùng phải đăng nhập VÀ có VaiTro là "Quản lý" mới được truy cập trang này
    [Authorize(Roles = "Quản lý")]
    public class QuanLyPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<PHONG> DanhSachPhong { get; set; } = new List<PHONG>();

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var query = _context.PHONG.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    query = query.Where(p => p.SoPhong.Contains(TuKhoaTimKiem) || p.LoaiPhong.Contains(TuKhoaTimKiem));
                }

                DanhSachPhong = await query.OrderBy(p => p.SoPhong).ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tải dữ liệu: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostXoaAsync(string id)
        {
            try
            {
                var phong = await _context.PHONG.FindAsync(id);
                if (phong != null)
                {
                    _context.PHONG.Remove(phong);
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi xóa phòng: {ex.Message}");
                DanhSachPhong = await _context.PHONG.AsNoTracking().ToListAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostKhoaAsync(string id)
        {
            try
            {
                var phong = await _context.PHONG.FindAsync(id);
                if (phong != null)
                {
                    phong.TRANG_THAI_PHONGMaTrangThai = "LOCKED";
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi khóa phòng: {ex.Message}");
                DanhSachPhong = await _context.PHONG.AsNoTracking().ToListAsync();
                return Page();
            }
        }
    }
}