using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class ChiTietPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ChiTietPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Đối tượng chứa thông tin phòng bám sát bảng PHONG trong CSDL
        public PHONG PhongChiTiet { get; set; } = new PHONG();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage("/Index");

            try
            {
                // Truy vấn thông tin phòng theo mã phòng
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == id);

                if (phong == null) return NotFound();

                PhongChiTiet = phong;
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải chi tiết phòng: " + ex.Message);
                return Page();
            }
        }
    }
}