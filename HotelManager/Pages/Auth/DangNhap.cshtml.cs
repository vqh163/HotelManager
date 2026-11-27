using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.Auth
{
    public class DangNhapModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DangNhapModel> _logger;

        public DangNhapModel(ApplicationDbContext context, ILogger<DangNhapModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class DangNhapInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
            public string TenDangNhap { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            public string MatKhau { get; set; } = string.Empty;
        }

        // Thuộc tính Binding nhận dữ liệu từ Form đăng nhập
        [BindProperty]
        public DangNhapInputModel Input { get; set; } = new DangNhapInputModel();

        // Thuộc tính hứng đường dẫn ReturnUrl qua phương thức GET
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string ThongBaoLoi { get; set; } = string.Empty;

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            // Nếu đã đăng nhập thì chuyển thẳng về trang chủ theo vai trò
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                return RedirectToPage(HangSoHeThong.LayTrangMacDinhTheoVaiTro(role));
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
                string tenDangNhapChuan = Input.TenDangNhap.Trim();
                string matKhauNhap = Input.MatKhau;

                // 1. Truy vấn LINQ theo tên đăng nhập trước để xác thực mật khẩu an toàn (chặn Pass-the-Hash)
                var taiKhoan = await _context.TAI_KHOAN
                    .FirstOrDefaultAsync(t => t.TenDangNhap.ToLower() == tenDangNhapChuan.ToLower());

                if (taiKhoan == null || !BaoMatHelper.XacThucMatKhau(taiKhoan.MatKhau, matKhauNhap, out bool canNangCapHash))
                {
                    ThongBaoLoi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                    return Page();
                }

                // 2. Kiểm tra trạng thái hoạt động theo danh sách cho phép (Allowlist)
                string trangThai = (taiKhoan.TrangThai ?? string.Empty).Trim();
                if (trangThai.Equals("Chờ xác thực", StringComparison.OrdinalIgnoreCase))
                {
                    ThongBaoLoi = "Tài khoản của bạn chưa được kích hoạt OTP. Vui lòng xác thực OTP trước khi đăng nhập!";
                    return Page();
                }

                if (!trangThai.Equals("Hoạt động", StringComparison.OrdinalIgnoreCase))
                {
                    ThongBaoLoi = "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ Quản trị viên!";
                    return Page();
                }

                // 3. Tự động nâng cấp mật khẩu Plaintext / Hex cũ lên chuẩn SHA-256 Base64
                if (canNangCapHash)
                {
                    taiKhoan.MatKhau = BaoMatHelper.BamMatKhauSHA256(matKhauNhap);
                    _context.TAI_KHOAN.Update(taiKhoan);
                }

                // Ghi nhận nhật ký đăng nhập vào NHAT_KY_HE_THONG
                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                    TenDangNhap = taiKhoan.TenDangNhap,
                    HanhDong = "Đăng nhập hệ thống",
                    ChiTiet = $"Đăng nhập thành công với vai trò: {taiKhoan.VaiTro}",
                    ThoiGian = DateTime.Now
                });
                await _context.SaveChangesAsync();

                string vaiTroNguoiDung = (taiKhoan.VaiTro ?? HangSoHeThong.VaiTroKhachHang).Trim();

                // 4. Thiết lập Claims cho Cookie Authentication tự code
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTaiKhoan),
                    new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                    new Claim(ClaimTypes.Role, vaiTroNguoiDung)
                };

                if (!string.IsNullOrWhiteSpace(taiKhoan.NHAN_VIENMaNV))
                {
                    claims.Add(new Claim("MaNV", taiKhoan.NHAN_VIENMaNV));
                }
                if (!string.IsNullOrWhiteSpace(taiKhoan.KHACH_HANGMaKH))
                {
                    claims.Add(new Claim("MaKH", taiKhoan.KHACH_HANGMaKH));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // 5. Điều hướng về ReturnUrl nếu hợp lệ (nội bộ)
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return LocalRedirect(ReturnUrl);
                }

                // 6. Điều hướng chuẩn theo Vai trò người dùng
                return RedirectToPage(HangSoHeThong.LayTrangMacDinhTheoVaiTro(vaiTroNguoiDung));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống trong quá trình đăng nhập.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống trong quá trình đăng nhập. Vui lòng thử lại sau!";
                return Page();
            }
        }
    }
}
