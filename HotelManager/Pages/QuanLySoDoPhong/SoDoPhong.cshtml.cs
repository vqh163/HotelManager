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
    // Cấp quyền cho các Role liên quan đến sơ đồ phòng
    [Authorize(Roles = "Quản lý,Lễ tân,Sales,Nhân viên Buồng phòng")]
    public class SoDoPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SoDoPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class RoomGridViewModel
        {
            public string MaPhong { get; set; } = default!;
            public string? SoPhong { get; set; }
            public string? LoaiPhong { get; set; }
            public int? SoGiuong { get; set; }
            public int? SucChuaToiDa { get; set; }

            public string? TrangThaiPhong { get; set; }
            public string? TrangThaiVeSinh { get; set; }

            // Các thuộc tính phục vụ UI Buồng phòng
            public string? IconVeSinh { get; set; }
            public string? MauIconVeSinh { get; set; }
            public int SoNgayChuaDon { get; set; }

            public string Tang => !string.IsNullOrEmpty(SoPhong) ? SoPhong.Substring(0, 1) : "1";
            public string? TenKhachHang { get; set; }
            public string? GhiChuKhachHang { get; set; }
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? ThoiGianLoc { get; set; }

        public List<RoomGridViewModel> DanhSachLieuPhong { get; set; } = new List<RoomGridViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                DateTime thoiGianKiemTra = ThoiGianLoc ?? DateTime.Now;

                // Truy vấn danh sách booking bằng LINQ
                var activeBookings = await _context.CHI_TIET_DAT_PHONG.AsNoTracking()
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
                              TrangThaiDat = joined.dp.TrangThaiDatPhong,
                              NgayNhan = joined.dp.NgayNhan,
                              NgayTra = joined.dp.NgayTra
                          })
                    .Where(x => x.NgayNhan <= thoiGianKiemTra && x.NgayTra >= thoiGianKiemTra
                             && (x.TrangThaiDat == "DANG_O" || x.TrangThaiDat == "DA_DAT"))
                    .ToListAsync();

                var query = await (from p in _context.PHONG.AsNoTracking()
                                   join t in _context.TRANG_THAI_PHONG.AsNoTracking() on p.MaTrangThai equals t.MaTrangThai into pt
                                   from t in pt.DefaultIfEmpty()
                                   select new { p, t }).ToListAsync();

                foreach (var item in query)
                {
                    var bookingTaiThoiDiem = activeBookings.FirstOrDefault(b => b.MaPhong == item.p.MaPhong);
                    string trangThaiTinhToan = "Trống";
                    string? tenKhach = null;
                    string? ghiChu = null;

                    if (bookingTaiThoiDiem != null)
                    {
                        trangThaiTinhToan = bookingTaiThoiDiem.TrangThaiDat == "DANG_O" ? "Đang ở" : "Đã đặt";
                        tenKhach = bookingTaiThoiDiem.TenKhachHang;
                        ghiChu = bookingTaiThoiDiem.GhiChu;
                    }
                    else if (ThoiGianLoc == null || ThoiGianLoc.Value.Date == DateTime.Now.Date)
                    {
                        string ttVatLy = item.t?.TrangThaiPhong?.Trim() ?? "Trống";
                        if (ttVatLy.Equals("Bảo trì", StringComparison.OrdinalIgnoreCase) ||
                            ttVatLy.Equals("DND", StringComparison.OrdinalIgnoreCase))
                        {
                            trangThaiTinhToan = ttVatLy;
                        }
                    }

                    var vm = new RoomGridViewModel
                    {
                        MaPhong = item.p.MaPhong,
                        SoPhong = item.p.SoPhong,
                        LoaiPhong = item.p.LoaiPhong,
                        SoGiuong = item.p.SoGiuong,
                        SucChuaToiDa = item.p.SucChuaToiDa,
                        TrangThaiPhong = trangThaiTinhToan,
                        TrangThaiVeSinh = item.t?.TrangThaiVeSinh?.Trim() ?? "Sạch",
                        TenKhachHang = tenKhach,
                        GhiChuKhachHang = ghiChu
                    };

                    // NGHIỆP VỤ: Tính toán độ bẩn của phòng theo ngày
                    if (item.t != null && item.t.ThoiGianDonVeSinh.HasValue)
                    {
                        vm.SoNgayChuaDon = (DateTime.Now.Date - item.t.ThoiGianDonVeSinh.Value.Date).Days;
                    }
                    else
                    {
                        vm.SoNgayChuaDon = 0;
                    }

                    // Phân loại trạng thái vệ sinh để hiển thị Icon
                    if (vm.TrangThaiVeSinh.ToLower() == "sạch" && vm.SoNgayChuaDon == 0)
                    {
                        vm.IconVeSinh = "bi-stars";
                        vm.MauIconVeSinh = "text-success"; // Trong ngày: Sạch
                    }
                    else if (vm.SoNgayChuaDon == 1)
                    {
                        vm.TrangThaiVeSinh = "Bẩn nhẹ";
                        vm.IconVeSinh = "bi-droplet-half";
                        vm.MauIconVeSinh = "text-warning"; // Qua 1 ngày: Bẩn nhẹ
                    }
                    else if (vm.SoNgayChuaDon >= 2)
                    {
                        vm.TrangThaiVeSinh = "Bẩn nặng (Cần dọn gấp)";
                        vm.IconVeSinh = "bi-exclamation-triangle-fill";
                        vm.MauIconVeSinh = "text-danger"; // Qua 2 ngày: Bẩn nặng
                    }
                    else
                    {
                        vm.IconVeSinh = "bi-brush-fill";
                        vm.MauIconVeSinh = "text-secondary";
                    }

                    DanhSachLieuPhong.Add(vm);
                }

                DanhSachLieuPhong = DanhSachLieuPhong.OrderBy(x => x.SoPhong).ToList();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tải sơ đồ phòng: {ex.Message}");
                return Page();
            }
        }

        // Bọc try-catch an toàn khi thao tác với CSDL
        public async Task<IActionResult> OnPostDoiTrangThaiAsync(string maPhong, string trangThaiMoi, string loaiCapNhat)
        {
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong != null)
                {
                    var trangThai = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                    if (trangThai != null)
                    {
                        if (loaiCapNhat == "BuongPhong")
                        {
                            trangThai.TrangThaiVeSinh = trangThaiMoi.Trim();
                            if (trangThaiMoi.Trim().ToLower() == "sạch")
                            {
                                // Cập nhật lại mốc thời gian dọn phòng về hiện tại
                                trangThai.ThoiGianDonVeSinh = DateTime.Now;
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi cập nhật: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}