using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using System.ComponentModel.DataAnnotations;

namespace HotelManager.Pages.Auth
{
    public class XacThucOTPModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public XacThucOTPModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Bắt giá trị Username từ Query String trên URL
        [BindProperty(SupportsGet = true)]
        public string TenDangNhap { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        public string MaOTP { get; set; } = string.Empty;

        public string ThongBaoLoi { get; set; } = "";
        public string ThongBaoThanhCong { get; set; } = "";

        public void OnGet()
        {
            // Trong thực tế, hệ thống sẽ gọi API gửi SMS hoặc dùng thư viện MailKit gửi Email chứa OTP ở đây.
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Truy vấn bảng TAI_KHOAN thông qua LINQ
                var taiKhoan = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.TenDangNhap == TenDangNhap);

                if (taiKhoan == null)
                {
                    ThongBaoLoi = "Không tìm thấy tài khoản trong hệ thống!";
                    return Page();
                }

                // (Dành cho việc test nội bộ: Thiết lập cứng mã OTP là 123456)
                if (MaOTP != "123456")
                {
                    ThongBaoLoi = "Mã OTP không chính xác. Vui lòng kiểm tra lại Email/Tin nhắn!";
                    return Page();
                }

                // Cập nhật trạng thái tài khoản từ "Chờ xác thực" sang "Hoạt động"
                taiKhoan.TrangThai = "Hoạt động";
                await _context.SaveChangesAsync();

                ThongBaoThanhCong = "Xác thực tài khoản thành công!";
                return Page(); // Hiển thị thông báo thành công và ẩn form
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi xác thực: " + ex.Message;
                return Page();
            }
        }
    }
}