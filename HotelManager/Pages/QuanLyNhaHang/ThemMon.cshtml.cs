using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
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
    public class ThemMonViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã món.")]
        [StringLength(20, ErrorMessage = "Mã món tối đa 20 ký tự.")]
        public string MaDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên món.")]
        [StringLength(100, ErrorMessage = "Tên món tối đa 100 ký tự.")]
        public string TenDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        public string LoaiDichVu { get; set; } = "Món chính";

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
        public decimal DonGia { get; set; }

        public string? MoTa { get; set; }
        public string? LuuYDiUng { get; set; }
        public IFormFile? HinhAnhUpload { get; set; }
        public string HinhAnh { get; set; } = string.Empty;
    }

    public class ThemMonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ThemMonModel> _logger;

        public ThemMonModel(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<ThemMonModel> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public ThemMonViewModel ThemMonInfo { get; set; } = new ThemMonViewModel();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                string maChuan = ThemMonInfo.MaDichVu.Trim();
                bool isExist = await _context.DICH_VU.AnyAsync(d => d.MaDichVu == maChuan);
                if (isExist)
                {
                    ModelState.AddModelError("ThemMonInfo.MaDichVu", "Mã món (SKU) này đã tồn tại. Vui lòng nhập mã khác.");
                    return Page();
                }

                // Kiểm tra và lưu hình ảnh an toàn (chặn tệp giả mạo .svg/.html/quá dung lượng)
                if (ThemMonInfo.HinhAnhUpload != null && ThemMonInfo.HinhAnhUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "thuc-don");
                    var (ok, ketQua) = await BaoMatHelper.LuuAnhAnToanAsync(ThemMonInfo.HinhAnhUpload, uploadsFolder, "MON");
                    if (!ok)
                    {
                        ModelState.AddModelError("ThemMonInfo.HinhAnhUpload", ketQua);
                        return Page();
                    }
                    ThemMonInfo.HinhAnh = "/images/thuc-don/" + ketQua;
                }

                // Lưu toàn bộ thông tin (kể cả MoTa, CanhBaoDiUng, ConMon) thẳng vào SQL Server
                var dichVuMoi = new DICH_VU
                {
                    MaDichVu = maChuan,
                    TenDichVu = ThemMonInfo.TenDichVu.Trim(),
                    LoaiDichVu = ThemMonInfo.LoaiDichVu,
                    DonGia = ThemMonInfo.DonGia,
                    DonViTinh = "Phần",
                    HinhAnh = ThemMonInfo.HinhAnh,
                    MoTa = ThemMonInfo.MoTa?.Trim(),
                    CanhBaoDiUng = ThemMonInfo.LuuYDiUng?.Trim(),
                    ConMon = true
                };

                _context.DICH_VU.Add(dichVuMoi);
                await _context.SaveChangesAsync();

                return RedirectToPage("./QuanLyThucDon");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm mới món ăn.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi lưu món ăn.");
                return Page();
            }
        }
    }
}
