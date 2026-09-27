using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using HotelManager.Data; // Thư mục chứa ApplicationDbContext của đồ án

namespace HotelManager.Pages.Auth
{
    public class XacThucOTPModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Tiêm (Inject) ApplicationDbContext vào PageModel để tương tác CSDL
        public XacThucOTPModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Nhận tham số phân loại luồng từ URL (QuenMatKhau hoặc DangKy)
        [BindProperty(SupportsGet = true)]
        public string Loai { get; set; } = string.Empty;

        // Nhận tên tài khoản truyền qua URL
        [BindProperty(SupportsGet = true)]
        public string TaiKhoan { get; set; } = string.Empty;

        // Nhận mã OTP người dùng nhập vào từ Form
        [BindProperty]
        public string MaOTP { get; set; } = string.Empty;

        public string ThongBaoLoi { get; set; } = string.Empty;
        public string ThongBaoThanhCong { get; set; } = string.Empty;

        public void OnGet()
        {
            // Xử lý khi tải trang xác thực OTP lần đầu
        }

        // Xử lý sự kiện POST khi người dùng bấm xác nhận mã OTP
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Kiểm tra tính hợp lệ của tên tài khoản
                if (string.IsNullOrEmpty(TaiKhoan))
                {
                    ThongBaoLoi = "Không xác định được tài khoản. Vui lòng quay lại màn hình trước và thử lại.";
                    return Page();
                }

                string sessionKey = "OTP_" + TaiKhoan;
                // Sử dụng kiểu string? để tránh cảnh báo CS8600 khi session trả về null
                string? savedOtp = HttpContext.Session.GetString(sessionKey);

                // Chấp nhận mã Master "123456" cho mục đích kiểm thử (phát triển) hoặc mã trong Session
                if (MaOTP == "123456" || (!string.IsNullOrEmpty(savedOtp) && savedOtp == MaOTP))
                {
                    // Xóa Session sau khi xác thực thành công để bảo mật
                    HttpContext.Session.Remove(sessionKey);

                    // Phân nhánh theo luồng nghiệp vụ
                    if (Loai == "QuenMatKhau")
                    {
                        // Luồng UC_01: Chuyển hướng sang trang đổi mật khẩu mới
                        return RedirectToPage("/Auth/QuenMatKhau", "ChoPhepDoiMatKhau", new { taiKhoan = TaiKhoan });
                    }
                    else if (Loai == "DangKy")
                    {
                        // Luồng UC_02: Dùng LINQ truy vấn bảng TAI_KHOAN và cập nhật trạng thái thành "Hoạt động"
                        var taiKhoanDb = await _context.TAI_KHOAN
                            .FirstOrDefaultAsync(t => t.TenDangNhap == TaiKhoan);

                        if (taiKhoanDb != null)
                        {
                            taiKhoanDb.TrangThai = "Hoạt động";
                            await _context.SaveChangesAsync(); // Lưu thay đổi vào SQL Server

                            ThongBaoThanhCong = "Kích hoạt tài khoản thành công! Bạn có thể đăng nhập ngay.";
                        }
                        else
                        {
                            ThongBaoLoi = "Lỗi dữ liệu: Không tìm thấy tài khoản để kích hoạt.";
                        }

                        return Page();
                    }
                }

                ThongBaoLoi = "Mã OTP không hợp lệ hoặc đã hết hạn.";
                return Page();
            }
            catch (Exception ex)
            {
                // Bắt ngoại lệ hệ thống nhằm tránh sập ứng dụng
                ThongBaoLoi = "Lỗi hệ thống: " + ex.Message;
                return Page();
            }
        }
    }
}