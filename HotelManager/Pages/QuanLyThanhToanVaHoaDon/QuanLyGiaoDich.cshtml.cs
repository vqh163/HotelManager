using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyThanhToan
{
    public class QuanLyGiaoDichModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyGiaoDichModel(ApplicationDbContext context) => _context = context;

        public IList<THANH_TOAN> DanhSachGiaoDich { get; set; } = new List<THANH_TOAN>();

        // Thống kê tổng dòng tiền & phân bổ theo từng phương thức
        public decimal TongThuVao { get; set; }
        public decimal TongHoanTra { get; set; }
        public decimal TongTienMat { get; set; }
        public decimal TongVietQR { get; set; }
        public decimal TongThe { get; set; }
        public decimal TongVi { get; set; }

        public double TyLeTienMat { get; set; }
        public double TyLeVietQR { get; set; }
        public double TyLeThe { get; set; }
        public double TyLeVi { get; set; }

        [BindProperty(SupportsGet = true)] public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty(SupportsGet = true)] public DateTime DenNgay { get; set; } = DateTime.Today;
        [BindProperty(SupportsGet = true)] public string PhuongThucFilter { get; set; } = "ALL";
        [BindProperty(SupportsGet = true)] public string? TuKhoa { get; set; }

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        /// <summary>
        /// Tải sổ cái giao dịch bằng LINQ và tính toán tỷ trọng dòng tiền theo phương thức thanh toán
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                DanhSachGiaoDich = await TruyVanGiaoDichAsync();
                TinhToanPhanBoDongTien(DanhSachGiaoDich);
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải sổ cái giao dịch: " + ex.Message;
            }
        }

        /// <summary>
        /// Xuất file CSV sổ đối soát giao dịch cho Kế toán (UTF-8 BOM hỗ trợ mở trực tiếp bằng Excel)
        /// </summary>
        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            try
            {
                var duLieu = await TruyVanGiaoDichAsync();
                var sb = new StringBuilder();
                sb.AppendLine("MaThanhToan,ThoiGianGiaoDich,MaHoaDon,PhuongThuc,MaGiaoDichNgoai,SoTien,TrangThai");

                foreach (var gd in duLieu)
                {
                    sb.AppendLine($"\"{gd.MaThanhToan}\",\"{gd.ThoiGianGiaoDich:dd/MM/yyyy HH:mm:ss}\",\"{gd.HOA_DONMaHoaDon ?? gd.HOA_DON_NHA_HANGMaHoaDonNhaHang ?? "ViCoc"}\",\"{gd.PhuongThucThanhToan}\",\"{gd.MaGiaoDichNgoai}\",{gd.SoTien ?? 0},\"{gd.TrangThaiGiaoDich}\"");
                }

                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                return File(bytes, "text/csv", $"DoiSoatGiaoDich_{TuNgay:yyyyMMdd}_{DenNgay:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi xuất file CSV đối soát: " + ex.Message;
                return RedirectToPage(new { TuNgay, DenNgay, PhuongThucFilter, TuKhoa });
            }
        }

        /// <summary>
        /// Hoàn tiền giao dịch trong 1 Transaction nguyên tử: tạo bút toán đảo âm và mở lại hóa đơn liên quan
        /// </summary>
        public async Task<IActionResult> OnPostHoanTienGiaoDichAsync(string maThanhToan)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var gdDb = await _context.THANH_TOAN.FirstOrDefaultAsync(t => t.MaThanhToan == maThanhToan);
                if (gdDb == null || gdDb.TrangThaiGiaoDich == "Đã hoàn tiền")
                {
                    ThongBaoLoi = "Giao dịch không tồn tại hoặc đã được hoàn tiền.";
                    return RedirectToPage(new { TuNgay, DenNgay, PhuongThucFilter, TuKhoa });
                }

                gdDb.TrangThaiGiaoDich = "Đã hoàn tiền";

                var butToanHoan = new THANH_TOAN
                {
                    MaThanhToan = "HT" + DateTime.Now.ToString("yyMMddHHmmssff"),
                    HOA_DONMaHoaDon = gdDb.HOA_DONMaHoaDon,
                    SoTien = -(gdDb.SoTien ?? 0),
                    PhuongThucThanhToan = gdDb.PhuongThucThanhToan,
                    MaGiaoDichNgoai = "REV-" + gdDb.MaThanhToan,
                    ThoiGianGiaoDich = DateTime.Now,
                    TrangThaiGiaoDich = "Đã hoàn tiền"
                };
                _context.THANH_TOAN.Add(butToanHoan);

                if (!string.IsNullOrEmpty(gdDb.HOA_DONMaHoaDon))
                {
                    var hdDb = await _context.HOA_DON.FirstOrDefaultAsync(h => h.MaHoaDon == gdDb.HOA_DONMaHoaDon);
                    if (hdDb != null) hdDb.TrangThaiThanhToan = "Chưa thanh toán";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Đã hoàn tiền giao dịch [{maThanhToan}] và ghi nhận bút toán đảo [{butToanHoan.MaThanhToan}].";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ThongBaoLoi = "Lỗi khi hoàn tiền giao dịch: " + ex.Message;
            }

            return RedirectToPage(new { TuNgay, DenNgay, PhuongThucFilter, TuKhoa });
        }

        private async Task<List<THANH_TOAN>> TruyVanGiaoDichAsync()
        {
            if (TuNgay > DenNgay) (TuNgay, DenNgay) = (DenNgay, TuNgay);
            DateTime start = TuNgay.Date;
            DateTime end = DenNgay.Date.AddDays(1).AddTicks(-1);

            var query = _context.THANH_TOAN
                .AsNoTracking()
                .Where(t => t.ThoiGianGiaoDich >= start && t.ThoiGianGiaoDich <= end);

            if (!string.IsNullOrEmpty(PhuongThucFilter) && PhuongThucFilter != "ALL")
                query = query.Where(t => t.PhuongThucThanhToan == PhuongThucFilter);

            var list = await query.OrderByDescending(t => t.ThoiGianGiaoDich).ToListAsync();

            if (!string.IsNullOrWhiteSpace(TuKhoa))
            {
                string kw = TuKhoa.Trim();
                list = list.Where(t =>
                    t.MaThanhToan.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    (t.HOA_DONMaHoaDon != null && t.HOA_DONMaHoaDon.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (t.MaGiaoDichNgoai != null && t.MaGiaoDichNgoai.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            return list;
        }

        private void TinhToanPhanBoDongTien(IList<THANH_TOAN> list)
        {
            var thuVao = list.Where(t => (t.SoTien ?? 0) > 0 && t.TrangThaiGiaoDich != "Đã hoàn tiền").ToList();
            TongThuVao = thuVao.Sum(t => t.SoTien ?? 0);
            TongHoanTra = list.Where(t => (t.SoTien ?? 0) < 0 || t.TrangThaiGiaoDich == "Đã hoàn tiền").Sum(t => Math.Abs(t.SoTien ?? 0));

            TongTienMat = thuVao.Where(t => t.PhuongThucThanhToan == "Tiền mặt").Sum(t => t.SoTien ?? 0);
            TongVietQR = thuVao.Where(t => t.PhuongThucThanhToan != null && t.PhuongThucThanhToan.Contains("VietQR")).Sum(t => t.SoTien ?? 0);
            TongThe = thuVao.Where(t => t.PhuongThucThanhToan != null && t.PhuongThucThanhToan.Contains("Thẻ")).Sum(t => t.SoTien ?? 0);
            TongVi = thuVao.Where(t => t.PhuongThucThanhToan != null && t.PhuongThucThanhToan.Contains("Ví")).Sum(t => t.SoTien ?? 0);

            if (TongThuVao > 0)
            {
                TyLeTienMat = (double)(TongTienMat * 100 / TongThuVao);
                TyLeVietQR = (double)(TongVietQR * 100 / TongThuVao);
                TyLeThe = (double)(TongThe * 100 / TongThuVao);
                TyLeVi = (double)(TongVi * 100 / TongThuVao);
            }
        }
    }
}