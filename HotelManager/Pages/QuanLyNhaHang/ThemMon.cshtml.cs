using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.NhaBep
{
    public class ThemMonViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã món.")]
        public string MaDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên món.")]
        public string TenDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        public string LoaiDichVu { get; set; } = "Món chính";

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
        public decimal DonGia { get; set; }

        // Thêm dấu ? để cho phép thuộc tính Mô tả để trống
        public string? MoTa { get; set; }

        // ĐÃ SỬA LỖI: Thêm dấu ? để biến Lưu ý dị ứng thành trường không bắt buộc (Optional)
        public string? LuuYDiUng { get; set; }

        public IFormFile? HinhAnhUpload { get; set; }

        public string HinhAnh { get; set; } = string.Empty;
    }

    public class ThemMonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ThemMonModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
                bool isExist = await _context.DICH_VU.AnyAsync(d => d.MaDichVu == ThemMonInfo.MaDichVu);
                if (isExist)
                {
                    ModelState.AddModelError("ThemMonInfo.MaDichVu", "Mã món (SKU) này đã tồn tại. Vui lòng nhập mã khác.");
                    return Page();
                }

                // Xử lý lưu hình ảnh
                if (ThemMonInfo.HinhAnhUpload != null && ThemMonInfo.HinhAnhUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "thuc-don");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + ThemMonInfo.HinhAnhUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ThemMonInfo.HinhAnhUpload.CopyToAsync(fileStream);
                    }

                    ThemMonInfo.HinhAnh = "/images/thuc-don/" + uniqueFileName;
                }

                var dichVuMoi = new DICH_VU
                {
                    MaDichVu = ThemMonInfo.MaDichVu,
                    TenDichVu = ThemMonInfo.TenDichVu,
                    LoaiDichVu = ThemMonInfo.LoaiDichVu,
                    DonGia = ThemMonInfo.DonGia,
                    DonViTinh = "Phần",
                    HinhAnh = ThemMonInfo.HinhAnh
                };

                _context.DICH_VU.Add(dichVuMoi);
                await _context.SaveChangesAsync();

                // Lưu dữ liệu tùy chọn vào RAM, nếu người dùng để trống thì lưu chuỗi rỗng
                TrangThaiMonStore.MoTaDict[dichVuMoi.MaDichVu] = ThemMonInfo.MoTa ?? "";
                TrangThaiMonStore.DiUngDict[dichVuMoi.MaDichVu] = ThemMonInfo.LuuYDiUng ?? "";

                return RedirectToPage("./QuanLyThucDon");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi lưu món ăn: " + ex.Message);
                return Page();
            }
        }
    }
}