using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class TimPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TimPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<PHONG> DanhSachPhong { get; set; } = new List<PHONG>();

        [BindProperty(SupportsGet = true)]
        public DateTime? NgayNhan { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? NgayTra { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SoKhach { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? HangPhong { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var query = _context.PHONG.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(HangPhong))
                {
                    query = query.Where(p => p.LoaiPhong == HangPhong);
                }

                if (SoKhach.HasValue && SoKhach.Value > 0)
                {
                    query = query.Where(p => p.SucChuaToiDa >= SoKhach.Value);
                }

                // Chỉ hiển thị phòng có trạng thái Trống (Giả định mã là TT01)
                query = query.Where(p => p.MaTrangThai == "TT01");

                DanhSachPhong = await query.OrderBy(p => p.SoPhong).ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tìm kiếm: " + ex.Message);
                return Page();
            }
        }
    }
}