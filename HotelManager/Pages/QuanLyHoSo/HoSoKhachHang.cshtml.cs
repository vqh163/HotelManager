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

namespace HotelManager.Pages.QuanLyHoSo
{
    [Authorize(Policy = HangSoHeThong.PolicyKhachHang)]
    public class HoSoKhachHangModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HoSoKhachHangModel> _logger;

        public HoSoKhachHangModel(ApplicationDbContext context, ILogger<HoSoKhachHangModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public KHACH_HANG KhachHangInfo { get; set; } = new KHACH_HANG();

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
                    var kh = await _context.KHACH_HANG.AsNoTracking().FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);
                    if (kh != null)
                    {
                        KhachHangInfo = kh;
                    }
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải hồ sơ khách hàng.");
                ThongBao = "Đã xảy ra lỗi khi tải hồ sơ cá nhân.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");

            if (!ModelState.IsValid)
            {
                ThongBao = "Vui lòng kiểm tra lại các thông tin nhập chưa đúng!";
                return Page();
            }

            try
            {
                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoan != null && !string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH))
                {
                    var khUpdate = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);
                    if (khUpdate != null)
                    {
                        string? cccdMoi = string.IsNullOrWhiteSpace(KhachHangInfo.SoCCCD) ? null : KhachHangInfo.SoCCCD.Trim();
                        if (!string.IsNullOrEmpty(cccdMoi))
                        {
                            bool trungCCCD = await _context.KHACH_HANG.AsNoTracking()
                                .AnyAsync(k => k.SoCCCD == cccdMoi && k.MaKH != khUpdate.MaKH);
                            if (trungCCCD)
                            {
                                ThongBao = "Số CCCD này đã được đăng ký cho một tài khoản khách hàng khác!";
                                return RedirectToPage();
                            }
                        }

                        // Chỉ cho phép cập nhật các trường cá nhân hợp lệ (không cho sửa DiemThuong/HangThanhVien/Blacklist)
                        khUpdate.HoTen = KhachHangInfo.HoTen?.Trim();
                        khUpdate.SDT = KhachHangInfo.SDT?.Trim();
                        khUpdate.Email = KhachHangInfo.Email?.Trim();
                        khUpdate.SoCCCD = cccdMoi;
                        khUpdate.GioiTinh = KhachHangInfo.GioiTinh;
                        khUpdate.NgaySinh = KhachHangInfo.NgaySinh;

                        await _context.SaveChangesAsync();
                        ThongBao = "Cập nhật hồ sơ thành công!";
                    }
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu hồ sơ khách hàng.");
                ThongBao = "Đã xảy ra lỗi hệ thống khi cập nhật hồ sơ.";
                return Page();
            }
        }
    }
}
