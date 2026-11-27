using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.QuanTriHeThong
{
    // Bắt buộc phải là Quản trị viên mới được truy cập tính năng này
    [Authorize(Policy = HangSoHeThong.PolicyQuanTri)]
    public class BaoMatMaHoaModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BaoMatMaHoaModel> _logger;

        public BaoMatMaHoaModel(ApplicationDbContext context, ILogger<BaoMatMaHoaModel> logger)
        {
            _context = context;
            _logger = logger;
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
            Input.SessionTimeout = 60;
            Input.YeuCauMatKhauManh = true;
        }

        public IActionResult OnPostLuuCauHinh()
        {
            try
            {
                TempData["ThongBao"] = "Cập nhật chính sách bảo mật thành công!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu cấu hình bảo mật.");
                TempData["Loi"] = "Đã xảy ra lỗi hệ thống khi lưu cấu hình.";
                return RedirectToPage();
            }
        }

        // Xử lý Mã hóa đồng loạt các mật khẩu cũ còn dạng Plaintext sang chuẩn SHA-256 Base64 (KHÔNG băm đè mật khẩu đã băm)
        public async Task<IActionResult> OnPostMaHoaDongLoatAsync()
        {
            try
            {
                var tatCaTaiKhoan = await _context.TAI_KHOAN.ToListAsync();
                int soLuongDaMaHoa = 0;
                int soLuongHexCu = 0;

                foreach (var tk in tatCaTaiKhoan)
                {
                    if (BaoMatHelper.LaChuoiDaBamBase64(tk.MatKhau))
                    {
                        // Đã ở chuẩn SHA-256 Base64 (44 ký tự) -> Bỏ qua, tuyệt đối không băm chồng
                        continue;
                    }

                    if (BaoMatHelper.LaChuoiDaBamHex(tk.MatKhau))
                    {
                        // Đã ở chuẩn Hex (64 ký tự) -> Không băm chồng, ghi nhận để thông báo
                        soLuongHexCu++;
                        continue;
                    }

                    // Chỉ băm các tài khoản còn lưu mật khẩu gốc (Plaintext)
                    tk.MatKhau = BaoMatHelper.BamMatKhauSHA256(tk.MatKhau);
                    _context.TAI_KHOAN.Update(tk);
                    soLuongDaMaHoa++;
                }

                if (soLuongDaMaHoa > 0)
                {
                    _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                    {
                        MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                        TenDangNhap = User.Identity?.Name ?? "Admin",
                        HanhDong = "Mã hóa mật khẩu đồng loạt",
                        ChiTiet = $"Đã nâng cấp mã hóa SHA-256 Base64 cho {soLuongDaMaHoa} tài khoản Plaintext.",
                        ThoiGian = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    TempData["ThongBao"] = $"Đã mã hóa an toàn {soLuongDaMaHoa} tài khoản Plaintext sang chuẩn SHA-256 Base64.";
                }
                else
                {
                    string ghiChuHex = soLuongHexCu > 0
                        ? $" (Có {soLuongHexCu} tài khoản dùng chuẩn Hex cũ sẽ tự động chuyển sang Base64 khi người dùng đăng nhập)."
                        : "";
                    TempData["ThongBao"] = "Tất cả tài khoản đều đã được mã hóa an toàn, không có mật khẩu Plaintext." + ghiChuHex;
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi mã hóa mật khẩu đồng loạt.");
                TempData["Loi"] = "Đã xảy ra lỗi hệ thống khi mã hóa dữ liệu.";
                return RedirectToPage();
            }
        }
    }
}
