using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class XoaTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public XoaTaiKhoanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dữ liệu tài khoản cần xóa hiển thị ra màn hình để Admin xác nhận lại
        [BindProperty]
        public TAI_KHOAN TaiKhoanXoa { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = "";

        // Hàm chạy khi trang load, lấy thông tin tài khoản dựa vào Mã TK trên URL
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            try
            {
                // Dùng LINQ tìm tài khoản
                var tk = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk == null)
                {
                    return RedirectToPage("./QuanLyTaiKhoan");
                }

                TaiKhoanXoa = tk;
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi truy xuất dữ liệu: " + ex.Message;
            }

            return Page();
        }

        // Hàm xử lý thao tác xóa khi Admin bấm nút Xác nhận Xóa
        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            try
            {
                // Tìm lại tài khoản một lần nữa trước khi xóa để đảm bảo an toàn
                var tk = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);

                if (tk != null)
                {
                    // Xóa vĩnh viễn khỏi CSDL
                    _context.TAI_KHOAN.Remove(tk);
                    await _context.SaveChangesAsync();
                }

                // Quay về trang danh sách sau khi xóa thành công
                return RedirectToPage("./QuanLyTaiKhoan");
            }
            catch (Exception ex)
            {
                // Bắt lỗi an toàn, đặc biệt hữu ích nếu sau này có ràng buộc khóa ngoại (Foreign Key)
                ThongBaoLoi = "Không thể xóa tài khoản này. Có thể do lỗi ràng buộc dữ liệu: " + ex.Message;

                // Load lại dữ liệu để hiển thị trang lỗi
                var tk = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk != null) TaiKhoanXoa = tk;

                return Page();
            }
        }
    }
}