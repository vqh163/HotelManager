using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class ThemBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ThemBanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Bắt buộc sử dụng [BindProperty] để tự động map dữ liệu từ Form HTML vào Object
        [BindProperty]
        public BAN_NHA_HANG BanNhaHang { get; set; } = new BAN_NHA_HANG();

        public void OnGet()
        {
            // Khởi tạo đối tượng với trạng thái mặc định là "Trống" khi người dùng mới mở form
            BanNhaHang.TrangThai = "Trống";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Kiểm tra tính hợp lệ của dữ liệu đầu vào (Validation)
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Sử dụng LINQ kiểm tra xem mã bàn đã tồn tại trong CSDL chưa
                bool isExist = await _context.BAN_NHA_HANG.AnyAsync(b => b.MaBan == BanNhaHang.MaBan);
                if (isExist)
                {
                    ModelState.AddModelError("BanNhaHang.MaBan", "Mã bàn này đã tồn tại. Vui lòng nhập mã khác.");
                    return Page();
                }

                // Thêm đối tượng bàn mới vào DbContext
                _context.BAN_NHA_HANG.Add(BanNhaHang);

                // Lưu thay đổi xuống cơ sở dữ liệu SQL Server
                await _context.SaveChangesAsync();

                // Lưu thành công, điều hướng quay lại trang Sơ đồ bàn.
                // Hệ thống sẽ tự động nhận diện Trạng thái bạn vừa chọn và hiển thị màu sắc/nút bấm tương ứng trên sơ đồ.
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                // Bọc try...catch theo chuẩn dự án để hiển thị thông báo an toàn
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi lưu dữ liệu: " + ex.Message);
                return Page();
            }
        }
    }
}