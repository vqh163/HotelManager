using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System.ComponentModel.DataAnnotations;

namespace HotelManager.Pages.Auth
{
    public class DangKyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DangKyModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lớp ViewModel chứa dữ liệu nhập vào từ người dùng
        public class DangKyInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên")]
            public string HoTen { get; set; } = default!;

            [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
            public string SDT { get; set; } = default!;

            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
            public string Email { get; set; } = default!;

            [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
            public string TenDangNhap { get; set; } = default!;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
            public string MatKhau { get; set; } = default!;
        }

        // Đóng gói input vào thuộc tính có BindProperty để nhận dữ liệu từ phương thức POST
        [BindProperty]
        public DangKyInputModel Input { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = "";
        public string ThongBaoThanhCong { get; set; } = "";

        public void OnGet()
        {
            // Hiển thị form đăng ký trắng
        }

        public async Task<IActionResult>
        OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page(); // Trả về trang kèm các lỗi Validation
            }

            try
            {
                // Dùng LINQ kiểm tra xem tên đăng nhập đã tồn tại chưa
                var tkTonTai = await _context.TAI_KHOAN
                .FirstOrDefaultAsync(t => t.TenDangNhap == Input.TenDangNhap);
                if (tkTonTai != null)
                {
                    ThongBaoLoi = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
                    return Page();
                }

                // 1. Tạo thông tin khách hàng trước (Bảng KHACH_HANG)
                string maKhachHangMoi = "KH" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var khachHangMoi = new KHACH_HANG
                {
                    MaKH = maKhachHangMoi,
                    HoTen = Input.HoTen,
                    SDT = Input.SDT,
                    Email = Input.Email,
                    HangThanhVien = "Bạc", // Mặc định khi mới đăng ký
                    DiemThuong = 0
                };
                _context.KHACH_HANG.Add(khachHangMoi);

                // 2. Tạo tài khoản đăng nhập liên kết với Khách hàng (Bảng TAI_KHOAN)
                string maTaiKhoanMoi = "TK" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var taiKhoanMoi = new TAI_KHOAN
                {
                    MaTaiKhoan = maTaiKhoanMoi,
                    KHACH_HANGMaKH = maKhachHangMoi,
                    TenDangNhap = Input.TenDangNhap,
                    MatKhau = Input.MatKhau, // (Lưu ý: Thực tế cần mã hóa hash mật khẩu tại đây theo chuẩn bảo mật)
                    VaiTro = "Khách hàng",
                    TrangThai = "Chờ xác thực"
                };
                _context.TAI_KHOAN.Add(taiKhoanMoi);

                // Lưu thay đổi vào CSDL
                await _context.SaveChangesAsync();

                ThongBaoThanhCong = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay.";
                ModelState.Clear(); // Xóa dữ liệu form sau khi thành công
            }
            catch (Exception ex)
            {
                // Bắt lỗi ngoại lệ tránh sập ứng dụng
                ThongBaoLoi = "Lỗi hệ thống trong quá trình đăng ký: " + ex.Message;
            }
            return RedirectToPage("./XacThucOTP", new { TenDangNhap = Input.TenDangNhap });
        }
    }
}