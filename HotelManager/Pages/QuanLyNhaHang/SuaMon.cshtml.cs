using HotelManager.Data;
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

        // Bổ sung dấu ? để cho phép Null
        public string? MoTa { get; set; }

        // ĐÃ SỬA LỖI: Bổ sung dấu ? để không bắt buộc nhập dị ứng
        public string? LuuYDiUng { get; set; }

        public string HinhAnh { get; set; } = string.Empty;
    }

    public class SuaMonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SuaMonModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public SuaMonViewModel MonInfo { get; set; } = new SuaMonViewModel();

        [BindProperty]
        public IFormFile? HinhAnhUpload { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage("./QuanLyThucDon");

            var monAn = await _context.DICH_VU.FirstOrDefaultAsync(d => d.MaDichVu == id);
            if (monAn == null) return RedirectToPage("./QuanLyThucDon");

            string moTaCache = TrangThaiMonStore.MoTaDict.ContainsKey(id) ? TrangThaiMonStore.MoTaDict[id] : "";
            string diUngCache = TrangThaiMonStore.DiUngDict.ContainsKey(id) ? TrangThaiMonStore.DiUngDict[id] : "";

            MonInfo = new SuaMonViewModel
            {
                MaDichVu = monAn.MaDichVu,
                TenDichVu = monAn.TenDichVu ?? string.Empty,
                LoaiDichVu = string.IsNullOrEmpty(monAn.LoaiDichVu) ? "Món chính" : monAn.LoaiDichVu,
                DonGia = monAn.DonGia ?? 0m,
                HinhAnh = monAn.HinhAnh ?? "",
                MoTa = moTaCache,
                LuuYDiUng = diUngCache
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
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + HinhAnhUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhUpload.CopyToAsync(fileStream);
                    }

                    if (!string.IsNullOrEmpty(monAnInDb.HinhAnh))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath, monAnInDb.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                    }

                    monAnInDb.HinhAnh = "/images/thuc-don/" + uniqueFileName;
                }

                monAnInDb.TenDichVu = MonInfo.TenDichVu;
                monAnInDb.LoaiDichVu = MonInfo.LoaiDichVu;
                monAnInDb.DonGia = MonInfo.DonGia;

                await _context.SaveChangesAsync();

                // Lưu thay đổi vào bộ nhớ RAM (xử lý Null Coalescing cho an toàn)
                TrangThaiMonStore.MoTaDict[id] = MonInfo.MoTa ?? "";
                TrangThaiMonStore.DiUngDict[id] = MonInfo.LuuYDiUng ?? "";

                TrangThaiMonStore.SaveData();

                return RedirectToPage("./QuanLyThucDon");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật món ăn: " + ex.Message);
                return Page();
            }
        }
    }
}