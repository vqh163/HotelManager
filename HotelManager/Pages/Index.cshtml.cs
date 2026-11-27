using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HotelManager.Helpers;
using System.Security.Claims;

namespace HotelManager.Pages
{
    public class IndexModel : PageModel
    {
        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            // Tự động điều hướng nhân sự nội bộ sang phân hệ tương ứng (Khách hàng giữ lại trên Trang chủ)
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(role) && role != HangSoHeThong.VaiTroKhachHang)
                {
                    string trangDich = HangSoHeThong.LayTrangMacDinhTheoVaiTro(role);
                    if (trangDich != "/Index")
                    {
                        return RedirectToPage(trangDich);
                    }
                }
            }

            return Page();
        }
    }
}
