using HotelManager.Data;
using HotelManager.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.NhaBep
{
    public class ThucDonViewModel
    {
        public string MaDichVu { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public string LoaiDichVu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public bool ConMon { get; set; }
    }

    public class QuanLyThucDonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuanLyThucDonModel> _logger;

        public QuanLyThucDonModel(ApplicationDbContext context, ILogger<QuanLyThucDonModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)] public string? TuKhoaTimKiem { get; set; }
        [BindProperty(SupportsGet = true)] public string? DanhMucLoc { get; set; }
        [BindProperty(SupportsGet = true)] public string? TrangThaiLoc { get; set; }

        // Thuộc tính xác định người dùng hiện tại có phải cấp Quản lý / Giám đốc hay không
        // (Quản lý: hiện nút Thêm/Sửa/Xóa món; Đầu bếp: chỉ bật/tắt trạng thái Còn/Hết món)
        public bool LaQuanLy { get; set; } = false;

        public List<ThucDonViewModel> DanhSachThucDon { get; set; } = new List<ThucDonViewModel>();

        public int TongSoMon { get; set; }
        public int DangBan { get; set; }
        public int TamNgung { get; set; }

        private bool KiemTraLaQuanLy()
        {
            return User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LaQuanLy = KiemTraLaQuanLy();

            try
            {
                // Truy vấn toàn bộ danh mục món ăn trực tiếp từ bảng DICH_VU trong SQL Server
                var query = _context.DICH_VU.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                {
                    string kw = TuKhoaTimKiem.Trim();
                    query = query.Where(d => (d.TenDichVu != null && d.TenDichVu.Contains(kw)) || d.MaDichVu.Contains(kw));
                }

                if (!string.IsNullOrEmpty(DanhMucLoc) && DanhMucLoc != "Tất cả")
                    query = query.Where(d => d.LoaiDichVu == DanhMucLoc);

                if (!string.IsNullOrEmpty(TrangThaiLoc))
                {
                    bool locConMon = (TrangThaiLoc == "ConMon");
                    query = query.Where(d => d.ConMon == locConMon);
                }

                var dsDichVu = await query.OrderBy(d => d.TenDichVu).ToListAsync();

                string[] foodImages =
                {
                    "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&q=80",
                    "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=300&q=80"
                };

                DanhSachThucDon = dsDichVu.Select((d, index) => new ThucDonViewModel
                {
                    MaDichVu = d.MaDichVu,
                    TenDichVu = d.TenDichVu ?? string.Empty,
                    LoaiDichVu = string.IsNullOrEmpty(d.LoaiDichVu) ? "Món chính" : d.LoaiDichVu,
                    DonGia = d.DonGia ?? 0m,
                    HinhAnh = !string.IsNullOrEmpty(d.HinhAnh) ? d.HinhAnh : foodImages[index % foodImages.Length],
                    MoTa = d.MoTa ?? string.Empty,
                    ConMon = d.ConMon
                }).ToList();

                // Thống kê tổng số món bằng LINQ
                TongSoMon = await _context.DICH_VU.AsNoTracking().CountAsync();
                DangBan = await _context.DICH_VU.AsNoTracking().CountAsync(d => d.ConMon);
                TamNgung = TongSoMon - DangBan;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải danh sách thực đơn.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải danh sách thực đơn.");
                return Page();
            }
        }

        // Xóa món ăn (chỉ cấp Quản lý mới được phép, chặn xóa nếu món đã phát sinh lịch sử gọi món)
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (!KiemTraLaQuanLy())
            {
                ModelState.AddModelError(string.Empty, "Truy cập bị từ chối: Chỉ tài khoản cấp Quản lý mới được phép xóa món ăn!");
                return await OnGetAsync();
            }

            try
            {
                bool daPhatSinh = await _context.CHI_TIET_DICH_VU.AsNoTracking()
                    .AnyAsync(c => c.DICH_VUMaDichVu == id);
                if (daPhatSinh)
                {
                    ModelState.AddModelError(string.Empty, "Không thể xóa món đã có trong hóa đơn/lịch sử gọi món! Vui lòng chuyển sang trạng thái 'Tạm ngưng' thay vì xóa.");
                    return await OnGetAsync();
                }

                var mon = await _context.DICH_VU.FirstOrDefaultAsync(m => m.MaDichVu == id);
                if (mon != null)
                {
                    _context.DICH_VU.Remove(mon);
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa món {Id}", id);
                ModelState.AddModelError(string.Empty, "Không thể xóa món ăn này do lỗi hệ thống.");
                return await OnGetAsync();
            }
        }

        // Bật/tắt trạng thái Còn món - Hết món trực tiếp trong SQL Server
        public async Task<IActionResult> OnPostToggleStatusAsync(string id)
        {
            try
            {
                var mon = await _context.DICH_VU.FirstOrDefaultAsync(m => m.MaDichVu == id);
                if (mon == null)
                    return new JsonResult(new { success = false, message = "Không tìm thấy món." });

                mon.ConMon = !mon.ConMon;
                await _context.SaveChangesAsync();

                var tongSo = await _context.DICH_VU.AsNoTracking().CountAsync();
                var dangBan = await _context.DICH_VU.AsNoTracking().CountAsync(d => d.ConMon);
                var tamNgung = tongSo - dangBan;

                return new JsonResult(new { success = true, tongSoMon = tongSo, dangBan = dangBan, tamNgung = tamNgung });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi chuyển trạng thái món {Id}", id);
                return new JsonResult(new { success = false });
            }
        }
    }
}
