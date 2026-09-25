using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            public string? MaNhanVien { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ThongBaoLoi { get; set; } = "";

        // Chứa danh sách vai trò (chức vụ) lấy từ DB
        public SelectList DanhSachVaiTro { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Chỉ truy vấn lấy các chức vụ nội bộ từ bảng CHUC_VU
            var chucVus = await _context.CHUC_VU.Select(c => c.TenChucVu).ToListAsync();

            // Đưa vào SelectList
            DanhSachVaiTro = new SelectList(chucVus);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Load lại danh sách nhân sự nếu form lỗi
                var chucVus = await _context.CHUC_VU.Select(c => c.TenChucVu).ToListAsync();
                DanhSachVaiTro = new SelectList(chucVus);

                return Page();
            }

            try
            {
                bool daTonTai = await _context.TAI_KHOAN.AnyAsync(t => t.TenDangNhap == Input.TenDangNhap);
                if (daTonTai)
                {
                    ThongBaoLoi = "Tên đăng nhập này đã tồn tại trong hệ thống. Vui lòng chọn tên khác!";

                    var chucVus = await _context.CHUC_VU.Select(c => c.TenChucVu).ToListAsync();
                    DanhSachVaiTro = new SelectList(chucVus);

                    return Page();
                }

                var taiKhoanMoi = new TAI_KHOAN
                {
                    MaTaiKhoan = "TK" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TenDangNhap = Input.TenDangNhap,
                    MatKhau = Input.MatKhau,
                    VaiTro = Input.VaiTro,
                    TrangThai = Input.TrangThai,
                    NHAN_VIENMaNV = string.IsNullOrWhiteSpace(Input.MaNhanVien) ? null : Input.MaNhanVien
                    // LƯU Ý: Quản trị viên chỉ tạo tài khoản cho nhân viên nên không truyền KHACH_HANGMaKH
                };

                _context.TAI_KHOAN.Add(taiKhoanMoi);
                await _context.SaveChangesAsync();

                return RedirectToPage("./QuanLyTaiKhoan");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi lưu CSDL: " + ex.Message;

                var chucVus = await _context.CHUC_VU.Select(c => c.TenChucVu).ToListAsync();
                DanhSachVaiTro = new SelectList(chucVus);

                return Page();
            }
        }
    }
}