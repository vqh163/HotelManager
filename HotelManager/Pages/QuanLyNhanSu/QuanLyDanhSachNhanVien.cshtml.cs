using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyNhanSu
{
    [Authorize(Policy = HangSoHeThong.PolicyNhanSu)]
    public class QuanLyDanhSachNhanVienModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuanLyDanhSachNhanVienModel> _logger;

        public QuanLyDanhSachNhanVienModel(ApplicationDbContext context, ILogger<QuanLyDanhSachNhanVienModel> logger)
        {
            _context = context;
            _logger = logger;
        }

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

        public IList<NhanVienViewModel> DanhSachNhanVien { get; set; } = new List<NhanVienViewModel>();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            // Truy vấn thuần LINQ kết hợp bảng NHAN_VIEN và CHUC_VU (loại bỏ đọc nhanvien_meta.txt)
            var chucVuDict = await _context.CHUC_VU.AsNoTracking()
                .ToDictionaryAsync(c => c.MaChucVu, c => c.TenChucVu ?? string.Empty);

            var nhanViens = await _context.NHAN_VIEN.AsNoTracking()
                .OrderByDescending(n => n.MaNV)
                .ToListAsync();

            DanhSachNhanVien = nhanViens.Select(nv =>
            {
                string chucVuHienThi = "Chưa phân chức vụ";
                if (!string.IsNullOrWhiteSpace(nv.CHUC_VUMaChucVu))
                {
                    chucVuHienThi = chucVuDict.TryGetValue(nv.CHUC_VUMaChucVu, out var ten) && !string.IsNullOrWhiteSpace(ten)
                        ? ten
                        : nv.CHUC_VUMaChucVu;
                }

                return new NhanVienViewModel
                {
                    MaNV = nv.MaNV,
                    HoTen = nv.HoTen ?? string.Empty,
                    GioiTinh = nv.GioiTinh ?? "Khác",
                    SoCCCD = nv.SoCCCD ?? string.Empty,
                    SDT = nv.SDT ?? string.Empty,
                    NgayVaoLam = nv.NgayVaoLam,
                    TenChucVu = chucVuHienThi
                };
            }).ToList();
        }

        // Xóa nhân viên đồng thời vô hiệu hóa tài khoản đăng nhập liên kết trong cùng Transaction
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nhanVien = await _context.NHAN_VIEN.FirstOrDefaultAsync(n => n.MaNV == id);
                if (nhanVien != null)
                {
                    // Vô hiệu hóa ngay tài khoản gắn với nhân viên nghỉ việc/bị xóa
                    var taiKhoans = await _context.TAI_KHOAN.Where(t => t.NHAN_VIENMaNV == id).ToListAsync();
                    foreach (var tk in taiKhoans)
                    {
                        tk.TrangThai = "Ngừng hoạt động";
                        _context.TAI_KHOAN.Update(tk);
                    }

                    _context.NHAN_VIEN.Remove(nhanVien);
                    _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                    {
                        MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                        TenDangNhap = User.Identity?.Name ?? "Quản lý",
                        HanhDong = "Xóa nhân viên",
                        ChiTiet = $"Đã xóa nhân viên {nhanVien.HoTen} ({nhanVien.MaNV}) và ngừng hoạt động {taiKhoans.Count} tài khoản liên kết.",
                        ThoiGian = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    ThongBao = $"Đã xóa nhân viên '{nhanVien.HoTen}' và thu hồi quyền truy cập tài khoản liên kết!";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xóa nhân viên {Id}", id);
                ThongBaoLoi = "Không thể xóa nhân viên do lỗi hệ thống hoặc dữ liệu liên kết.";
            }
            return RedirectToPage();
        }
    }
}
