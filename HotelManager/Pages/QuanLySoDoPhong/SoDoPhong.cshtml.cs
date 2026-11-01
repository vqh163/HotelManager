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

            public string? IconVeSinh { get; set; }
            public string? MauIconVeSinh { get; set; }
            public int SoNgayChuaDon { get; set; }

            public string Tang => !string.IsNullOrEmpty(SoPhong) ? SoPhong.Substring(0, 1) : "1";
            public string? TenKhachHang { get; set; }
            public string? SDTKhachHang { get; set; }
            public string? GhiChuKhachHang { get; set; }
            public DateTime? NgayNhan { get; set; }
            public DateTime? NgayTra { get; set; }
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? ThoiGianLoc { get; set; }

        public List<RoomGridViewModel> DanhSachLieuPhong { get; set; } = new List<RoomGridViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                DateTime thoiGianKiemTra = ThoiGianLoc ?? DateTime.Now;

                // TRUY VẤN 1: Lấy danh sách Booking đang hiệu lực
                // TRUY VẤN ĐỒNG BỘ: Đã bổ sung "Chờ xác nhận" để khớp với luồng tạo đơn đặt phòng trực tuyến
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
                              SDTKhachHang = kh.SDT, // BỔ SUNG LẤY SĐT KHÁCH HÀNG
                              GhiChu = joined.dp.GhiChu,
                              TrangThaiDat = joined.dp.TrangThaiDatPhong,
                              NgayNhan = joined.dp.NgayNhan,
                              NgayTra = joined.dp.NgayTra
                          })
                    .Where(x => x.NgayNhan <= thoiGianKiemTra && x.NgayTra >= thoiGianKiemTra
                             && (x.TrangThaiDat == "DANG_O" || x.TrangThaiDat == "DA_DAT" || x.TrangThaiDat == "Chờ xác nhận"))
                    .ToListAsync();

                // TRUY VẤN 2: Lấy danh sách Phòng và Trạng thái vật lý
                var query = await (from p in _context.PHONG.AsNoTracking()
                                   join t in _context.TRANG_THAI_PHONG.AsNoTracking() on p.MaTrangThai equals t.MaTrangThai into pt
                                   from t in pt.DefaultIfEmpty()
                                   select new { p, t }).ToListAsync();

                foreach (var item in query)
                {
                    var bookingTaiThoiDiem = activeBookings.FirstOrDefault(b => b.MaPhong == item.p.MaPhong);
                    string trangThaiTinhToan = "Trống";
                    string? tenKhach = null;
                    string? sdtKhach = null;
                    string? ghiChu = null;
                    DateTime? ngayNhan = null;
                    DateTime? ngayTra = null;

                    // Xử lý logic trạng thái
                    if (bookingTaiThoiDiem != null)
                    {
                        if (bookingTaiThoiDiem.TrangThaiDat == "DANG_O")
                        {
                            trangThaiTinhToan = "Đang ở";
                        }
                        else
                        {
                            trangThaiTinhToan = "Đã đặt";
                        }

                        tenKhach = bookingTaiThoiDiem.TenKhachHang;
                        sdtKhach = bookingTaiThoiDiem.SDTKhachHang; // Gán SĐT
                        ghiChu = bookingTaiThoiDiem.GhiChu;
                    }

                    // ... (Đoạn if (ThoiGianLoc == null) giữ nguyên) ...

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
                        SDTKhachHang = sdtKhach, // Truyền vào ViewModel
                        GhiChuKhachHang = ghiChu,
                        NgayNhan = bookingTaiThoiDiem?.NgayNhan,
                        NgayTra = bookingTaiThoiDiem?.NgayTra
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
                        vm.MauIconVeSinh = "text-success";
                    }
                    else if (vm.SoNgayChuaDon == 1)
                    {
                        vm.TrangThaiVeSinh = "Bẩn nhẹ";
                        vm.IconVeSinh = "bi-droplet-half";
                        vm.MauIconVeSinh = "text-warning";
                    }
                    else if (vm.SoNgayChuaDon >= 2)
                    {
                        vm.TrangThaiVeSinh = "Bẩn nặng (Cần dọn gấp)";
                        vm.IconVeSinh = "bi-exclamation-triangle-fill";
                        vm.MauIconVeSinh = "text-danger";
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

        // Cập nhật trạng thái dọn phòng (Buồng phòng)
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

        // HÀM CHECK-IN (Lễ tân) - Đã fix lỗi CS1061 bằng LINQ Join
        public async Task<IActionResult> OnPostCheckInAsync(string maPhongCheckIn)
        {
            try
            {
                // Dùng LINQ Join để truy cập trực tiếp DAT_PHONG thay vì dùng Include
                var datPhongCanCheckIn = await (from ct in _context.CHI_TIET_DAT_PHONG
                                                join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                where ct.PHONGMaPhong == maPhongCheckIn
                                                   && (dp.TrangThaiDatPhong == "DA_DAT" || dp.TrangThaiDatPhong == "Chờ xác nhận")
                                                select dp).FirstOrDefaultAsync();

                if (datPhongCanCheckIn != null)
                {
                    // Chuyển trạng thái sang Đang ở
                    datPhongCanCheckIn.TrangThaiDatPhong = "DANG_O";
                    _context.DAT_PHONG.Update(datPhongCanCheckIn);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi Check-in: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}