using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    [Authorize]
    public class XacNhanDatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<XacNhanDatPhongModel> _logger;

        public XacNhanDatPhongModel(ApplicationDbContext context, ILogger<XacNhanDatPhongModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public DAT_PHONG ThongTinDatPhong { get; set; } = new DAT_PHONG();
        public PHONG PhongDuocDat { get; set; } = new PHONG();

        public int SoNgayO { get; set; }
        public decimal TongTienPhong { get; set; }
        public decimal SoTienCanCoc { get; set; }

        public async Task<IActionResult> OnGetAsync(string maDatPhong)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");
            if (string.IsNullOrEmpty(maDatPhong)) return RedirectToPage("/Index");

            try
            {
                var datPhong = await _context.DAT_PHONG.AsNoTracking().FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);
                if (datPhong == null) return NotFound();

                // Chống IDOR: Chỉ chủ đơn đặt phòng hoặc nhân viên Lễ tân / Quản lý / Sales / Giám đốc / Quản trị viên được xem
                bool laNhanVienDuocXem = User.IsInRole(HangSoHeThong.VaiTroLeTan)
                    || User.IsInRole(HangSoHeThong.VaiTroQuanLy)
                    || User.IsInRole(HangSoHeThong.VaiTroGiamDoc)
                    || User.IsInRole(HangSoHeThong.VaiTroSales)
                    || User.IsInRole(HangSoHeThong.VaiTroQuanTri);

                if (!laNhanVienDuocXem)
                {
                    var taiKhoan = await _context.TAI_KHOAN.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TenDangNhap == User.Identity.Name);
                    if (taiKhoan == null || taiKhoan.KHACH_HANGMaKH != datPhong.KHACH_HANGMaKH)
                    {
                        return Forbid();
                    }
                }

                ThongTinDatPhong = datPhong;

                var chiTiet = await _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.DAT_PHONGMaDatPhong == maDatPhong);
                if (chiTiet != null)
                {
                    PhongDuocDat = await _context.PHONG.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.MaPhong == chiTiet.PHONGMaPhong) ?? new PHONG();

                    SoNgayO = Math.Max(1, chiTiet.SoNgayO);
                    TongTienPhong = (chiTiet.DonGia ?? 0m) * SoNgayO;
                    decimal tienCocDaNhap = ThongTinDatPhong.TienCoc ?? 0m;
                    SoTienCanCoc = tienCocDaNhap > 0 ? tienCocDaNhap : (TongTienPhong * 0.3m);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi truy xuất biên lai đặt phòng {MaDP}", maDatPhong);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi truy xuất biên lai đặt phòng.");
                return Page();
            }
        }
    }
}
