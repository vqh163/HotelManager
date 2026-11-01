using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace HotelManager.Pages.QuanLyNhanSu
{
    public class ThemNhanVienModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ThemNhanVienModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public NhanVienInputModel Input { get; set; } = new NhanVienInputModel();

        public SelectList DanhSachChucVu { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = string.Empty;

        public class NhanVienInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
            public string HoTen { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập CCCD.")]
            [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải đúng 12 số.")]
            public string SoCCCD { get; set; } = string.Empty;

            public string GioiTinh { get; set; } = "Nam";

            [Required]
            public DateTime NgaySinh { get; set; } = DateTime.Now.AddYears(-20);

            [Required]
            [RegularExpression(@"^\d{10}$", ErrorMessage = "SDT phải đúng 10 số.")]
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
            string[] vaiTroLoaiBo = { "Khách hàng", "Giám đốc", "Quản lý", "Quản trị viên hệ thống", "Quản trị viên" };

            var chucVus = await _context.CHUC_VU
                .Where(cv => !vaiTroLoaiBo.Contains(cv.TenChucVu))
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

                string chuoiTienTe = Input.LuongCoBan.Replace(",", "").Replace(".", "");
                if (!decimal.TryParse(chuoiTienTe, out decimal luongChuan))
                {
                    ThongBaoLoi = "Định dạng mức lương không hợp lệ.";
                    await LoadDropdownChucVuAsync();
                    return Page();
                }

                bool isDuplicate = await _context.NHAN_VIEN.AnyAsync(n => n.SoCCCD == Input.SoCCCD || n.SDT == Input.SDT || n.Email == Input.Email);
                if (isDuplicate)
                {
                    ThongBaoLoi = "CCCD, SĐT hoặc Email đã được đăng ký!";
                    await LoadDropdownChucVuAsync();
                    return Page();
                }

                var nhanVienMoi = new NHAN_VIEN
                {
                    MaNV = "NV" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    HoTen = Input.HoTen,
                    SoCCCD = Input.SoCCCD,
                    GioiTinh = Input.GioiTinh,
                    NgaySinh = Input.NgaySinh,
                    SDT = Input.SDT,
                    Email = Input.Email,
                    NgayVaoLam = Input.NgayVaoLam
                };

                _context.NHAN_VIEN.Add(nhanVienMoi);
                await _context.SaveChangesAsync();

                var chucVu = await _context.CHUC_VU.FirstOrDefaultAsync(c => c.MaChucVu == Input.MaChucVu);
                string tenChucVuThucTe = chucVu != null ? chucVu.TenChucVu : "Nhân viên";

                try
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "nhanvien_meta.txt");
                    string dataLine = $"{nhanVienMoi.MaNV}|{tenChucVuThucTe}|{luongChuan}\n";
                    System.IO.File.AppendAllText(filePath, dataLine);
                }
                catch { }

                TempData["ThongBao"] = $"Thêm mới nhân viên '{Input.HoTen}' thành công!";
                return RedirectToPage("./QuanLyDanhSachNhanVien");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống: " + ex.Message;
                await LoadDropdownChucVuAsync();
                return Page();
            }
        }
    }
}