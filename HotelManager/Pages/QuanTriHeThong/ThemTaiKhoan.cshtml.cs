using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System.ComponentModel.DataAnnotations;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class ThemTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ThemTaiKhoanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lớp ViewModel chứa dữ liệu nhập từ Form của Admin
        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
            public string TenDangNhap { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            public string MatKhau { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn vai trò")]
            public string VaiTro { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
            public string TrangThai { get; set; } = "Hoạt động";

            // Tùy chọn: Liên kết tài khoản này với một nhân viên cụ thể
            public string? MaNhanVien { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ThongBaoLoi { get; set; } = "";

        public void OnGet()
        {
            // Hàm khởi tạo trang khi được gọi qua phương thức GET
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Kiểm tra xem dữ liệu Admin nhập vào đã thỏa mãn các điều kiện Required chưa
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Sử dụng LINQ kiểm tra tên đăng nhập đã tồn tại dưới Database chưa
                bool daTonTai = await _context.TAI_KHOAN.AnyAsync(t => t.TenDangNhap == Input.TenDangNhap);
                if (daTonTai)
                {
                    ThongBaoLoi = "Tên đăng nhập này đã tồn tại trong hệ thống. Vui lòng chọn tên khác!";
                    return Page();
                }

                // Khởi tạo đối tượng TAI_KHOAN mới để thêm vào CSDL
                // Sinh mã tài khoản tự động dựa trên thời gian thực
                var taiKhoanMoi = new TAI_KHOAN
                {
                    MaTaiKhoan = "TK" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TenDangNhap = Input.TenDangNhap,
                    MatKhau = Input.MatKhau, // (Thực tế khi triển khai nên mã hóa Hash)
                    VaiTro = Input.VaiTro,
                    TrangThai = Input.TrangThai,
                    NHAN_VIENMaNV = string.IsNullOrWhiteSpace(Input.MaNhanVien) ? null : Input.MaNhanVien
                };

                // Thêm vào DbSet và lưu thay đổi
                _context.TAI_KHOAN.Add(taiKhoanMoi);
                await _context.SaveChangesAsync();

                // Sau khi thêm thành công, chuyển hướng Admin về lại trang danh sách tài khoản
                return RedirectToPage("./QuanLyTaiKhoan");
            }
            catch (Exception ex)
            {
                // Bắt lỗi ngoại lệ tránh sụp đổ (crash) chương trình
                ThongBaoLoi = "Lỗi hệ thống khi lưu CSDL: " + ex.Message;
                return Page();
            }
        }
    }
}