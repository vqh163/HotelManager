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
            // Kiểm tra tính hợp lệ của ngày nhận và ngày trả
            if (Input.NgayNhan.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: Ngày nhận phòng không được chọn trong quá khứ.");
                return Page();
            }

            if (Input.NgayNhan.Date >= Input.NgayTra.Date)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: Ngày trả phòng phải lớn hơn ngày nhận phòng ít nhất 1 ngày.");
                return Page();
            }

            try
            {
                DateTime requestedIn = Input.NgayNhan.Date.AddHours(14);  // Giờ check-in tiêu chuẩn 14:00
                DateTime requestedOut = Input.NgayTra.Date.AddHours(12); // Giờ check-out tiêu chuẩn 12:00

                // 1. Truy vấn danh sách phòng thỏa mãn tiêu chí và KHÔNG bị khóa bảo trì (TT04)
                var query = _context.PHONG.AsNoTracking()
                                          .Where(p => p.MaTrangThai != "TT04" && p.SucChuaToiDa >= Input.SoKhach);

                if (!string.IsNullOrEmpty(Input.LoaiPhongTimKiem) && Input.LoaiPhongTimKiem != "Tất cả")
                {
                    query = query.Where(p => p.LoaiPhong == Input.LoaiPhongTimKiem);
                }

                var rooms = await query.OrderBy(p => p.SoPhong).ToListAsync();

                // 2. [TỐI ƯU HIỆU NĂNG - CHỐNG LỖI N+1 QUERY]:
                // Chỉ dùng 1 câu truy vấn LINQ duy nhất để lấy toàn bộ Mã Phòng đã bị đặt trùng lịch
                var bookedRoomIds = await (from dp in _context.DAT_PHONG.AsNoTracking()
                                           join ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                                on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                           where dp.TrangThaiDatPhong != "Đã hủy"
                                              && dp.TrangThaiDatPhong != "Đã check-out"
                                              && dp.TrangThaiDatPhong != "Đã hoàn tất"
                                              && dp.NgayNhan < requestedOut
                                              && dp.NgayTra > requestedIn
                                           select ct.PHONGMaPhong)
                                           .Distinct()
                                           .ToListAsync();

                // Đưa vào HashSet để tra cứu O(1) siêu tốc trên RAM
                var bookedSet = new HashSet<string>(bookedRoomIds);

                // 3. Ánh xạ kết quả ra ViewModel mà không cần gọi lại Database
                DanhSachPhong = rooms.Select(r => new PhongHienThi
                {
                    PhongInfo = r,
                    IsAvailable = !bookedSet.Contains(r.MaPhong)
                }).ToList();

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