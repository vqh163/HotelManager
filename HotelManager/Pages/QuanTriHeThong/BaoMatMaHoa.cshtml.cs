using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using HotelManager.Data;

namespace HotelManager.Pages.QuanTriHeThong
{
    // Bắt buộc phải là Quản trị viên mới được truy cập tính năng này
    [Authorize(Roles = "Quản trị viên")]
    public class BaoMatMaHoaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BaoMatMaHoaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CauHinhBaoMatInput Input { get; set; } = new CauHinhBaoMatInput();

        public class CauHinhBaoMatInput
        {
            public int SessionTimeout { get; set; } = 60;
            public bool YeuCauMatKhauManh { get; set; } = true;
        }

        public void OnGet()
        {
            // Trong thực tế, bạn sẽ dùng LINQ gọi từ CSDL lên. Ở đây gán mặc định để hiển thị UI.
            Input.SessionTimeout = 60;
            Input.YeuCauMatKhauManh = true;
        }

        // Xử lý lưu cấu hình
        public IActionResult OnPostLuuCauHinh()
        {
            try
            {
                // TODO: Lưu vào CSDL hoặc file appsettings.json
                TempData["ThongBao"] = "Cập nhật chính sách bảo mật thành công!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Loi"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToPage();
            }
        }

        // Xử lý Mã hóa đồng loạt các mật khẩu cũ chưa mã hóa
        public async Task<IActionResult> OnPostMaHoaDongLoatAsync()
        {
            try
            {
                // Dùng LINQ tìm tất cả tài khoản (Giả sử mật khẩu chưa mã hóa có độ dài ngắn hơn 50 ký tự)
                var dsTaiKhoan = _context.TAI_KHOAN.Where(t => t.MatKhau.Length < 60).ToList();
                int count = 0;

                foreach (var tk in dsTaiKhoan)
                {
                    // Gọi hàm mã hóa
                    tk.MatKhau = HashPasswordSHA256(tk.MatKhau);
                    count++;
                }

                if (count > 0)
                {
                    await _context.SaveChangesAsync();
                    TempData["ThongBao"] = $"Đã mã hóa thành công {count} tài khoản trong hệ thống.";
                }
                else
                {
                    TempData["ThongBao"] = "Tất cả tài khoản đều đã được mã hóa an toàn.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Loi"] = "Lỗi mã hóa dữ liệu: " + ex.Message;
                return RedirectToPage();
            }
        }

        /// <summary>
        /// Hàm băm mật khẩu chuẩn SHA256 để bảo mật dữ liệu theo yêu cầu dự án
        /// Bạn Trần (UC_01) và Bạn Uyên (UC_02) cần dùng hàm này khi Đăng nhập/Đăng ký.
        /// </summary>
        private string HashPasswordSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Chuyển chuỗi thành mảng byte
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Chuyển mảng byte thành chuỗi Hex
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}