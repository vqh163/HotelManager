using HotelManager.Data; // Thay bằng namespace Data của bạn
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HotelManager.Pages.QuanTriHeThong
{
    public class SuaTaiKhoanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SuaTaiKhoanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lớp ViewModel chứa dữ liệu cần chỉnh sửa, dùng [BindProperty] để nhận data từ Form
        public class InputModel
        {
            public string MaTaiKhoan { get; set; } = string.Empty;

            public string TenDangNhap { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn vai trò")]
            public string VaiTro { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
            public string TrangThai { get; set; } = string.Empty;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ThongBaoLoi { get; set; } = "";

        // BỔ SUNG: Thuộc tính chứa danh sách chức vụ
        public SelectList DanhSachVaiTro { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("./QuanLyTaiKhoan");
            }

            try
            {
                // Lấy danh sách chức vụ từ CSDL để đưa vào Dropdown
                var danhSachChucVu = await _context.CHUC_VU.ToListAsync();
                DanhSachVaiTro = new SelectList(danhSachChucVu, "TenChucVu", "TenChucVu");

                var taiKhoan = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
                if (taiKhoan == null)
                {
                    return RedirectToPage("./QuanLyTaiKhoan");
                }

                Input = new InputModel
                {
                    MaTaiKhoan = taiKhoan.MaTaiKhoan,
                    TenDangNhap = taiKhoan.TenDangNhap,
                    VaiTro = taiKhoan.VaiTro ?? "",
                    TrangThai = taiKhoan.TrangThai ?? "Hoạt động"
                };
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi lấy dữ liệu tài khoản: " + ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // BỔ SUNG: Phải load lại DanhSachVaiTro nếu Validation lỗi
                var danhSachChucVu = await _context.CHUC_VU.ToListAsync();
                DanhSachVaiTro = new SelectList(danhSachChucVu, "TenChucVu", "TenChucVu");
                return Page();
            }

            try
            {
                var taiKhoan = await _context.TAI_KHOAN.FirstOrDefaultAsync(t => t.MaTaiKhoan == Input.MaTaiKhoan);

                if (taiKhoan == null)
                {
                    ThongBaoLoi = "Không tìm thấy tài khoản để cập nhật!";
                    // Load lại Dropdown
                    var danhSachChucVu = await _context.CHUC_VU.ToListAsync();
                    DanhSachVaiTro = new SelectList(danhSachChucVu, "TenChucVu", "TenChucVu");
                    return Page();
                }

                taiKhoan.VaiTro = Input.VaiTro;
                taiKhoan.TrangThai = Input.TrangThai;

                await _context.SaveChangesAsync();
                return RedirectToPage("./QuanLyTaiKhoan");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi cập nhật CSDL: " + ex.Message;
                // Load lại Dropdown
                var danhSachChucVu = await _context.CHUC_VU.ToListAsync();
                DanhSachVaiTro = new SelectList(danhSachChucVu, "TenChucVu", "TenChucVu");
                return Page();
            }
        }
    }
}