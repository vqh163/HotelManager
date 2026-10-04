using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class QuanLyTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyTaiKhoanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách tài khoản trả về cho HTML
        public IList<TAI_KHOAN> DanhSachTaiKhoan { get; set; } = new List<TAI_KHOAN>();

        // Thuộc tính tìm kiếm
        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Truy vấn tối ưu bằng AsNoTracking
                var query = _context.TAI_KHOAN.AsNoTracking().AsQueryable();

                // Xử lý tìm kiếm bằng LINQ
                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    query = query.Where(t =>
                        (t.MaTaiKhoan != null && t.MaTaiKhoan.Contains(TuKhoaTimKiem)) ||
                        (t.TenDangNhap != null && t.TenDangNhap.Contains(TuKhoaTimKiem))
                    );
                }

                // Sắp xếp và lấy danh sách
                DanhSachTaiKhoan = await query.OrderBy(t => t.MaTaiKhoan).ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi truy xuất dữ liệu: " + ex.Message);
                return Page();
            }
        }
    }
}