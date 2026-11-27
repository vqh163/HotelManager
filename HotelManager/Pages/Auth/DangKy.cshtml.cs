using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.Auth
{
    public class DangKyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DangKyModel> _logger;

        public DangKyModel(ApplicationDbContext context, ILogger<DangKyModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Đóng gói dữ liệu đầu vào với BindProperty chuẩn PascalCase
        [BindProperty]
        public DangKyInputModel Input { get; set; } = new DangKyInputModel();

        public string ThongBaoLoi { get; set; } = string.Empty;

        public class DangKyInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
            [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự.")]
            public string HoTen { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
            [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng số 0.")]
            public string SDT { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Địa chỉ Email.")]
            [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập.")]
            [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
            public string TenDangNhap { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
            [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
            public string MatKhau { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Xác nhận mật khẩu.")]
            [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp!")]
            public string XacNhanMatKhau { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ThongBaoLoi = "Vui lòng kiểm tra lại các thông tin đăng ký chưa hợp lệ!";
                return Page();
            }

            try
            {
                string tenDangNhapChuan = Input.TenDangNhap.Trim();

                // Kiểm tra trùng lặp Tên đăng nhập không phân biệt hoa thường bằng LINQ
                bool taiKhoanTonTai = await _context.TAI_KHOAN
                    .AnyAsync(t => t.TenDangNhap.ToLower() == tenDangNhapChuan.ToLower());

                if (taiKhoanTonTai)
                {
                    ThongBaoLoi = "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác!";
                    return Page();
                }

                // 1. Tạo bản ghi Khách hàng mới (mã sinh chống trùng lặp)
                string maKhachHangMoi = BaoMatHelper.SinhMaNgauNhien("KH");
                var khachHang = new KHACH_HANG
                {
                    MaKH = maKhachHangMoi,
                    HoTen = Input.HoTen.Trim(),
                    SDT = Input.SDT.Trim(),
                    Email = Input.Email.Trim(),
                    HangThanhVien = "Thành viên mới",
                    DiemThuong = 0,
                    Blacklist = "Không"
                };
                _context.KHACH_HANG.Add(khachHang);

                // 2. Tạo bản ghi Tài khoản với mật khẩu đã băm chuẩn SHA-256 Base64
                string maTaiKhoanMoi = BaoMatHelper.SinhMaNgauNhien("TK");
                var taiKhoan = new TAI_KHOAN
                {
                    MaTaiKhoan = maTaiKhoanMoi,
                    KHACH_HANGMaKH = maKhachHangMoi,
                    TenDangNhap = tenDangNhapChuan,
                    MatKhau = BaoMatHelper.BamMatKhauSHA256(Input.MatKhau),
                    VaiTro = HangSoHeThong.VaiTroKhachHang,
                    TrangThai = "Chờ xác thực"
                };
                _context.TAI_KHOAN.Add(taiKhoan);

                await _context.SaveChangesAsync();

                // 3. Khởi tạo phiên OTP hợp lệ trong Session (hiệu lực 5 phút, tối đa 5 lần thử)
                string generatedOtp = "123456"; // Giữ mã demo 123456 theo cấu hình đồ án
                string expIso = DateTime.UtcNow.AddMinutes(5).ToString("O");
                HttpContext.Session.SetString($"OTP_DATA_{tenDangNhapChuan}", $"{generatedOtp}|DangKy|{expIso}|0");

                return RedirectToPage("/Auth/XacThucOTP", new
                {
                    Loai = "DangKy",
                    TaiKhoan = tenDangNhapChuan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống trong quá trình đăng ký tài khoản.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống trong quá trình đăng ký. Vui lòng thử lại sau!";
                return Page();
            }
        }
    }
}
