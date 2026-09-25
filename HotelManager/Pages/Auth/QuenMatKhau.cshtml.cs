using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http; // BẮT BUỘC THÊM: Thư viện để dùng Session
using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;

namespace HotelManager.Pages.Auth
{
    public class QuenMatKhauModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuenMatKhauModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ForgotPasswordInput Input { get; set; } = new ForgotPasswordInput();

        [BindProperty]
        public int BuocHienTai { get; set; } = 1;

        public string ThongBaoLoi { get; set; } = string.Empty;
        public string ThongBaoThanhCong { get; set; } = string.Empty;

        public class ForgotPasswordInput
        {
            public string ThongTinKhoiPhuc { get; set; } = string.Empty;
            public string MaOTP { get; set; } = string.Empty;
            public string MatKhauMoi { get; set; } = string.Empty;
            public string XacNhanMatKhau { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            BuocHienTai = 1;
        }

        // BƯỚC 1: Xử lý khi nhấn nút "Gửi Yêu Cầu"
        public async Task<IActionResult> OnPostGuiYeuCauAsync()
        {
            try
            {
                // Dùng LINQ truy vấn bảng TAI_KHOAN theo tài liệu thiết kế[cite: 15]
                var user = _context.TAI_KHOAN.FirstOrDefault(t => t.TenDangNhap == Input.ThongTinKhoiPhuc);

                if (user != null)
                {
                    // 1. Tạo mã OTP ngẫu nhiên 6 số
                    Random rnd = new Random();
                    string generatedOtp = rnd.Next(100000, 999999).ToString();

                    // 2. Lưu OTP vào Session, gắn kèm Tên đăng nhập để tránh nhầm lẫn
                    HttpContext.Session.SetString("OTP_" + Input.ThongTinKhoiPhuc, generatedOtp);

                    // GHI CHÚ: Trong thực tế, ở đây sẽ gọi thư viện gửi Email/SMS chứa mã OTP.
                    // Để nhóm dễ test luồng, anh hiển thị luôn mã OTP lên thông báo.
                    ThongBaoThanhCong = $"Đã gửi mã xác thực. (Dành cho Test - Mã OTP của bạn là: {generatedOtp})";

                    BuocHienTai = 2; // Chuyển sang Bước 2
                }
                else
                {
                    ThongBaoLoi = "Tài khoản không tồn tại trong hệ thống.";
                    BuocHienTai = 1;
                }
                return Page();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống: " + ex.Message;
                BuocHienTai = 1;
                return Page();
            }
        }

        // BƯỚC 2: Xử lý khi nhấn nút "Xác nhận OTP"
        public IActionResult OnPostXacNhanOTP()
        {
            try
            {
                // 1. Lấy mã OTP đã lưu trong Session ra
                string savedOtp = HttpContext.Session.GetString("OTP_" + Input.ThongTinKhoiPhuc);

                // 2. So sánh mã người dùng nhập (Input.MaOTP) với mã trong Session
                if (!string.IsNullOrEmpty(savedOtp) && savedOtp == Input.MaOTP)
                {
                    // Trùng khớp -> Sang Bước 3
                    ThongBaoThanhCong = "Xác nhận OTP thành công. Vui lòng nhập mật khẩu mới.";
                    BuocHienTai = 3;

                    // Xóa OTP khỏi Session để đảm bảo an toàn, không cho dùng lại
                    HttpContext.Session.Remove("OTP_" + Input.ThongTinKhoiPhuc);
                }
                else
                {
                    // Sai OTP
                    ThongBaoLoi = "Mã OTP không hợp lệ hoặc đã hết hạn.";
                    BuocHienTai = 2; // Giữ lại ở Bước 2 để nhập lại
                }
                return Page();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống: " + ex.Message;
                BuocHienTai = 2;
                return Page();
            }
        }

        // BƯỚC 3: Xử lý khi nhấn nút "Đổi Mật Khẩu"
        public async Task<IActionResult> OnPostDoiMatKhauAsync()
        {
            try
            {
                if (Input.MatKhauMoi != Input.XacNhanMatKhau)
                {
                    ThongBaoLoi = "Mật khẩu xác nhận không khớp.";
                    BuocHienTai = 3;
                    return Page();
                }

                // Cập nhật CSDL[cite: 15]
                var user = _context.TAI_KHOAN.FirstOrDefault(t => t.TenDangNhap == Input.ThongTinKhoiPhuc);
                if (user != null)
                {
                    user.MatKhau = Input.MatKhauMoi;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
                    return RedirectToPage("/Auth/DangNhap");
                }

                ThongBaoLoi = "Lỗi xác thực dữ liệu người dùng.";
                BuocHienTai = 1;
                return Page();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống: " + ex.Message;
                BuocHienTai = 3;
                return Page();
            }
        }
    }
}