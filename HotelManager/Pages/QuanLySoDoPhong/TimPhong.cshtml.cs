using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class TimPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TimPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputKiemTra Input { get; set; } = new InputKiemTra();

        public class InputKiemTra
        {
            public DateTime NgayNhan { get; set; } = DateTime.Now;
            public DateTime NgayTra { get; set; } = DateTime.Now.AddDays(1);
            public int SoKhach { get; set; } = 1;
            public string? LoaiPhongTimKiem { get; set; } = "Tất cả";
        }

        // Tạo ViewModel để truyền cả thông tin phòng lẫn trạng thái "Khả dụng" ra View
        public class PhongHienThi
        {
            public PHONG PhongInfo { get; set; } = default!;
            public bool IsAvailable { get; set; }
        }

        public List<PhongHienThi> DanhSachPhong { get; set; } = new List<PhongHienThi>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Mặc định load danh sách và gán tất cả là khả dụng khi mới vào trang
                var rooms = await _context.PHONG.AsNoTracking().Take(9).ToListAsync();
                DanhSachPhong = rooms.Select(r => new PhongHienThi { PhongInfo = r, IsAvailable = true }).ToList();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input.NgayNhan >= Input.NgayTra)
            {
                ModelState.AddModelError(string.Empty, "Ngày trả phòng phải lớn hơn ngày nhận phòng.");
                return Page();
            }

            try
            {
                // 1. Lấy danh sách phòng thỏa mãn tiêu chí Loại phòng, Sức chứa và KHÔNG BỊ KHÓA
                var query = _context.PHONG.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(Input.LoaiPhongTimKiem) && Input.LoaiPhongTimKiem != "Tất cả")
                {
                    query = query.Where(p => p.LoaiPhong == Input.LoaiPhongTimKiem);
                }
                
                query = query.Where(p => p.SucChuaToiDa >= Input.SoKhach);
                
                // [FIX LỖI]: Lọc bỏ các phòng đang bị Khóa/Bảo trì (TT04)
                query = query.Where(p => p.MaTrangThai != "TT04");

                var rooms = await query.ToListAsync();
                var result = new List<PhongHienThi>();

                DateTime requestedIn = Input.NgayNhan.Date;
                DateTime requestedOut = Input.NgayTra.Date;

                // 2. Thuật toán kiểm tra tình trạng phòng trống (Availability Check)
                foreach (var r in rooms)
                {
                    // Truy vấn kiểm tra xem phòng này có bất kỳ đơn đặt phòng nào "Giao thoa" thời gian không
                    bool isBooked = await (from dp in _context.DAT_PHONG
                                           join ct in _context.CHI_TIET_DAT_PHONG on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                           where ct.PHONGMaPhong == r.MaPhong
                                              // Bỏ qua các phiếu đã hủy hoặc đã hoàn tất/check-out
                                              && dp.TrangThaiDatPhong != "Đã hủy"
                                              && dp.TrangThaiDatPhong != "Đã check-out"
                                              && dp.TrangThaiDatPhong != "Đã hoàn tất"
                                              // Thuật toán giao thoa thời gian (Overlap)
                                              && dp.NgayNhan < requestedOut
                                              && dp.NgayTra > requestedIn
                                           select dp.MaDatPhong).AnyAsync();

                    // Nếu isBooked = true tức là phòng đã bị đặt trong khoảng ngày đó -> IsAvailable = false
                    result.Add(new PhongHienThi { PhongInfo = r, IsAvailable = !isBooked });
                }

                DanhSachPhong = result;
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tìm kiếm: {ex.Message}");
                return Page();
            }
        }
    }
}