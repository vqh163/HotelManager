using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

        public DangNhapModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class DangNhapInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
            public string TenDangNhap { get; set; } = default!;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            public string MatKhau { get; set; } = default!;
        }

        // Thuộc tính Binding nhận dữ liệu từ Form đăng nhập
        [BindProperty]
        public DangNhapInputModel Input { get; set; } = default!;

        // Thuộc tính hứng đường dẫn ReturnUrl qua phương thức GET
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string ThongBaoLoi { get; set; } = "";

        // Hàm phụ trợ: Băm mật khẩu chuẩn SHA-256 Base64 (Đồng bộ với ThemTaiKhoan và SuaTaiKhoan)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            // Nếu đã đăng nhập thì chuyển thẳng về Trang chủ
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
                string tenDangNhapChuan = Input.TenDangNhap.Trim();
                string matKhauNhap = Input.MatKhau;
                string matKhauDaBam = HashPassword(matKhauNhap);

                // 1. Truy vấn LINQ: Kiểm tra tài khoản khớp mật khẩu đã Hash SHA-256 hoặc mật khẩu gốc
                var taiKhoan = await _context.TAI_KHOAN
                    .FirstOrDefaultAsync(t => t.TenDangNhap.ToLower() == tenDangNhapChuan.ToLower() &&
                                             (t.MatKhau == matKhauDaBam || t.MatKhau == matKhauNhap));

                if (taiKhoan == null)
                {
                    ThongBaoLoi = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                    return Page();
                }

                // 2. Kiểm tra trạng thái khóa của tài khoản
                if (taiKhoan.TrangThai == "Khóa" || taiKhoan.TrangThai == "Ngừng hoạt động")
                {
                    ThongBaoLoi = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Quản trị viên!";
                    return Page();
                }

                // 3. Tự động nâng cấp mật khẩu Plaintext cũ lên chuẩn Hash SHA-256
                if (taiKhoan.MatKhau == matKhauNhap && taiKhoan.MatKhau != matKhauDaBam)
                {
                    taiKhoan.MatKhau = matKhauDaBam;
                    _context.TAI_KHOAN.Update(taiKhoan);
                    await _context.SaveChangesAsync();
                }

                string vaiTroNguoiDung = (taiKhoan.VaiTro ?? "Khách hàng").Trim();

                // 4. Thiết lập Claims cho Cookie Authentication
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
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // 5. Điều hướng về ReturnUrl nếu hợp lệ
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return LocalRedirect(ReturnUrl);
                }

                // 6. Điều hướng theo Vai trò người dùng (Mục 1.2.5)
                switch (vaiTroNguoiDung)
                {
                    case "Quản trị viên":
                    case "Quản trị viên hệ thống":
                    case "Giám đốc":
                        return RedirectToPage("/QuanTriHeThong/Index");

                    case "Quản lý":
                        return RedirectToPage("/QuanLyLichCaNhan/QuanLyLichCaNhan");

                    case "Lễ tân":
                        return RedirectToPage("/QuanLyTienSanh/Index");

                    case "Sales":
                    case "Nhân viên Buồng phòng":
                        return RedirectToPage("/QuanLySoDoPhong/SoDoPhong");

                    case "Đầu bếp":
                        return RedirectToPage("/QuanLyNhaHang/QuanLyThucDon");

                    case "Phục vụ Nhà hàng":
                        return RedirectToPage("/QuanLyNhaHang/SoDoBan");

                    case "Thu ngân Nhà hàng":
                        return RedirectToPage("/QuanLyNhaHang/QuayThuNgan");

                    case "Nhân viên Kỹ thuật":
                    case "Thủ kho":
                    case "Kế toán":
                        return RedirectToPage("/QuanLyLichCaNhan/QuanLyLichCaNhan");

                    case "Khách hàng":
                        return RedirectToPage("/QuanLySoDoPhong/TimPhong");

                    default:
                        return RedirectToPage("/Index");
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống trong quá trình đăng nhập: " + ex.Message;
                return Page();
            }
        }
    }
}