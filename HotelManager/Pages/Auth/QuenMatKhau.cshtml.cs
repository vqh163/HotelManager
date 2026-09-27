using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
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

        // Đóng gói dữ liệu đầu vào với BindProperty
        [BindProperty]
        public ForgotPasswordInput Input { get; set; } = new ForgotPasswordInput();

        [BindProperty]
        public int BuocHienTai { get; set; } = 1;

        public string ThongBaoLoi { get; set; } = string.Empty;
        public string ThongBaoThanhCong { get; set; } = string.Empty;

        public class ForgotPasswordInput
        {
            public string ThongTinKhoiPhuc { get; set; } = string.Empty;
            public string MatKhauMoi { get; set; } = string.Empty;
            public string XacNhanMatKhau { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            BuocHienTai = 1;
        }

        // Hứng luồng từ XacThucOTP trả về
        public void OnGetChoPhepDoiMatKhau(string taiKhoan)
        {
            BuocHienTai = 3;
            Input.ThongTinKhoiPhuc = taiKhoan;
            ThongBaoThanhCong = "Xác thực OTP thành công! Vui lòng đặt lại mật khẩu mới.";
        }

        // BƯỚC 1: Xử lý khi nhấn nút "Gửi Yêu Cầu"
        public IActionResult OnPostGuiYeuCau()
        {
            try
            {
                // Dùng LINQ thực hiện LEFT JOIN bảng TAI_KHOAN và KHACH_HANG để quét cả Tên đăng nhập, Email và SĐT
                var user = (from tk in _context.TAI_KHOAN
                            join kh in _context.KHACH_HANG on tk.KHACH_HANGMaKH equals kh.MaKH into tk_kh
                            from kh in tk_kh.DefaultIfEmpty()
                            where tk.TenDangNhap == Input.ThongTinKhoiPhuc ||
                                  (kh != null && (kh.Email == Input.ThongTinKhoiPhuc || kh.SDT == Input.ThongTinKhoiPhuc))
                            select tk).FirstOrDefault();

                if (user != null)
                {
                    // Lấy Tên đăng nhập gốc của hệ thống dù khách hàng có nhập Email hay SĐT
                    string tenDangNhapChuan = user.TenDangNhap;

                    // Mã OTP mặc định là 123456 theo yêu cầu test
                    string generatedOtp = "123456";

                    // Lưu OTP vào Session theo Tên đăng nhập chuẩn
                    HttpContext.Session.SetString("OTP_" + tenDangNhapChuan, generatedOtp);

                    // Chuyển hướng sang trang xác thực
                    return RedirectToPage("/Auth/XacThucOTP", new
                    {
                        Loai = "QuenMatKhau",
                        TaiKhoan = tenDangNhapChuan
                    });
                }

                ThongBaoLoi = "Tài khoản không tồn tại trong hệ thống.";
                BuocHienTai = 1;
                return Page();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống: " + ex.Message;
                BuocHienTai = 1;
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

                // Cập nhật Database dựa vào Tên đăng nhập
                var user = _context.TAI_KHOAN.FirstOrDefault(t => t.TenDangNhap == Input.ThongTinKhoiPhuc);
                if (user != null)
                {
                    user.MatKhau = Input.MatKhauMoi; // Thực tế cần hash mật khẩu
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
