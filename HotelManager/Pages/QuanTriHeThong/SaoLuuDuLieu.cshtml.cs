using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class SaoLuuDuLieuModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SaoLuuDuLieuModel> _logger;

        public SaoLuuDuLieuModel(ApplicationDbContext context, IWebHostEnvironment env, ILogger<SaoLuuDuLieuModel> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public string ThongBao { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = false;

        public void OnGet()
        {
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

                // 2. Lấy tên Database thực tế từ chuỗi kết nối (HotelManagerDB) thay vì gõ cứng
                string dbName = _context.Database.GetDbConnection().Database;
                if (string.IsNullOrWhiteSpace(dbName)) dbName = "HotelManagerDB";

                string safeDbName = dbName.Replace("]", "]]");
                string fileName = $"{dbName}_Backup_{DateTime.Now:yyyyMMddHHmmss}.bak";
                string backupPath = Path.Combine(backupFolder, fileName);

                // 3. Thực thi lệnh BACKUP DATABASE có tham số hóa đường dẫn tệp (an toàn chống SQL Injection)
                string sqlCommand = $"BACKUP DATABASE [{safeDbName}] TO DISK = {{0}} WITH INIT, FORMAT";
                await _context.Database.ExecuteSqlRawAsync(sqlCommand, backupPath);

                // 4. Ghi nhật ký hệ thống
                var nhatKy = new NHAT_KY_HE_THONG
                {
                    MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                    TenDangNhap = User.Identity?.Name ?? "Admin",
                    HanhDong = "Sao lưu CSDL",
                    ChiTiet = $"Đã sao lưu CSDL [{dbName}] thành công tại tệp: {fileName}",
                    ThoiGian = DateTime.Now
                };
                _context.NHAT_KY_HE_THONG.Add(nhatKy);
                await _context.SaveChangesAsync();

                IsSuccess = true;
                ThongBao = $"Sao lưu dữ liệu thành công! Tệp sao lưu: {fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sao lưu cơ sở dữ liệu.");
                IsSuccess = false;
                ThongBao = "Không thể sao lưu cơ sở dữ liệu lúc này. Vui lòng kiểm tra quyền ghi thư mục của SQL Server.";
            }

            return Page();
        }
    }
}
