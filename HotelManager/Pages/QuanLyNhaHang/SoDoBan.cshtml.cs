using HotelManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    public class BanNhaHangViewModel
    {
        public string MaBan { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;
        public string SucChua { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? MaHoaDonNhaHang { get; set; }
        public string TenKhachDat { get; set; } = string.Empty;
    }

    public class SoDoBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SoDoBanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        public List<BanNhaHangViewModel> DanhSachBan { get; set; } = new List<BanNhaHangViewModel>();

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.BAN_NHA_HANG.AsQueryable();

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                    query = query.Where(b => b.TenBan.Contains(TuKhoaTimKiem) || b.MaBan.Contains(TuKhoaTimKiem));

                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả bàn")
                    query = query.Where(b => b.TrangThai == TrangThaiLoc);

                var dsBanDb = await query.OrderBy(b => b.TenBan).ToListAsync();

                // Chỉ lấy các hóa đơn thật chưa thanh toán
                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG
                    .Where(h => h.HinhThucThanhToan != "Đã Thanh Toán")
                    .ToListAsync();

                DanhSachBan = dsBanDb.Select(b =>
                {
                    // Map mã hóa đơn thật vào đúng bàn
                    string maBanCuaHoaDon = "";
                    var hoaDonCuaBan = dsHoaDonActive.OrderByDescending(x => x.ThoiGianTao).FirstOrDefault(x =>
                    {
                        var prop = x.GetType().GetProperty("MaBan") ?? x.GetType().GetProperty("BAN_NHA_HANGMaBan");
                        return prop != null && prop.GetValue(x)?.ToString() == b.MaBan;
                    });

                    string tenKhach = b.GetType().GetProperty("TenKhachDat")?.GetValue(b)?.ToString() ?? string.Empty;

                    return new BanNhaHangViewModel
                    {
                        MaBan = b.MaBan,
                        TenBan = b.TenBan ?? string.Empty,
                        SucChua = b.SucChua?.ToString() ?? "0",
                        TrangThai = b.TrangThai ?? "Trống",

                        // Lấy đúng mã thật sinh ra từ form Thêm Bàn
                        MaHoaDonNhaHang = (b.TrangThai == "Đang có khách" && hoaDonCuaBan != null) ? hoaDonCuaBan.MaHoaDonNhaHang : null,
                        TenKhachDat = (b.TrangThai == "Đã đặt trước") ? tenKhach : string.Empty
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải sơ đồ bàn: " + ex.Message);
            }
        }

        public async Task<IActionResult> OnPostXacNhanSachAsync(string maBan)
        {
            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBan);
                if (ban != null)
                {
                    ban.TrangThai = "Trống";
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật trạng thái bàn: " + ex.Message);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            try
            {
                var ban = await _context.BAN_NHA_HANG.FindAsync(id);
                if (ban != null)
                {
                    _context.BAN_NHA_HANG.Remove(ban);
                    await _context.SaveChangesAsync();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Không thể xóa bàn: " + ex.Message);
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostYeuCauThanhToanAsync(string maBan, string maHoaDon)
        {
            try
            {
                // Kiểm tra mã thật 100% trong CSDL
                var hoaDon = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDon);

                if (hoaDon != null)
                {
                    hoaDon.HinhThucThanhToan = "Chờ Thanh Toán";
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã gửi yêu cầu thanh toán (Mã HD: {maHoaDon}) tới Quầy thu ngân!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi: Không tìm thấy hóa đơn {maHoaDon} trong hệ thống. Vui lòng kiểm tra lại!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống khi gửi yêu cầu thanh toán: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}