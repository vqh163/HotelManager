using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Phân quyền truy cập theo tài liệu thiết kế của dự án
    [Authorize(Roles = "Quản lý,Lễ tân,Sales,Buồng phòng")]
    public class SoDoPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SoDoPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ViewModel trung gian chứa thông tin chi tiết để map ra giao diện (Chống lộ Model gốc)
        public class RoomGridViewModel
        {
            public string MaPhong { get; set; } = default!;
            public string? SoPhong { get; set; }
            public string? LoaiPhong { get; set; }
            public int? SoGiuong { get; set; }
            public int? SucChuaToiDa { get; set; }
            public double? DienTich { get; set; }

            public string? MaTrangThai { get; set; }
            public string? TrangThaiPhong { get; set; }
            public string? TrangThaiVeSinh { get; set; }

            // Logic lấy chữ số đầu tiên làm Tầng (VD: 201 -> Tầng 2)
            public string Tang => !string.IsNullOrEmpty(SoPhong) ? SoPhong.Substring(0, 1) : "1";

            // Thông tin khách hàng (Được Left Join từ bảng KHACH_HANG)
            public string? TenKhachHang { get; set; }
            public string? GhiChuKhachHang { get; set; }
        }

        // Danh sách đổ ra giao diện
        public List<RoomGridViewModel> DanhSachLieuPhong { get; set; } = new List<RoomGridViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // BƯỚC 1: Lấy danh sách Booking đang hiệu lực (Đang ở hoặc Đã đặt)
                // Sử dụng đúng tên khóa ngoại do Entity Framework sinh tự động
                var activeBookings = _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                    .Join(_context.DAT_PHONG.AsNoTracking(),
                          ctdp => ctdp.DAT_PHONGMaDatPhong,
                          dp => dp.MaDatPhong,
                          (ctdp, dp) => new { ctdp, dp })
                    .Join(_context.KHACH_HANG.AsNoTracking(),
                          joined => joined.dp.KHACH_HANGMaKH,
                          kh => kh.MaKH,
                          (joined, kh) => new {
                              MaPhong = joined.ctdp.PHONGMaPhong,
                              TenKhachHang = kh.HoTen,
                              GhiChu = joined.dp.GhiChu,
                              TrangThaiDat = joined.dp.TrangThaiDatPhong
                          })
                    .Where(x => x.TrangThaiDat == "DANG_O" || x.TrangThaiDat == "DA_DAT");

                // BƯỚC 2: Truy vấn danh sách phòng & Left Join để lấy tên khách
                var query = from p in _context.PHONG.AsNoTracking()

                            join t in _context.TRANG_THAI_PHONG.AsNoTracking()
                            on p.MaTrangThai equals t.MaTrangThai into pt
                            from t in pt.DefaultIfEmpty()

                            join b in activeBookings
                            on p.MaPhong equals b.MaPhong into pb
                            from b in pb.DefaultIfEmpty()

                            select new RoomGridViewModel
                            {
                                MaPhong = p.MaPhong,
                                SoPhong = p.SoPhong,
                                LoaiPhong = p.LoaiPhong,
                                SoGiuong = p.SoGiuong,
                                SucChuaToiDa = p.SucChuaToiDa,
                                MaTrangThai = p.MaTrangThai,

                                TrangThaiPhong = t != null ? t.TrangThaiPhong : "Trống",
                                TrangThaiVeSinh = t != null ? t.TrangThaiVeSinh : "Sạch",

                                TenKhachHang = b != null ? b.TenKhachHang : null,
                                GhiChuKhachHang = b != null ? b.GhiChu : null
                            };

                DanhSachLieuPhong = await query.OrderBy(x => x.SoPhong).ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tải sơ đồ phòng: {ex.Message}");
                return Page();
            }
        }

        // Cập nhật Vệ sinh (Dành cho Lễ tân, Buồng phòng)
        public async Task<IActionResult> OnPostCapNhatVeSinhAsync(string maPhong, string trangThaiVeSinh)
        {
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong != null)
                {
                    var trangThai = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                    if (trangThai != null)
                    {
                        trangThai.TrangThaiVeSinh = trangThaiVeSinh;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { /* Ghi log lỗi nếu cần */ }

            return RedirectToPage();
        }

        // Cập nhật trạng thái nghiệp vụ (Bảo trì, Hư hỏng...)
        public async Task<IActionResult> OnPostDoiTrangThaiAsync(string maPhong, string trangThaiMoi)
        {
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong != null)
                {
                    phong.MaTrangThai = trangThaiMoi;
                    await _context.SaveChangesAsync();
                }
            }
            catch { /* Ghi log lỗi nếu cần */ }

            return RedirectToPage();
        }
    }
}