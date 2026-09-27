using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace HotelManager.Pages
{
    public class TrangChuModel : PageModel
    {
        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            try
            {
                // Xử lý logic tải trang chủ (có thể mở rộng gọi LINQ lấy danh sách phòng ở các Sprint sau)
                return Page();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Đã xảy ra lỗi khi tải trang chủ: " + ex.Message;
                return Page();
            }
        }
    }
}