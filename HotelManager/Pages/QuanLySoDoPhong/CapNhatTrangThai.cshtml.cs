using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Sửa lại đúng Policy chuẩn (hỗ trợ cả "Nhân viên Buồng phòng" và "Buồng phòng")
    [Authorize(Policy = HangSoHeThong.PolicyBuongPhong)]
    public class CapNhatTrangThaiModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CapNhatTrangThaiModel> _logger;

        private static readonly string[] TrangThaiPhongHopLe = { "Trống", "Đã đặt", "Đang ở", "Bảo trì", "DND" };
        private static readonly string[] TrangThaiVeSinhHopLe = { "Sạch", "Bẩn nhẹ", "Bẩn", "Đang dọn dẹp" };

        public CapNhatTrangThaiModel(ApplicationDbContext context, ILogger<CapNhatTrangThaiModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TRANG_THAI_PHONG TrangThaiPhongEdit { get; set; } = new TRANG_THAI_PHONG();

        [BindProperty]
        public string MaPhong { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string maPhong)
        {
            if (string.IsNullOrEmpty(maPhong)) return NotFound();
            MaPhong = maPhong;

            try
            {
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong == null || string.IsNullOrEmpty(phong.MaTrangThai)) return NotFound();

                var trangThai = await _context.TRANG_THAI_PHONG.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                if (trangThai == null) return NotFound();

                TrangThaiPhongEdit = trangThai;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải trạng thái phòng {MaPhong}", maPhong);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi truy xuất dữ liệu trạng thái phòng.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                // Tra cứu ngược qua MaPhong để tránh giả mạo MaTrangThai từ form
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == MaPhong);
                if (phong == null || string.IsNullOrEmpty(phong.MaTrangThai)) return NotFound();

                var trangThaiDb = await _context.TRANG_THAI_PHONG
                    .FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                if (trangThaiDb == null) return NotFound();

                if (!string.IsNullOrWhiteSpace(TrangThaiPhongEdit.TrangThaiPhong) &&
                    TrangThaiPhongHopLe.Contains(TrangThaiPhongEdit.TrangThaiPhong.Trim()))
                {
                    trangThaiDb.TrangThaiPhong = TrangThaiPhongEdit.TrangThaiPhong.Trim();
                }

                if (!string.IsNullOrWhiteSpace(TrangThaiPhongEdit.TrangThaiVeSinh) &&
                    TrangThaiVeSinhHopLe.Contains(TrangThaiPhongEdit.TrangThaiVeSinh.Trim()))
                {
                    trangThaiDb.TrangThaiVeSinh = TrangThaiPhongEdit.TrangThaiVeSinh.Trim();
                    if (trangThaiDb.TrangThaiVeSinh == "Sạch")
                        trangThaiDb.ThoiGianDonVeSinh = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // SỬA LỖI 404: Chuyển hướng đúng về trang Sơ đồ phòng
                return RedirectToPage("/QuanLySoDoPhong/SoDoPhong");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật trạng thái phòng {MaPhong}", MaPhong);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi cập nhật trạng thái phòng.");
                return Page();
            }
        }
    }
}
