using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.Auth
{
    public class QuenMatKhauModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuenMatKhauModel> _logger;

        public QuenMatKhauModel(ApplicationDbContext context, ILogger<QuenMatKhauModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Đóng gói dữ liệu đầu vào với BindProperty chuẩn PascalCase
        [BindProperty]
        public ForgotPasswordInput Input { get; set; } = new ForgotPasswordInput();

        [BindProperty]
        public int BuocHienTai { get; set; } = 1;

        public string ThongBaoLoi { get; set; } = string.Empty;
        public string ThongBaoThanhCong { get; set; } = string.Empty;

        public class ForgotPasswordInput
        {
            public string ThongTinKhoiPhuc { get; set; } = string.Empty;
            public string MatKhauMoi { get; set; } = string.Empty;
            public string XacNhanMatKhau { get; set; } = string.Empty;
        }

        // Kiểm tra phiên xác thực OTP hợp lệ trong Session phía Server
        private bool LayTaiKhoanDaXacThucOtpTuSession(out string tenDangNhapHopLe)
        {
            tenDangNhapHopLe = string.Empty;
            string? payload = HttpContext.Session.GetString("DA_XAC_THUC_OTP_RESET");
            if (string.IsNullOrEmpty(payload)) return false;

            var parts = payload.Split('|');
            if (parts.Length != 2) return false;

            if (!DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiresUtc))
                return false;

            if (DateTime.UtcNow > expiresUtc)
            {
                HttpContext.Session.Remove("DA_XAC_THUC_OTP_RESET");
                return false;
            }

            tenDangNhapHopLe = parts[0];
            return !string.IsNullOrWhiteSpace(tenDangNhapHopLe);
        }

        public void OnGet()
        {
            BuocHienTai = 1;
        }

        // Hứng luồng từ XacThucOTP trả về — BẮT BUỘC kiểm tra cờ xác thực trong Session
        public IActionResult OnGetChoPhepDoiMatKhau()
        {
            if (!LayTaiKhoanDaXacThucOtpTuSession(out string taiKhoanHopLe))
            {
                ThongBaoLoi = "Phiên xác thực OTP không hợp lệ hoặc đã hết hạn. Vui lòng thực hiện lại từ Bước 1!";
                BuocHienTai = 1;
                return Page();
            }

            BuocHienTai = 3;
            Input.ThongTinKhoiPhuc = taiKhoanHopLe;
            ThongBaoThanhCong = "Xác thực OTP thành công! Vui lòng đặt lại mật khẩu mới.";
            return Page();
        }

        // BƯỚC 1: Xử lý khi nhấn nút "Gửi Yêu Cầu"
        public async Task<IActionResult> OnPostGuiYeuCauAsync()
        {
            try
            {
                string thongTin = (Input.ThongTinKhoiPhuc ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(thongTin))
                {
                    ThongBaoLoi = "Vui lòng nhập Tên đăng nhập, Email hoặc Số điện thoại.";
                    BuocHienTai = 1;
                    return Page();
                }

                // Dùng LINQ LEFT JOIN bảng TAI_KHOAN và KHACH_HANG để tìm theo Tên đăng nhập, Email hoặc SĐT
                var user = await (from tk in _context.TAI_KHOAN.AsNoTracking()
                                  join kh in _context.KHACH_HANG.AsNoTracking() on tk.KHACH_HANGMaKH equals kh.MaKH into tk_kh
                                  from kh in tk_kh.DefaultIfEmpty()
                                  where tk.TenDangNhap.ToLower() == thongTin.ToLower() ||
                                        (kh != null && (kh.Email == thongTin || kh.SDT == thongTin))
                                  select tk).FirstOrDefaultAsync();

                if (user != null)
                {
                    string tenDangNhapChuan = user.TenDangNhap;
                    string generatedOtp = "123456"; // Giữ mã demo 123456 theo cấu hình đồ án
                    string expIso = DateTime.UtcNow.AddMinutes(5).ToString("O");

                    // Lưu phiên OTP kèm mục đích và thời hạn vào Session phía Server
                    HttpContext.Session.SetString($"OTP_DATA_{tenDangNhapChuan}", $"{generatedOtp}|QuenMatKhau|{expIso}|0");

                    return RedirectToPage("/Auth/XacThucOTP", new
                    {
                        Loai = "QuenMatKhau",
                        TaiKhoan = tenDangNhapChuan
                    });
                }

                ThongBaoLoi = "Không tìm thấy thông tin tài khoản phù hợp trong hệ thống.";
                BuocHienTai = 1;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi gửi yêu cầu khôi phục mật khẩu.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau!";
                BuocHienTai = 1;
                return Page();
            }
        }

        // BƯỚC 3: Xử lý khi nhấn nút "Đổi Mật Khẩu" — CHỈ đổi cho tài khoản đã xác thực trong Session
        public async Task<IActionResult> OnPostDoiMatKhauAsync()
        {
            try
            {
                // 1. Bảo mật tuyệt đối: Chỉ chấp nhận tài khoản đã vượt qua bước XacThucOTP trong Session
                if (!LayTaiKhoanDaXacThucOtpTuSession(out string taiKhoanDuocPhep))
                {
                    ThongBaoLoi = "Truy cập bị từ chối! Bạn chưa xác thực mã OTP hợp lệ hoặc phiên đã hết hạn.";
                    BuocHienTai = 1;
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(Input.MatKhauMoi) || Input.MatKhauMoi.Length < 6)
                {
                    ThongBaoLoi = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                    Input.ThongTinKhoiPhuc = taiKhoanDuocPhep;
                    BuocHienTai = 3;
                    return Page();
                }

                if (Input.MatKhauMoi != Input.XacNhanMatKhau)
                {
                    ThongBaoLoi = "Mật khẩu xác nhận không khớp.";
                    Input.ThongTinKhoiPhuc = taiKhoanDuocPhep;
                    BuocHienTai = 3;
                    return Page();
                }

                // 2. Truy vấn LINQ theo đúng tài khoản từ Session (không tin dữ liệu giả mạo từ form)
                var user = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.TenDangNhap == taiKhoanDuocPhep);
                if (user != null)
                {
                    // Băm mật khẩu mới chuẩn SHA-256 Base64
                    user.MatKhau = BaoMatHelper.BamMatKhauSHA256(Input.MatKhauMoi);
                    _context.TAI_KHOAN.Update(user);

                    _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                    {
                        MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                        TenDangNhap = user.TenDangNhap,
                        HanhDong = "Khôi phục mật khẩu",
                        ChiTiet = $"Tài khoản '{user.TenDangNhap}' đã đặt lại mật khẩu mới qua luồng OTP.",
                        ThoiGian = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    // Xóa ngay cờ xác thực để không thể tái sử dụng
                    HttpContext.Session.Remove("DA_XAC_THUC_OTP_RESET");

                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
                    return RedirectToPage("/Auth/DangNhap");
                }

                ThongBaoLoi = "Lỗi xác thực dữ liệu người dùng.";
                BuocHienTai = 1;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi đổi mật khẩu.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại!";
                BuocHienTai = 3;
                return Page();
            }
        }
    }
}
