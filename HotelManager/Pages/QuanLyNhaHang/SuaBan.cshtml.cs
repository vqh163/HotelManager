using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class SuaBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuaBanModel> _logger;

        public SuaBanModel(ApplicationDbContext context, ILogger<SuaBanModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public ThemBanViewModel BanInfo { get; set; } = new ThemBanViewModel();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage("./SoDoBan");

            var ban = await _context.BAN_NHA_HANG.AsNoTracking().FirstOrDefaultAsync(b => b.MaBan == id);
            if (ban == null) return RedirectToPage("./SoDoBan");

            BanInfo = new ThemBanViewModel
            {
                MaBan = ban.MaBan,
                TenBan = ban.TenBan ?? string.Empty,
                SucChua = ban.SucChua,
                TrangThai = ban.TrangThai ?? "Trống",
                TenKhachDat = ban.TenKhachDat
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == BanInfo.MaBan);
                if (ban == null) return RedirectToPage("./SoDoBan");

                ban.TenBan = BanInfo.TenBan.Trim();
                ban.TrangThai = BanInfo.TrangThai;
                ban.SucChua = BanInfo.SucChua;
                ban.TenKhachDat = BanInfo.TrangThai == "Đã đặt trước" ? BanInfo.TenKhachDat?.Trim() : null;

                // Kiểm tra hóa đơn đang mở bằng LINQ chuẩn (thay thế hoàn toàn câu SQL thuần sai tên cột trước đó)
                if (BanInfo.TrangThai == "Đang có khách")
                {
                    bool hasActiveInvoice = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                        .AnyAsync(h => h.BAN_NHA_HANGMaBan == ban.MaBan
                            && (h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                                || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan));

                    if (!hasActiveInvoice)
                    {
                        _context.HOA_DON_NHA_HANG.Add(new HOA_DON_NHA_HANG
                        {
                            MaHoaDonNhaHang = BaoMatHelper.SinhMaNgauNhien("HD"),
                            BAN_NHA_HANGMaBan = ban.MaBan,
                            NHAN_VIENMaNV = User.FindFirst("MaNV")?.Value,
                            ThoiGianTao = DateTime.Now,
                            TongTienMonAn = 0,
                            PhiPhucVu = 0,
                            TienThueVAT = 0,
                            GiamGia = 0,
                            ThanhTien = 0,
                            TrangThaiThanhToan = HangSoHeThong.HoaDonNHDangPhucVu,
                            HinhThucThanhToan = "Đang phục vụ"
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin bàn thành công!";
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật thông tin bàn {MaBan}", BanInfo.MaBan);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi cập nhật thông tin bàn.");
                return Page();
            }
        }
    }
}
