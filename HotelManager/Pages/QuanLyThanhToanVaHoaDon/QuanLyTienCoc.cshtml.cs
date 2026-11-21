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
    public class QuanLyTienCocModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyTienCocModel(ApplicationDbContext context) => _context = context;

        // Danh sách đơn đặt phòng và thông tin đơn đang chọn
        public IList<DAT_PHONG> DanhSachDatPhong { get; set; } = new List<DAT_PHONG>();
        public DAT_PHONG? DatPhongDangChon { get; set; }
        public IList<THANH_TOAN> LichSuCocDonChon { get; set; } = new List<THANH_TOAN>();

        // Thống kê tổng quan ví tiền cọc
        public decimal TongTienCocDangGiu { get; set; }
        public int SoDonDaCoc { get; set; }
        public int SoDonChuaCoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? MaDatPhongChon { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TrangThaiCocFilter { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        [BindProperty]
        public XuLyCocInput InputCoc { get; set; } = new XuLyCocInput();

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public class XuLyCocInput
        {
            [Required]
            public string MaDatPhong { get; set; } = string.Empty;

            [Required]
            public string HanhDong { get; set; } = "THU"; // THU hoặc HOAN

            [Range(1000, double.MaxValue, ErrorMessage = "Số tiền phải từ 1.000 ₫ trở lên.")]
            public decimal SoTien { get; set; }

            public string PhuongThuc { get; set; } = "Chuyển khoản (VietQR)";
        }

        /// <summary>
        /// Tải danh sách ví tiền cọc theo đơn đặt phòng bằng LINQ
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                var toanBoDP = await _context.DAT_PHONG
                    .AsNoTracking()
                    .OrderByDescending(d => d.NgayDat)
                    .ToListAsync();

                TongTienCocDangGiu = toanBoDP.Sum(d => Convert.ToDecimal(d.TienCoc ?? 0m));
                SoDonDaCoc = toanBoDP.Count(d => Convert.ToDecimal(d.TienCoc ?? 0m) > 0);
                SoDonChuaCoc = toanBoDP.Count(d => Convert.ToDecimal(d.TienCoc ?? 0m) <= 0);

                var query = toanBoDP.AsQueryable();

                if (TrangThaiCocFilter == "DA_COC")
                    query = query.Where(d => Convert.ToDecimal(d.TienCoc ?? 0m) > 0);
                else if (TrangThaiCocFilter == "CHUA_COC")
                    query = query.Where(d => Convert.ToDecimal(d.TienCoc ?? 0m) <= 0);

                if (!string.IsNullOrWhiteSpace(TuKhoa))
                {
                    string kw = TuKhoa.Trim();
                    query = query.Where(d =>
                        d.MaDatPhong.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        (d.KHACH_HANGMaKH != null && d.KHACH_HANGMaKH.Contains(kw, StringComparison.OrdinalIgnoreCase)));
                }

                DanhSachDatPhong = query.ToList();

                if (!string.IsNullOrEmpty(MaDatPhongChon))
                    DatPhongDangChon = DanhSachDatPhong.FirstOrDefault(d => d.MaDatPhong == MaDatPhongChon);

                if (DatPhongDangChon == null && DanhSachDatPhong.Any())
                    DatPhongDangChon = DanhSachDatPhong.First();

                // Nạp các giao dịch cọc gắn với đơn đặt phòng đang chọn
                if (DatPhongDangChon != null)
                {
                    string maDP = DatPhongDangChon.MaDatPhong;
                    LichSuCocDonChon = await _context.THANH_TOAN
                        .AsNoTracking()
                        .Where(t => t.MaGiaoDichNgoai != null && t.MaGiaoDichNgoai.Contains(maDP))
                        .OrderByDescending(t => t.ThoiGianGiaoDich)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải danh sách tiền cọc: " + ex.Message;
            }
        }

        /// <summary>
        /// Thu thêm hoặc hoàn tiền cọc trong Transaction nguyên tử, ghi nhận log đối soát vào THANH_TOAN
        /// </summary>
        public async Task<IActionResult> OnPostXuLyTienCocAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var dpDb = await _context.DAT_PHONG.FirstOrDefaultAsync(d => d.MaDatPhong == InputCoc.MaDatPhong);
                if (dpDb == null)
                {
                    ThongBaoLoi = "Không tìm thấy đơn đặt phòng tương ứng.";
                    return RedirectToPage();
                }

                decimal cocHienTai = Convert.ToDecimal(dpDb.TienCoc ?? 0m);

                if (InputCoc.HanhDong == "HOAN")
                {
                    if (InputCoc.SoTien > cocHienTai)
                    {
                        ThongBaoLoi = $"Số tiền hoàn ({InputCoc.SoTien:N0} ₫) vượt quá số dư cọc hiện tại ({cocHienTai:N0} ₫).";
                        return RedirectToPage(new { MaDatPhongChon = dpDb.MaDatPhong, TrangThaiCocFilter, TuKhoa });
                    }

                    dpDb.TienCoc = cocHienTai - InputCoc.SoTien;

                    // Lưu vết hoàn cọc vào bảng THANH_TOAN cho Kế toán đối soát
                    _context.THANH_TOAN.Add(new THANH_TOAN
                    {
                        MaThanhToan = "HC" + DateTime.Now.ToString("yyMMddHHmmssff"),
                        SoTien = -InputCoc.SoTien,
                        PhuongThucThanhToan = InputCoc.PhuongThuc,
                        MaGiaoDichNgoai = "REFUND-" + dpDb.MaDatPhong,
                        ThoiGianGiaoDich = DateTime.Now,
                        TrangThaiGiaoDich = "Đã hoàn tiền"
                    });

                    ThongBao = $"Đã hoàn cọc {InputCoc.SoTien:N0} ₫ cho đơn [{dpDb.MaDatPhong}].";
                }
                else
                {
                    dpDb.TienCoc = cocHienTai + InputCoc.SoTien;

                    // Lưu vết thu cọc vào bảng THANH_TOAN
                    _context.THANH_TOAN.Add(new THANH_TOAN
                    {
                        MaThanhToan = "TC" + DateTime.Now.ToString("yyMMddHHmmssff"),
                        SoTien = InputCoc.SoTien,
                        PhuongThucThanhToan = InputCoc.PhuongThuc,
                        MaGiaoDichNgoai = "DEPOSIT-" + dpDb.MaDatPhong,
                        ThoiGianGiaoDich = DateTime.Now,
                        TrangThaiGiaoDich = "Thu cọc thành công"
                    });

                    ThongBao = $"Đã thu thêm cọc {InputCoc.SoTien:N0} ₫ cho đơn [{dpDb.MaDatPhong}].";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ThongBaoLoi = "Lỗi khi cập nhật ví tiền cọc: " + ex.Message;
            }

            return RedirectToPage(new { MaDatPhongChon = InputCoc.MaDatPhong, TrangThaiCocFilter, TuKhoa });
        }
    }
}