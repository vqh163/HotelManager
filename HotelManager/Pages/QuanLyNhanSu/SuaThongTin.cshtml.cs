using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Bắt buộc thêm thư viện này để dùng phân quyền
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace HotelManager.Pages.QuanLyNhanSu
{
    // 🛡️ BẢO MẬT: Chặn truy cập toàn bộ trang này nếu User không có Role là "Quản lý"
    [Authorize(Roles = "Quản lý")]
    public class SuaThongTinModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SuaThongTinModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public NHAN_VIEN Input { get; set; } = default!;

        [BindProperty]
        public string MaChucVuSelected { get; set; } = string.Empty;

        public SelectList DanhSachChucVuSelectList { get; set; } = default!;

        public string ThongBaoLoi { get; set; } = string.Empty;

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        public bool IsManager { get; set; } = false;

        private async Task LoadDropdownChucVuAsync()
        {
            // MỞ KHÓA: Chỉ lọc bỏ vai trò "Khách hàng", giữ lại Quản lý, Giám đốc, Quản trị viên
            var chucVus = await _context.CHUC_VU
                .Where(cv => cv.TenChucVu != "Khách hàng")
                .ToListAsync();

            DanhSachChucVuSelectList = new SelectList(chucVus, "MaChucVu", "TenChucVu", MaChucVuSelected);
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // Đã đi qua được [Authorize] thì chắc chắn là Quản lý
            IsManager = User.IsInRole("Quản lý");

            try
            {
                // Truy vấn bằng LINQ
                var nhanVien = await _context.NHAN_VIEN.FirstOrDefaultAsync(n => n.MaNV == id);
                if (nhanVien == null)
                {
                    return NotFound();
                }

                Input = nhanVien;

                // Map dữ liệu chức danh của nhân viên (giả định dùng chung bảng hoặc logic riêng, ở đây ta load dropdown)
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
            try
            {
                var nhanVienDb = await _context.NHAN_VIEN.FirstOrDefaultAsync(n => n.MaNV == Input.MaNV);
                if (nhanVienDb == null)
                {
                    return NotFound();
                }

                // Cập nhật các trường thông tin
                nhanVienDb.HoTen = Input.HoTen;
                nhanVienDb.SoCCCD = Input.SoCCCD;
                nhanVienDb.GioiTinh = Input.GioiTinh;
                nhanVienDb.NgaySinh = Input.NgaySinh;
                nhanVienDb.SDT = Input.SDT;
                nhanVienDb.Email = Input.Email;
                nhanVienDb.NgayVaoLam = Input.NgayVaoLam;

                // (Nếu Model của em có lưu MaChucVu trong bảng NHAN_VIEN, em sẽ gán thêm: nhanVienDb.MaChucVu = MaChucVuSelected;)

                _context.Attach(nhanVienDb).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["ThongBao"] = $"Cập nhật thành công thông tin nhân viên: {nhanVienDb.HoTen}";
                return RedirectToPage("./QuanLyDanhSachNhanVien");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi lưu cập nhật thông tin: " + ex.Message;
                await LoadDropdownChucVuAsync();
                return Page();
            }
        }
    }
}