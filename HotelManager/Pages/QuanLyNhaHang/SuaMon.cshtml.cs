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
        // Mã món không được phép sửa, chỉ để hiển thị
        public string MaDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên món.")]
        public string TenDichVu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        public string LoaiDichVu { get; set; } = "Món chính";

        [Required(ErrorMessage = "Vui lòng nhập giá bán.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0.")]
        public decimal DonGia { get; set; }

        public string MoTa { get; set; } = string.Empty;

        // Đường dẫn ảnh hiện tại lưu trong DB
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

        // Biến riêng hứng file upload mới (nếu có)
        [BindProperty]
        public IFormFile? HinhAnhUpload { get; set; }

        // 1. HÀM LOAD DỮ LIỆU CŨ LÊN FORM
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyThucDon");
            }

            var monAn = await _context.DICH_VU.FirstOrDefaultAsync(d => d.MaDichVu == id);

            if (monAn == null)
            {
                return RedirectToPage("./QuanLyThucDon");
            }

            // Đổ dữ liệu từ DB vào ViewModel để hiển thị lên Form HTML
            MonInfo = new SuaMonViewModel
            {
                MaDichVu = monAn.MaDichVu,
                TenDichVu = monAn.TenDichVu,
                LoaiDichVu = string.IsNullOrEmpty(monAn.LoaiDichVu) ? "Món chính" : monAn.LoaiDichVu,
                DonGia = monAn.DonGia ?? 0m,
                HinhAnh = monAn.HinhAnh ?? "", // Gắn ảnh cũ vào
                MoTa = "Thành phần cơ bản của món ăn..." // Chỗ này nếu DB bạn có cột Mô tả thì thay bằng monAn.MoTa
            };

            return Page();
        }

        // 2. HÀM LƯU DỮ LIỆU SAU KHI CHỈNH SỬA
        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var monAnInDb = await _context.DICH_VU.FirstOrDefaultAsync(d => d.MaDichVu == id);

                if (monAnInDb == null)
                {
                    ModelState.AddModelError(string.Empty, "Không tìm thấy món ăn này trong hệ thống.");
                    return Page();
                }

                // Xử lý upload ảnh mới nếu Đầu bếp có chọn file
                if (HinhAnhUpload != null && HinhAnhUpload.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "thuc-don");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + HinhAnhUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await HinhAnhUpload.CopyToAsync(fileStream);
                    }

                    // Xóa ảnh vật lý cũ để giải phóng bộ nhớ (Optional)
                    if (!string.IsNullOrEmpty(monAnInDb.HinhAnh))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath, monAnInDb.HinhAnh.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Cập nhật đường dẫn ảnh mới vào DB
                    monAnInDb.HinhAnh = "/images/thuc-don/" + uniqueFileName;
                }
                else
                {
                    // NẾU KHÔNG CHỌN ẢNH MỚI: Hệ thống tự động giữ nguyên monAnInDb.HinhAnh cũ
                }

                // Cập nhật các thông tin khác
                monAnInDb.TenDichVu = MonInfo.TenDichVu;
                monAnInDb.LoaiDichVu = MonInfo.LoaiDichVu;
                monAnInDb.DonGia = MonInfo.DonGia;

                // Lưu thay đổi xuống CSDL
                await _context.SaveChangesAsync();

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