using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HotelManager.Pages.Auth
{
    public class DangNhapModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DangNhapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lớp ViewModel nhận dữ liệu từ Form
        public class DangNhapInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
            public string TenDangNhap { get; set; } = default!;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            public string MatKhau { get; set; } = default!;
        }

        [BindProperty]
        public DangNhapInputModel Input { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = "";

        public IActionResult OnGet()
        {
            // Nếu người dùng đã đăng nhập rồi thì chuyển hướng về trang chủ, không cho vào lại form đăng nhập
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Dùng LINQ kiểm tra tài khoản trong CSDL
                var taiKhoan = await _context.TAI_KHOAN
                    .FirstOrDefaultAsync(t => t.TenDangNhap == Input.TenDangNhap && t.MatKhau == Input.MatKhau);

                if (taiKhoan == null)
                {
                    ThongBaoLoi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                    return Page();
                }

                if (taiKhoan.TrangThai == "Khóa" || taiKhoan.TrangThai == "Ngừng hoạt động")
                {
                    ThongBaoLoi = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Quản trị viên!";
                    return Page();
                }

                // TẠO COOKIE AUTHENTICATION (Cấp quyền đăng nhập)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                    new Claim(ClaimTypes.Role, taiKhoan.VaiTro ?? "Khách hàng"),
                    new Claim("MaTaiKhoan", taiKhoan.MaTaiKhoan)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                // Đăng nhập hệ thống
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Phân luồng chuyển hướng dựa theo vai trò (Role)
                if (taiKhoan.VaiTro == "Quản trị viên")
                {
                    return RedirectToPage("/QuanTriHeThong/QuanLyTaiKhoan");
                }

                // Mặc định chuyển về trang chủ
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống trong quá trình đăng nhập: " + ex.Message;
                return Page();
            }
        }
    }
}
