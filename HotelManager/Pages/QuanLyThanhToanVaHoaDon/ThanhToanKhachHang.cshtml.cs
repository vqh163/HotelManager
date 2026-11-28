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
    public class ThanhToanKhachHangModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ThanhToanKhachHangModel(ApplicationDbContext context) => _context = context;

        // Hồ sơ khách hàng đang đăng nhập (từ bảng KHACH_HANG)
        public KHACH_HANG? KhachHangHienTai { get; set; }

        // Danh sách hóa đơn thuộc về khách hàng
        public IList<HOA_DON> DanhSachHoaDonKhach { get; set; } = new List<HOA_DON>();

        // Tổng công nợ cần trả và số dư tiền cọc đang giữ
        public decimal TongTienCanThanhToan { get; set; }
        public decimal TongTienCocHienCo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? MaTraCuu { get; set; }

        [BindProperty]
        public ThanhToanOnlineInput InputPay { get; set; } = new ThanhToanOnlineInput();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public class ThanhToanOnlineInput
        {
            [Required(ErrorMessage = "Thiếu mã hóa đơn cần thanh toán.")]
            public string MaHoaDon { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
            public string PhuongThucThanhToan { get; set; } = "Chuyển khoản (VietQR)";
        }

        /// <summary>
        /// Tải danh sách hóa đơn và tiền cọc của khách hàng đang đăng nhập bằng LINQ
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                string? maKH = await LayMaKhachHangDangNhapAsync();
                var queryHD = _context.HOA_DON.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(maKH))
                {
                    KhachHangHienTai = await _context.KHACH_HANG
                        .AsNoTracking()
                        .FirstOrDefaultAsync(k => k.MaKH == maKH);

                    // Chỉ hiển thị hóa đơn của chính khách hàng đang đăng nhập
                    queryHD = queryHD.Where(h => h.KHACH_HANGMaKH == maKH);

                    // Tính tổng tiền cọc đang giữ trong bảng DAT_PHONG
                    var datPhongs = await _context.DAT_PHONG
                        .AsNoTracking()
                        .Where(d => d.KHACH_HANGMaKH == maKH)
                        .ToListAsync();

                    TongTienCocHienCo = datPhongs.Sum(d => Convert.ToDecimal(d.TienCoc ?? 0m));
                }
                else if (!string.IsNullOrWhiteSpace(MaTraCuu))
                {
                    // Chỉ cho phép tra cứu theo đúng mã hóa đơn hoặc mã đặt phòng cụ thể
                    string kw = MaTraCuu.Trim();
                    queryHD = queryHD.Where(h => h.MaHoaDon == kw || h.DAT_PHONGMaDatPhong == kw);
                }
                else
                {
                    // Chặn hiển thị toàn bộ hóa đơn hệ thống nếu không xác định được mã khách hàng
                    queryHD = queryHD.Where(h => false);
                }

                // Nếu khách đã đăng nhập nhưng vẫn gõ lọc theo mã trên thanh tra cứu
                if (!string.IsNullOrEmpty(maKH) && !string.IsNullOrWhiteSpace(MaTraCuu))
                {
                    string kw = MaTraCuu.Trim();
                    queryHD = queryHD.Where(h =>
                        h.MaHoaDon.Contains(kw) ||
                        (h.DAT_PHONGMaDatPhong != null && h.DAT_PHONGMaDatPhong.Contains(kw)));
                }

                DanhSachHoaDonKhach = await queryHD
                    .OrderBy(h => h.TrangThaiThanhToan == "Đã thanh toán" ? 1 : 0)
                    .ThenByDescending(h => h.NgayLap)
                    .ToListAsync();

                TongTienCanThanhToan = DanhSachHoaDonKhach
                    .Where(h => h.TrangThaiThanhToan != "Đã thanh toán")
                    .Sum(h => h.TongTienThanhToan ?? 0);
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải danh sách hóa đơn của quý khách: " + ex.Message;
            }
        }

        /// <summary>
        /// Xác nhận thanh toán trực tuyến trong 1 Transaction nguyên tử, chống thanh toán kép & tuân thủ PCI-DSS
        /// </summary>
        public async Task<IActionResult> OnPostThanhToanTrucTuyenAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hdDb = await _context.HOA_DON.FirstOrDefaultAsync(h => h.MaHoaDon == InputPay.MaHoaDon);
                if (hdDb == null)
                {
                    ThongBaoLoi = "Không tìm thấy hóa đơn cần thanh toán.";
                    return RedirectToPage(new { MaTraCuu });
                }

                // Chặn ở tầng Backend: khách hàng không được thanh toán hộ/can thiệp hóa đơn của người khác
                string? maKH = await LayMaKhachHangDangNhapAsync();
                if (User.IsInRole("Khách hàng") && (string.IsNullOrEmpty(maKH) || hdDb.KHACH_HANGMaKH != maKH))
                {
                    ThongBaoLoi = "Truy cập bị từ chối! Hóa đơn này không thuộc tài khoản của bạn.";
                    return RedirectToPage(new { MaTraCuu });
                }

                // Chống thanh toán kép (Idempotency)
                bool daCoGiaoDichThanhCong = await _context.THANH_TOAN
                    .AnyAsync(t => t.HOA_DONMaHoaDon == hdDb.MaHoaDon && t.TrangThaiGiaoDich == "Thành công");

                if (hdDb.TrangThaiThanhToan == "Đã thanh toán" || daCoGiaoDichThanhCong)
                {
                    ThongBaoLoi = $"Hóa đơn [{hdDb.MaHoaDon}] đã được thanh toán hoàn tất trước đó.";
                    return RedirectToPage(new { MaTraCuu });
                }

                decimal soTien = hdDb.TongTienThanhToan ?? 0;
                string maGiaoDichNgoai = "ONL-" + DateTime.Now.ToString("yyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                // 1. Cập nhật trạng thái hóa đơn
                hdDb.TrangThaiThanhToan = "Đã thanh toán";

                // 2. Lưu log giao dịch vào bảng THANH_TOAN (chỉ lưu mã giao dịch cổng ngoài theo PCI-DSS)
                _context.THANH_TOAN.Add(new THANH_TOAN
                {
                    MaThanhToan = "TT" + DateTime.Now.ToString("yyMMddHHmmssff"),
                    HOA_DONMaHoaDon = hdDb.MaHoaDon,
                    SoTien = soTien,
                    PhuongThucThanhToan = InputPay.PhuongThucThanhToan,
                    MaGiaoDichNgoai = maGiaoDichNgoai,
                    ThoiGianGiaoDich = DateTime.Now,
                    TrangThaiGiaoDich = "Thành công"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Thanh toán thành công hóa đơn [{hdDb.MaHoaDon}] ({soTien:N0} ₫)! Mã xác nhận giao dịch: {maGiaoDichNgoai}.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ThongBaoLoi = "Giao dịch thanh toán chưa hoàn tất do lỗi hệ thống: " + ex.Message;
            }

            return RedirectToPage(new { MaTraCuu });
        }

        /// <summary>
        /// Lấy mã khách hàng (KHACH_HANGMaKH) từ Cookie Claims hoặc truy vấn bảng TAI_KHOAN
        /// </summary>
        private async Task<string?> LayMaKhachHangDangNhapAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return null;

            string? claimMaKH = User.Claims.FirstOrDefault(c => c.Type == "MaKH")?.Value;
            if (!string.IsNullOrEmpty(claimMaKH))
                return claimMaKH;

            string? tenDangNhap = User.Identity.Name;
            if (string.IsNullOrEmpty(tenDangNhap))
                return null;

            var taiKhoan = await _context.TAI_KHOAN
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

            return taiKhoan?.KHACH_HANGMaKH;
        }
    }
}