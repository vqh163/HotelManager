using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
// using HotelManager.Data; // Uncomment khi kết nối CSDL
using System;

namespace HotelManager.Pages.QuanLyLichCaNhan
{
    public class QuanLyLichCaNhanModel : PageModel
    {
        // Khai báo DbContext ở đây để truy vấn bảng LICH_LAM_VIEC (UC_10)

        public IActionResult OnGet()
        {
            try
            {
                // 1. Kiểm tra vòng ngoài: Chưa đăng nhập thì đá về Trang đăng nhập
                if (User.Identity == null || !User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Auth/DangNhap");
                }

                // 2. Chặn Admin: UC_10 chỉ dành cho Nhân viên, Lễ tân, Quản lý, Buồng phòng...
                if (User.IsInRole("Quản trị viên"))
                {
                    TempData["ThongBaoLoi"] = "Quản trị viên không tham gia vào ca làm việc vận hành.";
                    return RedirectToPage("/QuanTriHeThong/QuanLyTaiKhoan");
                }

                // 3. Logic truy xuất bảng LICH_LAM_VIEC của bạn Ký sẽ viết bằng LINQ tại đây
                // VD: Lấy danh sách lịch làm việc của User đang đăng nhập...

                return Page();
            }
            catch (Exception ex)
            {
                TempData["ThongBaoLoi"] = "Lỗi tải lịch làm việc: " + ex.Message;
                return RedirectToPage("/Index");
            }
        }
    }
}