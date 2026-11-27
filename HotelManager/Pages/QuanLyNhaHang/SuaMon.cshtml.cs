using HotelManager.Data;
using HotelManager.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.NhaBep
{
    public class SuaMonViewModel
    {
        public string MaDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên món.")]
        public string TenDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        public string LoaiDichVu { get; set; } = "Món chính";

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
        public decimal DonGia { get; set; }

        public string? MoTa { get; set; }
        public string? LuuYDiUng { get; set; }
        public string HinhAnh { get; set; } = string.Empty;
    }

    public class SuaMonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SuaMonModel> _logger;

        public SuaMonModel(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<SuaMonModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public SuaMonViewModel MonInfo { get; set; } = new SuaMonViewModel();

        [BindProperty]
        public IFormFile? HinhAnhUpload { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage("./QuanLyThucDon");

            var monAn = await _context.DICH_VU.AsNoTracking().FirstOrDefaultAsync(d => d.MaDichVu == id);
            if (monAn == null) return RedirectToPage("./QuanLyThucDon");

            MonInfo = new SuaMonViewModel
            {
                MaDichVu = monAn.MaDichVu,
                TenDichVu = monAn.TenDichVu ?? string.Empty,
                LoaiDichVu = string.IsNullOrEmpty(monAn.LoaiDichVu) ? "Món chính" : monAn.LoaiDichVu,
                DonGia = monAn.DonGia ?? 0m,
                HinhAnh = monAn.HinhAnh ?? string.Empty,
                MoTa = monAn.MoTa,
                LuuYDiUng = monAn.CanhBaoDiUng
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                var monAnInDb = await _context.DICH_VU.FirstOrDefaultAsync(d => d.MaDichVu == id);
                if (monAnInDb == null)
                {
                    ModelState.AddModelError(string.Empty, "Không tìm thấy món ăn này trong hệ thống.");
                    return Page();
                }

                if (HinhAnhUpload != null && HinhAnhUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "thuc-don");
                    var (ok, ketQua) = await BaoMatHelper.LuuAnhAnToanAsync(HinhAnhUpload, uploadsFolder, "MON");
                    if (!ok)
                    {
                        ModelState.AddModelError("HinhAnhUpload", ketQua);
                        return Page();
                    }

                    // Chỉ xóa ảnh cũ nếu nằm trong thư mục hợp lệ /images/thuc-don/
                    if (!string.IsNullOrEmpty(monAnInDb.HinhAnh) &&
                        monAnInDb.HinhAnh.StartsWith("/images/thuc-don/", StringComparison.OrdinalIgnoreCase))
                    {
                        string oldFileName = Path.GetFileName(monAnInDb.HinhAnh);
                        string oldFilePath = Path.Combine(uploadsFolder, oldFileName);
                        if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                    }

                    monAnInDb.HinhAnh = "/images/thuc-don/" + ketQua;
                }

                monAnInDb.TenDichVu = MonInfo.TenDichVu.Trim();
                monAnInDb.LoaiDichVu = MonInfo.LoaiDichVu;
                monAnInDb.DonGia = MonInfo.DonGia;
                monAnInDb.MoTa = MonInfo.MoTa?.Trim();
                monAnInDb.CanhBaoDiUng = MonInfo.LuuYDiUng?.Trim();

                await _context.SaveChangesAsync();
                return RedirectToPage("./QuanLyThucDon");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật món ăn {Id}", id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi cập nhật món ăn.");
                return Page();
            }
        }
    }
}
