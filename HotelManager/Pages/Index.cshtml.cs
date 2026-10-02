using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace HotelManager.Pages
{
    public class IndexModel : PageModel
    {
        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            try
            {
                // Xử lý logic tải trang chủ (có thể mở rộng gọi LINQ lấy danh sách phòng ở các Sprint sau)
                // KIỂM TRA TRẠNG THÁI: Tự động điều hướng nhân sự đã đăng nhập
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    if (User.IsInRole("Quản trị viên"))
                    {
                        return RedirectToPage("/QuanTriHeThong/QuanLyTaiKhoan");
                    }
                    else if (User.IsInRole("Quản lý"))
                    {
                        return RedirectToPage("/QuanLyLichCaNhan/QuanLyLichCaNhan");
                    }
                    else if (User.IsInRole("Lễ tân"))
                    {
                        return RedirectToPage("/QuanLyTienSanh/Index");
                    }
                    // Nếu là "Khách hàng" thì bỏ qua if này và tiếp tục load trang chủ
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Bắt lỗi an toàn để ứng dụng không bị sập
                ThongBaoLoi = "Đã xảy ra lỗi khi tải trang chủ: " + ex.Message;
                return Page();
            }
        }
    }
}