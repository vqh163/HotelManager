using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class QuanLyTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Tiêm Dependency Injection (DI) để sử dụng ApplicationDbContext
        public QuanLyTaiKhoanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Chứa danh sách tài khoản để hiển thị ra View
        public IList<TAI_KHOAN> DanhSachTaiKhoan { get; set; } = default!;

        // Biến lưu thông báo lỗi nếu quá trình truy vấn gặp sự cố
        public string ThongBaoLoi { get; set; } = "";

        public async Task OnGetAsync()
        {
            try
            {
                // Sử dụng LINQ lấy toàn bộ dữ liệu từ bảng TAI_KHOAN trong SQL Server
                DanhSachTaiKhoan = await _context.TAI_KHOAN.ToListAsync();
            }
            catch (Exception ex)
            {
                // Bắt lỗi an toàn, tránh để ứng dụng sụp đổ (crash) khi lỗi DB
                ThongBaoLoi = "Đã xảy ra lỗi khi tải dữ liệu tài khoản: " + ex.Message;
            }
        }
    }
}