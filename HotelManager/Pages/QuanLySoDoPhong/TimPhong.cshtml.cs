using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class TimPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TimPhongModel> _logger;

        public TimPhongModel(ApplicationDbContext context, ILogger<TimPhongModel> logger)
        {
            _context = context;
            _logger = logger;
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

        public class PhongHienThi
        {
            public PHONG PhongInfo { get; set; } = default!;
            public bool IsAvailable { get; set; }
        }

        public List<PhongHienThi> DanhSachPhong { get; set; } = new List<PhongHienThi>();

        // Hàm phụ trợ: Tải danh sách phòng khả dụng thật (loại trừ phòng Bảo trì trong TRANG_THAI_PHONG và phòng trùng lịch)
        private async Task TimKiemPhongThucTeAsync(DateTime ngayNhan, DateTime ngayTra, int soKhach, string? loaiPhong, int? takeLimit = null)
        {
            DateTime requestedIn = ngayNhan.Date.AddHours(14);
            DateTime requestedOut = ngayTra.Date.AddHours(12);

            // 1. Lấy danh sách mã trạng thái đang bị khóa "Bảo trì" từ bảng TRANG_THAI_PHONG
            var maTrangThaiBaoTri = await _context.TRANG_THAI_PHONG.AsNoTracking()
                .Where(t => t.TrangThaiPhong == "Bảo trì")
                .Select(t => t.MaTrangThai)
                .ToListAsync();

            var query = _context.PHONG.AsNoTracking()
                .Where(p => p.MaTrangThai != "TT04"
                         && (p.MaTrangThai == null || !maTrangThaiBaoTri.Contains(p.MaTrangThai))
                         && (p.SucChuaToiDa ?? 1) >= soKhach);

            if (!string.IsNullOrEmpty(loaiPhong) && loaiPhong != "Tất cả")
            {
                query = query.Where(p => p.LoaiPhong == loaiPhong);
            }

            query = query.OrderBy(p => p.SoPhong);
            var rooms = takeLimit.HasValue ? await query.Take(takeLimit.Value).ToListAsync() : await query.ToListAsync();

            // 2. Lấy tập phòng đã có lịch đặt trùng trong 1 truy vấn LINQ duy nhất
            var bookedRoomIds = await (from dp in _context.DAT_PHONG.AsNoTracking()
                                       join ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                            on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                       where !HangSoHeThong.TrangThaiDaKetThuc.Contains(dp.TrangThaiDatPhong ?? "")
                                          && dp.NgayNhan < requestedOut
                                          && dp.NgayTra > requestedIn
                                          && ct.PHONGMaPhong != null
                                       select ct.PHONGMaPhong!)
                                       .Distinct()
                                       .ToListAsync();

            var bookedSet = new HashSet<string>(bookedRoomIds);
            DanhSachPhong = rooms.Select(r => new PhongHienThi
            {
                PhongInfo = r,
                IsAvailable = !bookedSet.Contains(r.MaPhong)
            }).ToList();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Tính khả dụng thực tế cho ngày hôm nay -> ngày mai (không gán cứng IsAvailable = true)
                await TimKiemPhongThucTeAsync(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), 1, "Tất cả", takeLimit: 9);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách phòng tìm kiếm.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải danh sách phòng.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
                await TimKiemPhongThucTeAsync(Input.NgayNhan, Input.NgayTra, Math.Max(1, Input.SoKhach), Input.LoaiPhongTimKiem);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tìm kiếm phòng trống.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tìm kiếm phòng trống.");
                return Page();
            }
        }
    }
}
