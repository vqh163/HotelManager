using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // Thuộc tính hứng ngày giờ truy vấn sơ đồ phòng từ giao diện
        [BindProperty(SupportsGet = true)]
        public DateTime? NgayTraCuu { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Nếu người dùng không chọn ngày trên lịch, mặc định lấy ngày hôm nay
                DateTime targetDate = NgayTraCuu ?? DateTime.Now.Date;

                // Gán lại biến để đẩy ra View HTML
                NgayTraCuu = targetDate;

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
                    query = query.Where(p => p.MaTrangThai == TrangThaiLoc);
                }

                var rooms = await query.OrderBy(p => p.SoPhong).ToListAsync();

                // =====================================================================
                // THUẬT TOÁN XÁC ĐỊNH TRẠNG THÁI PHÒNG THEO NGÀY TRUY VẤN (UC_03)
                // =====================================================================
                foreach (var phong in rooms)
                {
                    // Bỏ qua các phòng đang khóa/bảo trì (Trạng thái tĩnh)
                    if (phong.MaTrangThai != "TT04")
                    {
                        // Kiểm tra xem phòng có giao dịch đặt phòng nào bao trùm lên targetDate không
                        bool isBooked = await (from dp in _context.DAT_PHONG
                                               join ct in _context.CHI_TIET_DAT_PHONG on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                               where ct.PHONGMaPhong == phong.MaPhong
                                                  && dp.TrangThaiDatPhong != "Đã hủy"
                                                  && dp.TrangThaiDatPhong != "Đã check-out"
                                                  && dp.TrangThaiDatPhong != "Đã hoàn tất"
                                                  && dp.NgayNhan <= targetDate
                                                  && dp.NgayTra > targetDate
                                               select dp.MaDatPhong).AnyAsync();

                        // Ảo hóa trạng thái hiển thị theo ngày được chọn (Không thay đổi Database gốc)
                        if (isBooked)
                        {
                            phong.MaTrangThai = "TT02"; // Đã đặt / Đang ở
                        }
                        else
                        {
                            phong.MaTrangThai = "TT01"; // Trống
                        }
                    }
                }

                DanhSachPhong = rooms;

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