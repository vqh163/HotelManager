using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
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

        [BindProperty]
        public DangNhapInputModel Input { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = "";

        public IActionResult OnGet()
        {
            // 1. FIX LỖI Ở ĐÂY: Sửa /TrangChu thành /Index
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

                // Chuẩn hóa chuỗi để tránh lỗi khoảng trắng trong CSDL
                string vaiTroNguoiDung = (taiKhoan.VaiTro ?? "Khách hàng").Trim();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTaiKhoan),
                    new Claim(ClaimTypes.Name, taiKhoan.TenDangNhap),
                    new Claim(ClaimTypes.Role, vaiTroNguoiDung)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // 2. FIX LỖI Ở ĐÂY: Trả tất cả các fallback về /Index
                switch (vaiTroNguoiDung)
                {
                    case "Quản trị viên":
                        // Trỏ đến module Quản trị hệ thống (UC_13)
                        return RedirectToPage("/QuanTriHeThong/Index");

                    case "Quản lý":
                        // Trỏ đến module Quản lý lịch cá nhân (UC_10)
                        return RedirectToPage("/QuanLyLichCaNhan/QuanLyLichCaNhan");

                    case "Lễ tân":
                        // Trỏ đến module Tiền sảnh (UC_04)
                        return RedirectToPage("/QuanLyTienSanh/Index");

                    case "Khách hàng":
                        return RedirectToPage("/Index");

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