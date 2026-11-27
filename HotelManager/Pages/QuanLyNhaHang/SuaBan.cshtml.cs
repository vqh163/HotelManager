using HotelManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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

        // Tải thông tin bàn hiện tại lên form chỉnh sửa
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

        // Xử lý lưu cập nhật thông tin bàn (Tên/Số bàn & Sức chứa; giữ nguyên Trạng thái vận hành thực tế)
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                string maBanHienTai = BanInfo.MaBan.Trim();
                string tenBanMoi = BanInfo.TenBan.Trim();

                // Kiểm tra trùng Số bàn/Tên bàn với bàn khác trong hệ thống bằng LINQ
                bool isDuplicate = await _context.BAN_NHA_HANG.AsNoTracking()
                    .AnyAsync(b => b.MaBan != maBanHienTai && b.TenBan == tenBanMoi);
                if (isDuplicate)
                {
                    ModelState.AddModelError(string.Empty, "Tên bàn/Số bàn này đã tồn tại ở một bàn khác. Vui lòng nhập số bàn khác!");
                    return Page();
                }

                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBanHienTai);
                if (ban == null) return RedirectToPage("./SoDoBan");

                ban.TenBan = tenBanMoi;
                ban.SucChua = BanInfo.SucChua;

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
