using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class SaoLuuDuLieuModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Tiêm Dependency Injection cho DbContext và WebHostEnvironment (để lấy đường dẫn thư mục)
        public SaoLuuDuLieuModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Biến lưu trữ thông báo cho UI
        public string ThongBao { get; set; } = "";
        public bool IsSuccess { get; set; } = false;

        public void OnGet()
        {
            // Chỉ hiển thị giao diện mặc định
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // 1. Tạo thư mục Backups nằm trong thư mục gốc của dự án nếu chưa có
                string backupFolder = Path.Combine(_env.ContentRootPath, "Backups");
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                // 2. Đặt tên file backup theo chuẩn thời gian thực để không bị trùng
                string fileName = $"HotelManager_Backup_{DateTime.Now:yyyyMMddHHmmss}.bak";
                string backupPath = Path.Combine(backupFolder, fileName);

                // 3. Chuẩn bị câu lệnh SQL Server thuần để thực thi lệnh BACKUP
                // Lưu ý: Đảm bảo tên Database trong chuỗi này khớp với tên trong appsettings.json của bạn
                string sqlCommand = $"BACKUP DATABASE [HotelManager] TO DISK = '{backupPath}'";

                // 4. Dùng Entity Framework Core để thực thi câu lệnh SQL thuần
                await _context.Database.ExecuteSqlRawAsync(sqlCommand);

                // 5. Ghi log lại hành động này vào bảng NHAT_KY_HE_THONG
                var nhatKy = new NHAT_KY_HE_THONG
                {
                    MaNhatKy = "LOG" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TenDangNhap = User.Identity?.Name ?? "Admin",
                    HanhDong = "Sao lưu CSDL",
                    ChiTiet = $"Đã tạo bản sao lưu dữ liệu thành công tại tệp: {fileName}"
                };
                _context.NHAT_KY_HE_THONG.Add(nhatKy);
                await _context.SaveChangesAsync();

                IsSuccess = true;
                ThongBao = $"Sao lưu dữ liệu thành công! File được lưu tại: {fileName}";
            }
            catch (Exception ex)
            {
                // Bắt lỗi nếu SQL Server không có quyền ghi file hoặc sai tên CSDL
                IsSuccess = false;
                ThongBao = "Đã xảy ra lỗi trong quá trình sao lưu: " + ex.Message;
            }

            return Page();
        }
    }
}