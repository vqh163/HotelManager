using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace HotelManager.Pages.QuanLyNhanSu
{
    public class SuaThongTinModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SuaThongTinModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Binding thông tin nhân viên để map với form HTML
        [BindProperty]
        public NHAN_VIEN Input { get; set; } = default!;

        // Thuộc tính nhận giá trị mã chức vụ người dùng chọn từ thẻ <select>
        [BindProperty]
        public string MaChucVuSelected { get; set; } = string.Empty;

        // Tập dữ liệu đổ vào thẻ <select> chức vụ
        public SelectList DanhSachChucVuSelectList { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = string.Empty;

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        public bool IsManager { get; set; } = false;

        private async Task LoadDropdownChucVuAsync()
        {
            // TECH LEAD: Lọc bỏ các chức vụ quản lý cấp cao và khách hàng
            string[] vaiTroLoaiBo = { "Khách hàng", "Giám đốc", "Quản lý", "Quản trị viên hệ thống", "Quản trị viên" };

            var chucVus = await _context.CHUC_VU
                .Where(cv => !vaiTroLoaiBo.Contains(cv.TenChucVu))
                .ToListAsync();

            DanhSachChucVuSelectList = new SelectList(chucVus, "MaChucVu", "TenChucVu", MaChucVuSelected);
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            // 🛡️ BẢO MẬT: Chặn truy cập nếu không phải Quản lý
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            string employeeRole = roleClaim != null ? roleClaim.Value : "Nhân viên";
            IsManager = employeeRole == "Quản lý" || employeeRole == "Trưởng bộ phận" || User.IsInRole("Quản lý");

            if (!IsManager)
            {
                TempData["ThongBaoLoi"] = "Truy cập bị từ chối! Chỉ Quản lý mới có quyền chỉnh sửa nhân sự.";
                return RedirectToPage("/QuanLyLichCaNhan/QuanLyLichCaNhan");
            }

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // Dùng LINQ truy vấn thông tin nhân viên theo mã
                var nhanVien = await _context.NHAN_VIEN.FirstOrDefaultAsync(n => n.MaNV == id);
                if (nhanVien == null)
                {
                    return NotFound();
                }

                Input = nhanVien;

                // Nạp danh sách Dropdown (đã qua bộ lọc loại trừ Giám đốc, Quản lý)
                await LoadDropdownChucVuAsync();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải thông tin nhân viên: " + ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 🛡️ BẢO MẬT: Chặn POST nếu không đủ quyền
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            string employeeRole = roleClaim != null ? roleClaim.Value : "Nhân viên";
            if (employeeRole != "Quản lý" && employeeRole != "Trưởng bộ phận" && !User.IsInRole("Quản lý"))
            {
                TempData["ThongBaoLoi"] = "Truy cập bị từ chối! Không đủ quyền thao tác.";
                return RedirectToPage("/QuanLyLichCaNhan/QuanLyLichCaNhan");
            }

            try
            {
                // Dùng LINQ tìm bản ghi cũ trong cơ sở dữ liệu
                var nhanVienDb = await _context.NHAN_VIEN.FirstOrDefaultAsync(n => n.MaNV == Input.MaNV);
                if (nhanVienDb == null)
                {
                    return NotFound();
                }

                // Gán lại các giá trị mới từ form
                nhanVienDb.HoTen = Input.HoTen;
                nhanVienDb.SoCCCD = Input.SoCCCD;
                nhanVienDb.GioiTinh = Input.GioiTinh;
                nhanVienDb.NgaySinh = Input.NgaySinh;
                nhanVienDb.SDT = Input.SDT;
                nhanVienDb.Email = Input.Email;
                nhanVienDb.NgayVaoLam = Input.NgayVaoLam;

                // Đánh dấu Entity bị chỉnh sửa và lưu vào CSDL
                _context.Attach(nhanVienDb).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Cập nhật thành công thông tin nhân viên: {nhanVienDb.HoTen}";
                return RedirectToPage("./QuanLyDanhSachNhanVien");
            }
            catch (Exception ex)
            {
                // Bắt lỗi an toàn và hiển thị thông báo
                ThongBaoLoi = "Lỗi khi lưu cập nhật thông tin: " + ex.Message;

                // Load lại Dropdown chức vụ khi trả về form
                await LoadDropdownChucVuAsync();
                return Page();
            }
        }
    }
}