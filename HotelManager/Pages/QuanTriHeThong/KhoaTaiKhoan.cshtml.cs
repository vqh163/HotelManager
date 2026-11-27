using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class KhoaTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KhoaTaiKhoanModel> _logger;

        public KhoaTaiKhoanModel(ApplicationDbContext context, ILogger<KhoaTaiKhoanModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TAI_KHOAN TaiKhoanKhoa { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            try
            {
                var tk = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk == null)
                {
                    return RedirectToPage("./QuanLyTaiKhoan");
                }

                TaiKhoanKhoa = tk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi truy xuất tài khoản để khóa/mở khóa.");
                ThongBaoLoi = "Đã xảy ra lỗi khi truy xuất dữ liệu tài khoản.";
            }

            return Page();
        }

        // Xử lý Khóa hoặc Mở khóa tài khoản an toàn kèm ghi nhật ký hệ thống
        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            try
            {
                var tk = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk == null)
                {
                    return RedirectToPage("./QuanLyTaiKhoan");
                }

                string maTaiKhoanDangNhap = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                bool dangBiKhoa = string.Equals(tk.TrangThai?.Trim(), "Khóa", StringComparison.OrdinalIgnoreCase);

                if (!dangBiKhoa)
                {
                    // Chặn Quản trị viên tự khóa chính tài khoản đang đăng nhập
                    if (tk.MaTaiKhoan == maTaiKhoanDangNhap)
                    {
                        ThongBaoLoi = "Không thể tự khóa tài khoản Quản trị viên đang đăng nhập!";
                        TaiKhoanKhoa = tk;
                        return Page();
                    }

                    // Chặn khóa tài khoản Quản trị viên hoạt động cuối cùng
                    if (tk.VaiTro == HangSoHeThong.VaiTroQuanTri)
                    {
                        int soAdminHoatDong = await _context.TAI_KHOAN.CountAsync(t =>
                            t.VaiTro == HangSoHeThong.VaiTroQuanTri && t.TrangThai == "Hoạt động");
                        if (soAdminHoatDong <= 1)
                        {
                            ThongBaoLoi = "Không thể khóa tài khoản Quản trị viên hoạt động cuối cùng của hệ thống!";
                            TaiKhoanKhoa = tk;
                            return Page();
                        }
                    }

                    tk.TrangThai = "Khóa";
                }
                else
                {
                    // Mở khóa lại tài khoản đang bị khóa
                    tk.TrangThai = "Hoạt động";
                }

                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                    TenDangNhap = User.Identity?.Name ?? "Admin",
                    HanhDong = dangBiKhoa ? "Mở khóa tài khoản" : "Khóa tài khoản",
                    ChiTiet = $"Đã chuyển trạng thái tài khoản '{tk.TenDangNhap}' ({tk.MaTaiKhoan}) sang '{tk.TrangThai}'.",
                    ThoiGian = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return RedirectToPage("./QuanLyTaiKhoan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi khóa/mở khóa tài khoản {Id}", id);
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi cập nhật trạng thái tài khoản.";
                var tk = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk != null) TaiKhoanKhoa = tk;
                return Page();
            }
        }
    }
}
