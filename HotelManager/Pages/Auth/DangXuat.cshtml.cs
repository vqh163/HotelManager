using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HotelManager.Pages.Auth
{
    public class DangXuatModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            // Xóa Cookie xác thực của người dùng
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Đẩy về lại trang chủ sau khi đăng xuất thành công
            return RedirectToPage("/Index");
        }
    }
}