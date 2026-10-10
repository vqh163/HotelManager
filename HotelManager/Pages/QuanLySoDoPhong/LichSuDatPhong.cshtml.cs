using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class LichSuDatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LichSuDatPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class ThongTinLichSu
        {
            public DAT_PHONG DatPhong { get; set; } = default!;
            public CHI_TIET_DAT_PHONG ChiTiet { get; set; } = default!;
            public PHONG Phong { get; set; } = default!;
        }

        public List<ThongTinLichSu> DanhSachSapToi { get; set; } = new List<ThongTinLichSu>();
        public List<ThongTinLichSu> DanhSachDaHoanTat { get; set; } = new List<ThongTinLichSu>();

        [TempData]
        public string? ThongBao { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");

            try
            {
                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoan != null && !string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH))
                {
                    var query = from dp in _context.DAT_PHONG
                                join ct in _context.CHI_TIET_DAT_PHONG on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                join p in _context.PHONG on ct.PHONGMaPhong equals p.MaPhong
                                where dp.KHACH_HANGMaKH == taiKhoan.KHACH_HANGMaKH
                                orderby dp.NgayNhan descending
                                select new ThongTinLichSu { DatPhong = dp, ChiTiet = ct, Phong = p };

                    var tatCaLichSu = await query.AsNoTracking().ToListAsync();

                    DanhSachSapToi = tatCaLichSu.Where(x => x.DatPhong.TrangThaiDatPhong == "Chờ xác nhận" || x.DatPhong.TrangThaiDatPhong == "Đã xác nhận" || x.DatPhong.TrangThaiDatPhong == "Chờ nhận phòng").ToList();
                    DanhSachDaHoanTat = tatCaLichSu.Where(x => x.DatPhong.TrangThaiDatPhong == "Đã hoàn tất" || x.DatPhong.TrangThaiDatPhong == "Đã hủy" || x.DatPhong.TrangThaiDatPhong == "Đã check-out").ToList();
                }
                return Page();
            }
            catch (Exception ex)
            {
                ThongBao = "❌ Lỗi hệ thống: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostHuyPhongAsync(string maDatPhong)
        {
            try
            {
                var datPhong = await _context.DAT_PHONG.FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);
                if (datPhong == null || !datPhong.NgayNhan.HasValue)
                {
                    ThongBao = "❌ Lỗi dữ liệu đặt phòng!";
                    return RedirectToPage();
                }

                // Thuật toán tính ngày và hoàn cọc
                double soNgayConLai = (datPhong.NgayNhan.Value.Date - DateTime.Now.Date).TotalDays;
                decimal tienCoc = datPhong.TienCoc.GetValueOrDefault(0m);
                decimal tienHoan = 0m;
                string ghiChuHoanTien = "";

                if (soNgayConLai >= 7)
                {
                    tienHoan = tienCoc;
                    ghiChuHoanTien = $"Hoàn 100% cọc: {tienHoan:N0} đ (Hủy trước {soNgayConLai} ngày).";
                }
                else if (soNgayConLai >= 5)
                {
                    tienHoan = tienCoc * 0.5m;
                    ghiChuHoanTien = $"Hoàn 50% cọc: {tienHoan:N0} đ (Hủy trước {soNgayConLai} ngày).";
                }
                else
                {
                    ghiChuHoanTien = $"Không hoàn cọc (Hủy dưới 5 ngày).";
                }

                datPhong.TrangThaiDatPhong = "Đã hủy";
                datPhong.GhiChu = string.IsNullOrEmpty(datPhong.GhiChu) ? ghiChuHoanTien : $"{datPhong.GhiChu} | {ghiChuHoanTien}";
                _context.DAT_PHONG.Update(datPhong);

                var chiTiet = await _context.CHI_TIET_DAT_PHONG.FirstOrDefaultAsync(c => c.DAT_PHONGMaDatPhong == maDatPhong);
                if (chiTiet != null && !string.IsNullOrEmpty(chiTiet.PHONGMaPhong))
                {
                    var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == chiTiet.PHONGMaPhong);
                    if (phong != null)
                    {
                        // Giả định cập nhật trạng thái phòng Trống tại đây (bạn có thể thay đổi thuộc tính tương ứng)
                        _context.PHONG.Update(phong);
                    }
                }

                await _context.SaveChangesAsync();
                ThongBao = "✅ Hủy phòng thành công! " + ghiChuHoanTien;
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ThongBao = "❌ Lỗi khi hủy phòng: " + ex.Message;
                return RedirectToPage();
            }
        }
    }
}