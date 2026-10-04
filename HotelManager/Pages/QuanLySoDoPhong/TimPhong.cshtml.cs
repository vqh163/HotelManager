using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// Khai báo namespace chứa Models (KHACH_HANG, PHONG...) và Data[cite: 8]
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

        // Khai báo Input form tìm kiếm với BindProperty
        [BindProperty]
        public InputKiemTra Input { get; set; } = new InputKiemTra();

        public class InputKiemTra
        {
            public DateTime NgayNhan { get; set; } = DateTime.Now;
            public DateTime NgayTra { get; set; } = DateTime.Now.AddDays(1);
            public int SoKhach { get; set; } = 1;
            public string? LoaiPhongTimKiem { get; set; } = "Tất cả";
        }

        // Danh sách phòng trả về giao diện
        public List<PHONG> DanhSachPhong { get; set; } = new List<PHONG>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Lấy mặc định một số phòng hiển thị khi vừa vào trang
                DanhSachPhong = await _context.PHONG.AsNoTracking().Take(9).ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                // Bắt lỗi kết nối DB
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống khi lấy danh sách phòng: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Xây dựng câu truy vấn bằng LINQ thay vì SQL thuần
                var query = _context.PHONG.AsNoTracking().AsQueryable();

                // Lọc theo loại phòng nếu không chọn "Tất cả"
                if (!string.IsNullOrEmpty(Input.LoaiPhongTimKiem) && Input.LoaiPhongTimKiem != "Tất cả")
                {
                    query = query.Where(p => p.LoaiPhong == Input.LoaiPhongTimKiem);
                }

                // Lọc theo sức chứa tối đa
                query = query.Where(p => p.SucChuaToiDa >= Input.SoKhach);

                // Thực thi truy vấn
                DanhSachPhong = await query.ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tìm kiếm: {ex.Message}");
                return Page();
            }
        }
    }
}