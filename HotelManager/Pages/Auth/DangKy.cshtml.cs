using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using HotelManager.Data; // Thư mục chứa cấu hình DbContext của nhóm
using HotelManager.Models; // Thư mục chứa các class định nghĩa bảng CSDL

namespace HotelManager.Pages.Auth
{
    public class DangKyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DangKyModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Đóng gói dữ liệu đầu vào, bắt buộc dùng BindProperty theo quy chuẩn
        [BindProperty]
        public DangKyInputModel Input { get; set; } = new DangKyInputModel();

        public string ThongBaoLoi { get; set; } = string.Empty;

        // Class chứa các thuộc tính từ form đăng ký (Tuân thủ PascalCase)
        public class DangKyInputModel
        {
            public string HoTen { get; set; } = string.Empty;
            public string SDT { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string TenDangNhap { get; set; } = string.Empty;
            public string MatKhau { get; set; } = string.Empty;
            public string XacNhanMatKhau { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            // Hiển thị form đăng ký trắng khi người dùng truy cập lần đầu
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Bọc toàn bộ thao tác thêm dữ liệu trong try...catch
            try
            {
                // Kiểm tra xác nhận mật khẩu
                if (Input.MatKhau != Input.XacNhanMatKhau)
                {
                    ThongBaoLoi = "Mật khẩu xác nhận không khớp!";
                    return Page();
                }

                // Dùng LINQ truy vấn kiểm tra trùng lặp Tên đăng nhập trong CSDL
                var taiKhoanTonTai = await _context.TAI_KHOAN
                    .FirstOrDefaultAsync(t => t.TenDangNhap == Input.TenDangNhap);

                if (taiKhoanTonTai != null)
                {
                    ThongBaoLoi = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
                    return Page();
                }

                // 1. Tạo bản ghi Khách hàng mới (Bảng KHACH_HANG)
                string maKhachHangMoi = "KH" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var khachHang = new KHACH_HANG
                {
                    MaKH = maKhachHangMoi,
                    HoTen = Input.HoTen,
                    SDT = Input.SDT,
                    Email = Input.Email,
                    HangThanhVien = "Bạc", // Hạng mặc định
                    DiemThuong = 0
                };
                _context.KHACH_HANG.Add(khachHang);

                // 2. Tạo bản ghi Tài khoản liên kết (Bảng TAI_KHOAN)
                string maTaiKhoanMoi = "TK" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var taiKhoan = new TAI_KHOAN
                {
                    MaTaiKhoan = maTaiKhoanMoi,
                    KHACH_HANGMaKH = maKhachHangMoi, // Khóa ngoại liên kết tới khách hàng
                    TenDangNhap = Input.TenDangNhap,
                    MatKhau = Input.MatKhau, // Hệ thống thực tế sẽ hash mật khẩu ở bước này
                    VaiTro = "Khách hàng",
                    TrangThai = "Chờ xác thực" // Trạng thái chờ kích hoạt OTP
                };
                _context.TAI_KHOAN.Add(taiKhoan);

                // Lưu thay đổi đồng thời vào 2 bảng
                await _context.SaveChangesAsync();

                // 3. Xử lý cấp mã OTP mặc định (123456) và lưu vào Session
                string generatedOtp = "123456";
                HttpContext.Session.SetString("OTP_" + Input.TenDangNhap, generatedOtp);

                // 4. Chuyển hướng sang trang dùng chung XacThucOTP, truyền đúng 2 tham số
                return RedirectToPage("/Auth/XacThucOTP", new
                {
                    Loai = "DangKy",
                    TaiKhoan = Input.TenDangNhap
                });
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống trong quá trình đăng ký: " + ex.Message;
                return Page();
            }
        }
    }
}
