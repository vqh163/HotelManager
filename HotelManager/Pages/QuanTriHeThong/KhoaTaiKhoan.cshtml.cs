using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class KhoaTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public KhoaTaiKhoanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Đối tượng để hiển thị thông tin xác nhận trên giao diện
        [BindProperty]
        public TAI_KHOAN TaiKhoanKhoa { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = "";

        // Hàm chạy khi trang load, lấy thông tin tài khoản dựa vào Mã TK
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            try
            {
                // Dùng LINQ tìm tài khoản trong CSDL
                var tk = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk == null)
                {
                    return RedirectToPage("./QuanLyTaiKhoan");
                }

                TaiKhoanKhoa = tk;
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi truy xuất dữ liệu: " + ex.Message;
            }

            return Page();
        }

        // Hàm xử lý khi Admin bấm nút Xác nhận Khóa
        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            try
            {
                // Tìm lại tài khoản để thao tác
                var tk = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);

                if (tk != null)
                {
                    // Cập nhật trạng thái thành "Khóa" thay vì xóa vật lý
                    tk.TrangThai = "Khóa";

                    // Lưu thay đổi vào SQL Server
                    await _context.SaveChangesAsync();
                }

                // Quay về trang danh sách sau khi khóa thành công
                return RedirectToPage("./QuanLyTaiKhoan");
            }
            catch (Exception ex)
            {
                // Bắt lỗi an toàn
                ThongBaoLoi = "Lỗi hệ thống khi khóa tài khoản: " + ex.Message;

                // Load lại dữ liệu để hiển thị trang lỗi
                var tk = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk != null) TaiKhoanKhoa = tk;

                return Page();
            }
        }
    }
}