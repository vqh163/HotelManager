using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    [Authorize(Roles = "Quản lý,Lễ tân,Sales,Buồng phòng")]
    public class SoDoPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SoDoPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class RoomGridViewModel
        {
            public string MaPhong { get; set; } = default!;
            public string? SoPhong { get; set; }
            public string? LoaiPhong { get; set; }
            public string? MaTrangThai { get; set; }
            public string? TrangThaiPhong { get; set; }
            public string? TrangThaiVeSinh { get; set; }

            public string Tang => !string.IsNullOrEmpty(SoPhong) ? SoPhong.Substring(0, 1) : "1";
        }

        public List<RoomGridViewModel> DanhSachLieuPhong { get; set; } = new List<RoomGridViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var query = from p in _context.PHONG.AsNoTracking()
                            join t in _context.TRANG_THAI_PHONG.AsNoTracking()
                            on p.MaTrangThai equals t.MaTrangThai into pt
                            from t in pt.DefaultIfEmpty()
                            select new RoomGridViewModel
                            {
                                MaPhong = p.MaPhong,
                                SoPhong = p.SoPhong,
                                LoaiPhong = p.LoaiPhong,
                                MaTrangThai = p.MaTrangThai,
                                TrangThaiPhong = t != null ? t.TrangThaiPhong : "Trống",
                                TrangThaiVeSinh = t != null ? t.TrangThaiVeSinh : "Sạch"
                            };

                DanhSachLieuPhong = await query.OrderBy(x => x.SoPhong).ToListAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tải sơ đồ phòng: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCapNhatVeSinhAsync(string maPhong, string trangThaiVeSinh)
        {
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong != null)
                {
                    var trangThai = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                    if (trangThai != null)
                    {
                        trangThai.TrangThaiVeSinh = trangThaiVeSinh;
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi cập nhật vệ sinh: {ex.Message}");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCapNhatTrangThaiAsync(string maPhong, string maTrangThaiMoi)
        {
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong != null)
                {
                    phong.MaTrangThai = maTrangThaiMoi;
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi cập nhật trạng thái: {ex.Message}");
                return RedirectToPage();
            }
        }
    }
}