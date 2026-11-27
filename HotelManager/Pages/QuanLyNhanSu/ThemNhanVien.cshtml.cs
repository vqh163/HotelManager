using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyNhanSu
{
    [Authorize(Policy = HangSoHeThong.PolicyNhanSu)]
    public class ThemNhanVienModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ThemNhanVienModel> _logger;

        public ThemNhanVienModel(ApplicationDbContext context, ILogger<ThemNhanVienModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public NhanVienInputModel Input { get; set; } = new NhanVienInputModel();

        public SelectList DanhSachChucVu { get; set; } = default!;
        public string ThongBaoLoi { get; set; } = string.Empty;

        public class NhanVienInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
            public string HoTen { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập CCCD.")]
            [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải đúng 12 chữ số.")]
            public string SoCCCD { get; set; } = string.Empty;

            public string GioiTinh { get; set; } = "Nam";

            [Required]
            public DateTime NgaySinh { get; set; } = DateTime.Now.AddYears(-20);

            [Required]
            [RegularExpression(@"^\d{10}$", ErrorMessage = "SĐT phải đúng 10 chữ số.")]
            public string SDT { get; set; } = string.Empty;

            [Required, EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            public string Email { get; set; } = string.Empty;

            public DateTime NgayVaoLam { get; set; } = DateTime.Now;

            [Required(ErrorMessage = "Vui lòng chọn chức vụ.")]
            public string MaChucVu { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mức lương.")]
            public string LuongCoBan { get; set; } = string.Empty;
        }

        private async Task LoadDropdownChucVuAsync()
        {
            var chucVus = await _context.CHUC_VU.AsNoTracking()
                .Where(cv => cv.TenChucVu != HangSoHeThong.VaiTroKhachHang)
                .OrderBy(cv => cv.MaChucVu)
                .ToListAsync();

            DanhSachChucVu = new SelectList(chucVus, "MaChucVu", "TenChucVu");
        }

        public async Task OnGetAsync() => await LoadDropdownChucVuAsync();

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDropdownChucVuAsync();
                    ThongBaoLoi = "Vui lòng kiểm tra lại thông tin nhập liệu.";
                    return Page();
                }

                string chuoiTienTe = Input.LuongCoBan.Replace(",", "").Replace(".", "").Trim();
                if (!decimal.TryParse(chuoiTienTe, out decimal luongChuan) || luongChuan <= 0)
                {
                    ThongBaoLoi = "Mức lương cơ bản phải là số dương hợp lệ!";
                    await LoadDropdownChucVuAsync();
                    return Page();
                }

                string cccd = Input.SoCCCD.Trim();
                string sdt = Input.SDT.Trim();
                string email = Input.Email.Trim();

                bool isDuplicate = await _context.NHAN_VIEN.AsNoTracking()
                    .AnyAsync(n => n.SoCCCD == cccd || n.SDT == sdt || n.Email == email);
                if (isDuplicate)
                {
                    ThongBaoLoi = "CCCD, SĐT hoặc Email đã được đăng ký cho nhân viên khác!";
                    await LoadDropdownChucVuAsync();
                    return Page();
                }

                // Lưu đồng bộ CHUC_VUMaChucVu và LuongCoBan thẳng vào SQL Server (bỏ file nhanvien_meta.txt)
                var nhanVienMoi = new NHAN_VIEN
                {
                    MaNV = BaoMatHelper.SinhMaNgauNhien("NV"),
                    HoTen = Input.HoTen.Trim(),
                    SoCCCD = cccd,
                    GioiTinh = Input.GioiTinh,
                    NgaySinh = Input.NgaySinh,
                    SDT = sdt,
                    Email = email,
                    NgayVaoLam = Input.NgayVaoLam,
                    CHUC_VUMaChucVu = Input.MaChucVu,
                    LuongCoBan = luongChuan
                };

                _context.NHAN_VIEN.Add(nhanVienMoi);
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Thêm mới nhân viên '{nhanVienMoi.HoTen}' thành công!";
                return RedirectToPage("./QuanLyDanhSachNhanVien");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm mới nhân viên.");
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi thêm mới nhân viên.";
                await LoadDropdownChucVuAsync();
                return Page();
            }
        }
    }
}
