using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using HotelManager.Data; // Đảm bảo đúng thư mục DbContext của dự án

namespace HotelManager.Pages.Auth
{
    public class XacThucOTPModel : PageModel
    {
        // 1. Khai báo DbContext để tương tác với cơ sở dữ liệu
        private readonly ApplicationDbContext _context;

        public XacThucOTPModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Loai { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string TaiKhoan { get; set; } = string.Empty;

        [BindProperty]
        public string MaOTP { get; set; } = string.Empty;

        public string ThongBaoLoi { get; set; } = string.Empty;
        public string ThongBaoThanhCong { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        // Đổi thành async Task<IActionResult> để dùng await với EF Core
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(TaiKhoan))
                {
                    ThongBaoLoi = "Không xác định được tài khoản. Vui lòng quay lại màn hình trước và thử lại.";
                    return Page();
                }

                string sessionKey = "OTP_" + TaiKhoan;
                string savedOtp = HttpContext.Session.GetString(sessionKey);

                // Chấp nhận mã Master 123456 hoặc mã chuẩn trong Session
                if (MaOTP == "123456" || (!string.IsNullOrEmpty(savedOtp) && savedOtp == MaOTP))
                {
                    // Xóa Session sau khi xác thực
                    HttpContext.Session.Remove(sessionKey);

                    if (Loai == "QuenMatKhau")
                    {
                        // Luồng UC_01: Chuyển về trang đổi mật khẩu
                        return RedirectToPage("/Auth/QuenMatKhau", "ChoPhepDoiMatKhau", new { taiKhoan = TaiKhoan });
                    }
                    else if (Loai == "DangKy")
                    {
                        // Luồng UC_02: Cập nhật trạng thái tài khoản trong Database thành "Hoạt động"
                        var taiKhoanDb = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.TenDangNhap == TaiKhoan);

                        if (taiKhoanDb != null)
                        {
                            // Thay đổi trạng thái
                            taiKhoanDb.TrangThai = "Hoạt động";
                            await _context.SaveChangesAsync(); // Lưu vào SQL Server

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
                ThongBaoLoi = "Lỗi hệ thống: " + ex.Message;
                return Page();
            }
        }
    }
}
