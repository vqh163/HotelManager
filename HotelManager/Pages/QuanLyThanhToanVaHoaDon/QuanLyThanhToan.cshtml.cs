using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyThanhToan
{
    public class QuanLyThanhToanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyThanhToanModel(ApplicationDbContext context) => _context = context;

        public IList<HOA_DON> HoaDonChuaThanhToan { get; set; } = new List<HOA_DON>();
        public HOA_DON? HoaDonDangChon { get; set; }

        [BindProperty(SupportsGet = true)] public string? MaHoaDonChon { get; set; }
        [BindProperty] public ThanhToanInputModel Input { get; set; } = new ThanhToanInputModel();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public class ThanhToanInputModel
        {
            [Required] public string MaHoaDon { get; set; } = string.Empty;
            [Range(0, double.MaxValue)] public decimal SoTien { get; set; }
            [Required] public string PhuongThucThanhToan { get; set; } = "Tiền mặt";
            public string? MaGiaoDichNgoai { get; set; }
        }

        /// <summary>
        /// Tải danh sách hóa đơn chờ thu và chọn hóa đơn hiển thị trên quầy thu ngân bằng LINQ
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                HoaDonChuaThanhToan = await _context.HOA_DON
                    .AsNoTracking()
                    .Where(h => h.TrangThaiThanhToan != "Đã thanh toán")
                    .OrderByDescending(h => h.NgayLap)
                    .ToListAsync();

                if (!string.IsNullOrEmpty(MaHoaDonChon))
                    HoaDonDangChon = HoaDonChuaThanhToan.FirstOrDefault(h => h.MaHoaDon == MaHoaDonChon);

                if (HoaDonDangChon == null && HoaDonChuaThanhToan.Any())
                    HoaDonDangChon = HoaDonChuaThanhToan.First();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải dữ liệu thanh toán: " + ex.Message;
            }
        }

        /// <summary>
        /// Xác nhận thanh toán nguyên tử (Atomic Transaction), chống thanh toán kép & lưu mã PCI-DSS
        /// </summary>
        public async Task<IActionResult> OnPostXacNhanThanhToanAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hdDb = await _context.HOA_DON.FirstOrDefaultAsync(h => h.MaHoaDon == Input.MaHoaDon);
                if (hdDb == null)
                {
                    ThongBaoLoi = "Không tìm thấy hóa đơn cần thanh toán.";
                    return RedirectToPage();
                }

                // Chống thanh toán kép
                bool daThanhToan = await _context.THANH_TOAN
                    .AnyAsync(t => t.HOA_DONMaHoaDon == hdDb.MaHoaDon && t.TrangThaiGiaoDich == "Thành công");

                if (hdDb.TrangThaiThanhToan == "Đã thanh toán" || daThanhToan)
                {
                    ThongBaoLoi = $"Hóa đơn [{hdDb.MaHoaDon}] đã được thanh toán trước đó (đã chặn thanh toán kép).";
                    return RedirectToPage();
                }

                string maRef = string.IsNullOrWhiteSpace(Input.MaGiaoDichNgoai)
                    ? "TXN-" + DateTime.Now.ToString("yyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()
                    : Input.MaGiaoDichNgoai.Trim();

                if (!string.IsNullOrWhiteSpace(Input.MaGiaoDichNgoai))
                {
                    bool trungMa = await _context.THANH_TOAN.AnyAsync(t => t.MaGiaoDichNgoai == maRef);
                    if (trungMa)
                    {
                        ThongBaoLoi = $"Mã giao dịch [{maRef}] đã tồn tại trong hệ thống!";
                        return RedirectToPage(new { MaHoaDonChon = Input.MaHoaDon });
                    }
                }

                decimal soTienThucThu = hdDb.TongTienThanhToan ?? Input.SoTien;
                hdDb.TrangThaiThanhToan = "Đã thanh toán";

                _context.THANH_TOAN.Add(new THANH_TOAN
                {
                    MaThanhToan = "TT" + DateTime.Now.ToString("yyMMddHHmmssff"),
                    HOA_DONMaHoaDon = hdDb.MaHoaDon,
                    SoTien = soTienThucThu,
                    PhuongThucThanhToan = Input.PhuongThucThanhToan,
                    MaGiaoDichNgoai = maRef,
                    ThoiGianGiaoDich = DateTime.Now,
                    TrangThaiGiaoDich = "Thành công"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Thanh toán thành công hóa đơn [{hdDb.MaHoaDon}] ({soTienThucThu:N0} ₫) qua {Input.PhuongThucThanhToan}.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ThongBaoLoi = "Lỗi khi xác nhận thanh toán: " + ex.Message;
                return RedirectToPage(new { MaHoaDonChon = Input.MaHoaDon });
            }
        }
    }
}