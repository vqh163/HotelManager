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
    public class XoaTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<XoaTaiKhoanModel> _logger;

        public XoaTaiKhoanModel(ApplicationDbContext context, ILogger<XoaTaiKhoanModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TAI_KHOAN TaiKhoanXoa { get; set; } = default!;

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

                TaiKhoanXoa = tk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi truy xuất dữ liệu tài khoản cần xóa.");
                ThongBaoLoi = "Đã xảy ra lỗi khi truy xuất thông tin tài khoản.";
            }

            return Page();
        }

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
                if (tk.MaTaiKhoan == maTaiKhoanDangNhap)
                {
                    ThongBaoLoi = "Không thể tự xóa tài khoản Quản trị viên đang đăng nhập!";
                    TaiKhoanXoa = tk;
                    return Page();
                }

                if (tk.VaiTro == HangSoHeThong.VaiTroQuanTri)
                {
                    int soAdminHoatDong = await _context.TAI_KHOAN.CountAsync(t =>
                        t.VaiTro == HangSoHeThong.VaiTroQuanTri && t.TrangThai == "Hoạt động");
                    if (soAdminHoatDong <= 1)
                    {
                        ThongBaoLoi = "Không thể xóa tài khoản Quản trị viên hoạt động cuối cùng của hệ thống!";
                        TaiKhoanXoa = tk;
                        return Page();
                    }
                }

                _context.TAI_KHOAN.Remove(tk);
                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                    TenDangNhap = User.Identity?.Name ?? "Admin",
                    HanhDong = "Xóa tài khoản",
                    ChiTiet = $"Đã xóa tài khoản '{tk.TenDangNhap}' ({tk.MaTaiKhoan}, Vai trò: {tk.VaiTro}).",
                    ThoiGian = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return RedirectToPage("./QuanLyTaiKhoan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa tài khoản {Id}", id);
                ThongBaoLoi = "Không thể xóa tài khoản này do lỗi ràng buộc dữ liệu hoặc lỗi hệ thống.";
                var tk = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (tk != null) TaiKhoanXoa = tk;
                return Page();
            }
        }
    }
}
