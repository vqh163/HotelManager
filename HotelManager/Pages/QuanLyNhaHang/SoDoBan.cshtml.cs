using HotelManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    // DTO map dữ liệu hiển thị ra View
    public class BanNhaHangViewModel
    {
        public string MaBan { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;
        public string SucChua { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;

        public string? MaHoaDonNhaHang { get; set; }
        public string TenKhachDat { get; set; } = string.Empty;
    }

    public class SoDoBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SoDoBanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        public List<BanNhaHangViewModel> DanhSachBan { get; set; } = new List<BanNhaHangViewModel>();

        public async Task OnGetAsync()
        {
            try
            {
                // Sử dụng LINQ truy vấn bảng BAN_NHA_HANG
                var query = _context.BAN_NHA_HANG.AsQueryable();

                // 1. Tìm kiếm theo số bàn
                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    query = query.Where(b => b.TenBan.Contains(TuKhoaTimKiem) || b.MaBan.Contains(TuKhoaTimKiem));
                }

                // 2. Lọc theo trạng thái từ Dropdown
                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả bàn")
                {
                    query = query.Where(b => b.TrangThai == TrangThaiLoc);
                }

                var dsBanDb = await query.OrderBy(b => b.TenBan).ToListAsync();

                // Map dữ liệu sang ViewModel
                DanhSachBan = dsBanDb.Select(b => new BanNhaHangViewModel
                {
                    MaBan = b.MaBan,
                    TenBan = b.TenBan,
                    SucChua = b.SucChua?.ToString() ?? "0",
                    TrangThai = b.TrangThai,

                    MaHoaDonNhaHang = b.TrangThai == "Đang có khách" ? "#1284" : null,
                    TenKhachDat = b.TrangThai == "Đã đặt trước" ? "Nguyễn Kiên" : string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải dữ liệu sơ đồ bàn: " + ex.Message);
            }
        }

        // Handler xử lý khi Phục vụ bấm nút "Xác nhận sạch"
        public async Task<IActionResult> OnPostXacNhanSachAsync(string maBan)
        {
            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBan);
                if (ban != null)
                {
                    ban.TrangThai = "Trống";
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật trạng thái bàn: " + ex.Message);
            }
            return RedirectToPage();
        }
    }
}