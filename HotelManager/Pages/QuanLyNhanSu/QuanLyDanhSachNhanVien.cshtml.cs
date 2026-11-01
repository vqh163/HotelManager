using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace HotelManager.Pages.QuanLyNhanSu
{
    public class QuanLyDanhSachNhanVienModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyDanhSachNhanVienModel(ApplicationDbContext context) => _context = context;

        public class NhanVienViewModel
        {
            public string MaNV { get; set; } = string.Empty;
            public string HoTen { get; set; } = string.Empty;
            public string GioiTinh { get; set; } = string.Empty;
            public string SoCCCD { get; set; } = string.Empty;
            public string SDT { get; set; } = string.Empty;
            public DateTime? NgayVaoLam { get; set; }
            public string TenChucVu { get; set; } = string.Empty;
        }

        public IList<NhanVienViewModel> DanhSachNhanVien { get; set; } = default!;

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var nhanViens = await _context.NHAN_VIEN.ToListAsync();
            DanhSachNhanVien = new List<NhanVienViewModel>();

            var cacheDulieu = new Dictionary<string, string>();
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "nhanvien_meta.txt");
            if (System.IO.File.Exists(filePath))
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 2)
                    {
                        cacheDulieu[parts[0]] = parts[1];
                    }
                }
            }

            foreach (var nv in nhanViens)
            {
                string chucVuHienThi = "Nhân viên (Mặc định)";
                if (cacheDulieu.ContainsKey(nv.MaNV))
                {
                    chucVuHienThi = cacheDulieu[nv.MaNV];
                }

                DanhSachNhanVien.Add(new NhanVienViewModel
                {
                    MaNV = nv.MaNV,
                    HoTen = nv.HoTen,
                    GioiTinh = nv.GioiTinh ?? "Khác",
                    SoCCCD = nv.SoCCCD,
                    SDT = nv.SDT,
                    NgayVaoLam = nv.NgayVaoLam,
                    TenChucVu = chucVuHienThi
                });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            try
            {
                var nhanVien = await _context.NHAN_VIEN.FirstOrDefaultAsync(n => n.MaNV == id);
                if (nhanVien != null)
                {
                    _context.NHAN_VIEN.Remove(nhanVien);
                    await _context.SaveChangesAsync();
                    ThongBao = $"Đã xóa nhân viên: {nhanVien.HoTen}";
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Không thể xóa do vướng dữ liệu liên kết: " + ex.Message;
            }
            return RedirectToPage();
        }
    }
}