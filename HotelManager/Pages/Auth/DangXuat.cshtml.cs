using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace HotelManager.Pages.Auth
{
    public class DangXuatModel : PageModel
    {
        // Hàm OnPostAsync xử lý khi người dùng bấm nút Đăng xuất từ _Layout
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Xóa hoàn toàn Cookie Authentication của hệ thống
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Điều hướng về trang chủ mặc định
                return RedirectToPage("/Index");
            }
            catch (Exception)
            {
                // Xử lý an toàn nếu có lỗi bất ngờ khi xóa Cookie
                return RedirectToPage("/Index");
            }
        }

        // Đề phòng trường hợp người dùng gõ trực tiếp URL /Auth/DangXuat
        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }
    }
}