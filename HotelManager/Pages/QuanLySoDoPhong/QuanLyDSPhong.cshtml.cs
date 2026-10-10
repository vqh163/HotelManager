using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class QuanLyDSPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyDSPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<PHONG> DanhSachPhong { get; set; } = new List<PHONG>();

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var query = _context.PHONG.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    // Xử lý an toàn cảnh báo CS8602 bằng toán tử !
                    query = query.Where(p => 
                        (p.SoPhong != null && p.SoPhong.Contains(TuKhoaTimKiem!)) || 
                        (p.LoaiPhong != null && p.LoaiPhong.Contains(TuKhoaTimKiem!))
                    );
                }

                if (!string.IsNullOrEmpty(TrangThaiLoc))
                {
                    // ĐÃ SỬA: Gọi đúng tên thuộc tính MaTrangThai theo model PHONG.cs mới
                    query = query.Where(p => p.MaTrangThai == TrangThaiLoc); 
                }

                DanhSachPhong = await query.OrderBy(p => p.SoPhong).ToListAsync();
                
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải danh sách phòng: " + ex.Message);
                return Page();
            }
        }
        public async Task<IActionResult> OnGetKhoaAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            try
            {
                var phong = await _context.PHONG.FindAsync(id);
                if (phong != null)
                {
                    // Kiểm tra: Nếu phòng đang ở trạng thái bảo trì/khóa (TT04) thì mở khóa về trống (TT01), 
                    // ngược lại nếu đang ở trạng thái khác thì chuyển sang khóa/bảo trì (TT04).
                    phong.MaTrangThai = phong.MaTrangThai == "TT04" ? "TT01" : "TT04";

                    _context.PHONG.Update(phong);
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi thay đổi trạng thái khóa phòng: " + ex.Message);
                DanhSachPhong = await _context.PHONG.AsNoTracking().OrderBy(p => p.SoPhong).ToListAsync();
                return Page();
            }
        }

        // Xử lý logic Xóa phòng an toàn
        public async Task<IActionResult> OnGetXoaAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            try
            {
                var phong = await _context.PHONG.FindAsync(id);
                if (phong != null)
                {
                    // Kiểm tra nghiệp vụ: Không cho phép xóa phòng đang có khách hoặc đã đặt (TT02)
                    if (phong.MaTrangThai == "TT02")
                    {
                        ModelState.AddModelError(string.Empty, "Không thể xóa phòng đang có khách hoặc đã được đặt!");
                        DanhSachPhong = await _context.PHONG.AsNoTracking().OrderBy(p => p.SoPhong).ToListAsync();
                        return Page();
                    }

                    _context.PHONG.Remove(phong);
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi xóa phòng: " + ex.Message);
                DanhSachPhong = await _context.PHONG.AsNoTracking().OrderBy(p => p.SoPhong).ToListAsync();
                return Page();
            }
        }
    }
}