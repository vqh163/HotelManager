using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using HotelManager.Data;

namespace HotelManager.Pages.Auth
{
    public class XacThucOTPModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<XacThucOTPModel> _logger;

        public XacThucOTPModel(ApplicationDbContext context, ILogger<XacThucOTPModel> logger)
        {
            _context = context;
            _logger = logger;
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
        }

        // Xử lý sự kiện POST khi người dùng bấm xác nhận mã OTP
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(Loai))
                {
                    ThongBaoLoi = "Không xác định được phiên xác thực tài khoản. Vui lòng quay lại màn hình trước và thử lại.";
                    return Page();
                }

                string taiKhoanChuan = TaiKhoan.Trim();
                string sessionKey = $"OTP_DATA_{taiKhoanChuan}";
                string? otpPayload = HttpContext.Session.GetString(sessionKey);

                // Bắt buộc phải có phiên OTP hợp lệ đã được khởi tạo từ bước Đăng ký / Quên mật khẩu
                if (string.IsNullOrEmpty(otpPayload))
                {
                    ThongBaoLoi = "Phiên mã OTP không tồn tại hoặc đã hết hạn. Vui lòng gửi lại yêu cầu mới!";
                    return Page();
                }

                var parts = otpPayload.Split('|');
                if (parts.Length < 4
                    || !string.Equals(parts[1], Loai, StringComparison.OrdinalIgnoreCase)
                    || !DateTime.TryParse(parts[2], null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiresUtc)
                    || !int.TryParse(parts[3], out int soLanThu))
                {
                    HttpContext.Session.Remove(sessionKey);
                    ThongBaoLoi = "Dữ liệu phiên OTP không hợp lệ. Vui lòng gửi lại yêu cầu mới!";
                    return Page();
                }

                // Kiểm tra hết hạn (5 phút)
                if (DateTime.UtcNow > expiresUtc)
                {
                    HttpContext.Session.Remove(sessionKey);
                    ThongBaoLoi = "Mã OTP đã hết hạn sử dụng. Vui lòng gửi lại yêu cầu mới!";
                    return Page();
                }

                // Giới hạn tối đa 5 lần nhập sai chống dò mã
                if (soLanThu >= 5)
                {
                    HttpContext.Session.Remove(sessionKey);
                    ThongBaoLoi = "Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại yêu cầu cấp mã mới!";
                    return Page();
                }

                string savedOtp = parts[0];
                string otpNhap = (MaOTP ?? string.Empty).Trim();

                // Chấp nhận mã OTP trong Session hoặc mã demo 123456 (chỉ khi phiên OTP đã khởi tạo hợp lệ ở trên)
                if (!string.IsNullOrEmpty(otpNhap) && (otpNhap == savedOtp || otpNhap == "123456"))
                {
                    // Xóa phiên OTP sau khi dùng để chống sử dụng lại
                    HttpContext.Session.Remove(sessionKey);

                    if (Loai == "QuenMatKhau")
                    {
                        // Ghi cờ xác thực phía Server trong Session (hiệu lực 10 phút) cho bước đặt lại mật khẩu
                        string resetExp = DateTime.UtcNow.AddMinutes(10).ToString("O");
                        HttpContext.Session.SetString("DA_XAC_THUC_OTP_RESET", $"{taiKhoanChuan}|{resetExp}");
                        return RedirectToPage("/Auth/QuenMatKhau", "ChoPhepDoiMatKhau");
                    }
                    else if (Loai == "DangKy")
                    {
                        var taiKhoanDb = await _context.TAI_KHOAN
                            .FirstOrDefaultAsync(t => t.TenDangNhap == taiKhoanChuan);

                        if (taiKhoanDb != null)
                        {
                            taiKhoanDb.TrangThai = "Hoạt động";
                            await _context.SaveChangesAsync();
                            ThongBaoThanhCong = "Kích hoạt tài khoản thành công! Bạn có thể đăng nhập ngay.";
                        }
                        else
                        {
                            ThongBaoLoi = "Không tìm thấy tài khoản cần kích hoạt.";
                        }
                        return Page();
                    }
                }

                // Tăng bộ đếm số lần nhập sai
                soLanThu++;
                HttpContext.Session.SetString(sessionKey, $"{savedOtp}|{parts[1]}|{parts[2]}|{soLanThu}");
                ThongBaoLoi = $"Mã OTP không chính xác! (Còn {Math.Max(0, 5 - soLanThu)} lần thử)";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi xác thực OTP.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi xác thực OTP. Vui lòng thử lại!";
                return Page();
            }
        }
    }
}
