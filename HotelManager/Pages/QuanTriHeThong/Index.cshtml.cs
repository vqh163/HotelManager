using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Các thuộc tính Binding hiển thị số liệu thống kê lên giao diện
        public int TongSoTaiKhoan { get; set; }
        public int SoLuotThaoTacHomNay { get; set; }

        public async Task OnGetAsync()
        {
            // Dùng LINQ đếm tổng số tài khoản đang có trong hệ thống
            TongSoTaiKhoan = await _context.TAI_KHOAN.CountAsync();

            // Dùng LINQ đếm số lượng thao tác (Audit Trail) sinh ra trong ngày hôm nay
            SoLuotThaoTacHomNay = await _context.NHAT_KY_HE_THONG
                .Where(log => log.ThoiGian.Date == DateTime.Today)
                .CountAsync();
        }
    }
}