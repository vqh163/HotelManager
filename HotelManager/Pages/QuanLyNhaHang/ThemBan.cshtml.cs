using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class ThemBanViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã bàn.")]
        [StringLength(20, ErrorMessage = "Mã bàn tối đa 20 ký tự.")]
        public string MaBan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên/số bàn.")]
        [StringLength(50, ErrorMessage = "Tên bàn tối đa 50 ký tự.")]
        public string TenBan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập sức chứa.")]
        [Range(1, 100, ErrorMessage = "Sức chứa phải từ 1 đến 100 khách.")]
        public int? SucChua { get; set; }

        // Mặc định khi thêm bàn mới sẽ là Trống
        public string TrangThai { get; set; } = "Trống";
        public string? TenKhachDat { get; set; }
        public string? SoPhongDat { get; set; }
    }

    public class ThemBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ThemBanModel> _logger;

        public ThemBanModel(ApplicationDbContext context, ILogger<ThemBanModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public ThemBanViewModel BanInfo { get; set; } = new ThemBanViewModel();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                string maBanChuan = BanInfo.MaBan.Trim();
                string tenBanChuan = BanInfo.TenBan.Trim();

                // Kiểm tra trùng Mã bàn hoặc Tên/Số bàn bằng LINQ
                bool daTonTai = await _context.BAN_NHA_HANG.AsNoTracking()
                    .AnyAsync(b => b.MaBan == maBanChuan || b.TenBan == tenBanChuan);
                if (daTonTai)
                {
                    ModelState.AddModelError(string.Empty, "Mã bàn hoặc Tên/Số bàn này đã tồn tại trong hệ thống. Vui lòng nhập thông tin khác!");
                    return Page();
                }

                string trangThai = string.IsNullOrWhiteSpace(BanInfo.TrangThai) ? "Trống" : BanInfo.TrangThai.Trim();

                var ban = new BAN_NHA_HANG
                {
                    MaBan = maBanChuan,
                    TenBan = tenBanChuan,
                    SucChua = BanInfo.SucChua,
                    TrangThai = trangThai,
                    TenKhachDat = trangThai == "Đã đặt trước" ? BanInfo.TenKhachDat?.Trim() : null
                };

                _context.BAN_NHA_HANG.Add(ban);

                // Sinh hóa đơn đang phục vụ nếu thêm bàn ở trạng thái Đang có khách
                if (trangThai == "Đang có khách")
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
                        HinhThucThanhToan = "Chưa thanh toán"
                    });
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm bàn mới thành công!";
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm bàn nhà hàng.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi thêm bàn.");
                return Page();
            }
        }
    }
}
