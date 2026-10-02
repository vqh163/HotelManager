using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// Khai báo đúng namespace chứa DbContext và các Model thực thể của dự án
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class TimPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TimPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Khởi tạo đối tượng Input để nhận dữ liệu từ Form tìm kiếm (Sử dụng [BindProperty])
        [BindProperty]
        public InputKiemTra Input { get; set; } = new InputKiemTra();

        public class InputKiemTra
        {
            public DateTime NgayNhan { get; set; } = DateTime.Now;
            public DateTime NgayTra { get; set; } = DateTime.Now.AddDays(2);
            public int SoKhach { get; set; } = 2;
            public string? LoaiPhongTimKiem { get; set; } = "Tất cả";
        }

        // Danh sách phòng truyền ra giao diện HTML
        public List<PHONG> DanhSachPhong { get; set; } = new List<PHONG>();

        // Phương thức xử lý khi tải trang lần đầu (GET)
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Sử dụng LINQ truy xuất danh sách phòng từ bảng PHONG
                DanhSachPhong = await _context.PHONG
                                              .AsNoTracking()
                                              .Take(6)
                                              .ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                // Bọc try...catch theo chuẩn dự án để bắt lỗi kết nối CSDL an toàn
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống khi tải danh sách phòng: {ex.Message}");
                return Page();
            }
        }

        // Phương thức xử lý khi người dùng bấm nút Tìm kiếm (POST)
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var query = _context.PHONG.AsNoTracking().AsQueryable();

                // Lọc theo loại phòng nếu có chọn
                if (!string.IsNullOrEmpty(Input.LoaiPhongTimKiem) && Input.LoaiPhongTimKiem != "Tất cả")
                {
                    query = query.Where(p => p.LoaiPhong == Input.LoaiPhongTimKiem);
                }

                // Lọc theo sức chứa tối đa của phòng
                query = query.Where(p => p.SucChuaToiDa >= Input.SoKhach);

                DanhSachPhong = await query.ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tìm kiếm phòng: {ex.Message}");
                return Page();
            }
        }
    }
}