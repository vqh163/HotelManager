using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class NhatKyHeThongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public NhatKyHeThongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách chứa dữ liệu nhật ký
        public IList<NHAT_KY_HE_THONG> DanhSachNhatKy { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Dùng LINQ truy xuất dữ liệu tối ưu với AsNoTracking và giới hạn 200 bản ghi gần nhất
            DanhSachNhatKy = await _context.NHAT_KY_HE_THONG
                .AsNoTracking()
                .OrderByDescending(n => n.ThoiGian)
                .Take(200)
                .ToListAsync();
        }
    }
}