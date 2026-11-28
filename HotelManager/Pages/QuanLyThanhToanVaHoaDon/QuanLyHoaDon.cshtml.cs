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
    public class QuanLyHoaDonModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyHoaDonModel(ApplicationDbContext context) => _context = context;

        public IList<HOA_DON> DanhSachHoaDon { get; set; } = new List<HOA_DON>();
        public IList<HOA_DON> HoaDonChuaThanhToanDeGhep { get; set; } = new List<HOA_DON>();

        // Các chỉ số tổng hợp doanh thu hóa đơn
        public int TongSoHoaDon { get; set; }
        public int SoDaThanhToan { get; set; }
        public int SoChuaThanhToan { get; set; }
        public decimal TongDoanhThuDaThu { get; set; }
        public decimal TongCongNoChoThu { get; set; }
        public decimal TongThueVAT { get; set; }

        [BindProperty(SupportsGet = true)] public string TrangThaiFilter { get; set; } = "ALL";
        [BindProperty(SupportsGet = true)] public string? TuKhoa { get; set; }

        [BindProperty] public HOA_DON InputHoaDon { get; set; } = new HOA_DON();
        [BindProperty] public decimal TyLeVAT { get; set; } = 10;
        [BindProperty] public GhiNoInputModel InputGhiNo { get; set; } = new GhiNoInputModel();
        [BindProperty] public ChiaHoaDonInputModel InputChia { get; set; } = new ChiaHoaDonInputModel();

        // Nhận danh sách nhiều mã hóa đơn được tích chọn để ghép cùng lúc
        [BindProperty] public List<string> DanhSachMaHoaDonGhep { get; set; } = new List<string>();
        [BindProperty] public string? MaHoaDonGiuLai { get; set; }

        [TempData] public string ThongBao { get; set; } = string.Empty;
        [TempData] public string ThongBaoLoi { get; set; } = string.Empty;

        public class GhiNoInputModel
        {
            public string MaHoaDon { get; set; } = string.Empty;
            public string LoaiChiPhi { get; set; } = "DichVu";
            public decimal SoTienThem { get; set; }
        }

        public class ChiaHoaDonInputModel
        {
            [Required] public string MaHoaDonGoc { get; set; } = string.Empty;
            [Range(2, 10, ErrorMessage = "Số phần chia phải từ 2 đến 10 hóa đơn.")]
            public int SoPhanChia { get; set; } = 2;
        }

        /// <summary>
        /// Tải danh sách hóa đơn và thống kê doanh thu bóc tách bằng LINQ
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                var toanBoHD = await _context.HOA_DON
                    .AsNoTracking()
                    .OrderByDescending(h => h.NgayLap)
                    .ToListAsync();

                TongSoHoaDon = toanBoHD.Count;
                SoDaThanhToan = toanBoHD.Count(h => h.TrangThaiThanhToan == "Đã thanh toán");
                SoChuaThanhToan = toanBoHD.Count(h => h.TrangThaiThanhToan != "Đã thanh toán");
                TongDoanhThuDaThu = toanBoHD.Where(h => h.TrangThaiThanhToan == "Đã thanh toán").Sum(h => h.TongTienThanhToan ?? 0);
                TongCongNoChoThu = toanBoHD.Where(h => h.TrangThaiThanhToan != "Đã thanh toán").Sum(h => h.TongTienThanhToan ?? 0);
                TongThueVAT = toanBoHD.Sum(h => h.TienThueVAT ?? 0);

                // Danh sách hóa đơn chưa thanh toán dùng cho Modal Ghép nhiều hóa đơn
                HoaDonChuaThanhToanDeGhep = toanBoHD.Where(h => h.TrangThaiThanhToan != "Đã thanh toán").ToList();

                var query = toanBoHD.AsQueryable();
                if (!string.IsNullOrEmpty(TrangThaiFilter) && TrangThaiFilter != "ALL")
                {
                    query = TrangThaiFilter == "Đã thanh toán"
                        ? query.Where(h => h.TrangThaiThanhToan == "Đã thanh toán")
                        : query.Where(h => h.TrangThaiThanhToan != "Đã thanh toán");
                }

                if (!string.IsNullOrWhiteSpace(TuKhoa))
                {
                    string kw = TuKhoa.Trim();
                    query = query.Where(h =>
                        h.MaHoaDon.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        (h.DAT_PHONGMaDatPhong != null && h.DAT_PHONGMaDatPhong.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                        (h.KHACH_HANGMaKH != null && h.KHACH_HANGMaKH.Contains(kw, StringComparison.OrdinalIgnoreCase)));
                }

                DanhSachHoaDon = query.ToList();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải danh sách hóa đơn: " + ex.Message;
            }
        }

        /// <summary>
        /// Lập hóa đơn mới, tự động tính thuế VAT và khấu trừ tiền cọc
        /// </summary>
        public async Task<IActionResult> OnPostTaoHoaDonAsync()
        {
            try
            {
                decimal tienPhong = InputHoaDon.TienPhong ?? 0;
                decimal tienDV = InputHoaDon.TienDichVu ?? 0;
                decimal tienNH = InputHoaDon.TienNhaHang ?? 0;
                decimal tienGiam = InputHoaDon.TienGiamGia ?? 0;
                decimal tienCoc = InputHoaDon.TienCocDaTru ?? 0;

                decimal tamTinh = Math.Max(0, tienPhong + tienDV + tienNH - tienGiam);
                decimal tienVAT = Math.Round(tamTinh * (TyLeVAT / 100m), 0);
                decimal tongTT = Math.Max(0, tamTinh + tienVAT - tienCoc);

                var hdMoi = new HOA_DON
                {
                    MaHoaDon = "HD" + DateTime.Now.ToString("yyMMddHHmmss"),
                    DAT_PHONGMaDatPhong = string.IsNullOrWhiteSpace(InputHoaDon.DAT_PHONGMaDatPhong) ? null : InputHoaDon.DAT_PHONGMaDatPhong.Trim(),
                    KHACH_HANGMaKH = string.IsNullOrWhiteSpace(InputHoaDon.KHACH_HANGMaKH) ? null : InputHoaDon.KHACH_HANGMaKH.Trim(),
                    NgayLap = DateTime.Now,
                    TienPhong = tienPhong,
                    TienDichVu = tienDV,
                    TienNhaHang = tienNH,
                    TienGiamGia = tienGiam,
                    TienThueVAT = tienVAT,
                    TienCocDaTru = tienCoc,
                    TongTienThanhToan = tongTT,
                    TrangThaiThanhToan = tongTT == 0 ? "Đã thanh toán" : "Chưa thanh toán"
                };

                _context.HOA_DON.Add(hdMoi);
                await _context.SaveChangesAsync();
                ThongBao = $"Đã lập hóa đơn [{hdMoi.MaHoaDon}] thành công ({tongTT:N0} ₫).";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi lập hóa đơn: " + ex.Message;
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Ghi nợ chi phí Dịch vụ hoặc Nhà hàng vào hóa đơn phòng
        /// </summary>
        public async Task<IActionResult> OnPostGhiNoDichVuAsync()
        {
            try
            {
                var hdDb = await _context.HOA_DON.FirstOrDefaultAsync(h => h.MaHoaDon == InputGhiNo.MaHoaDon);
                if (hdDb == null || hdDb.TrangThaiThanhToan == "Đã thanh toán")
                {
                    ThongBaoLoi = "Hóa đơn không hợp lệ hoặc đã thanh toán.";
                    return RedirectToPage();
                }

                if (InputGhiNo.LoaiChiPhi == "NhaHang")
                    hdDb.TienNhaHang = (hdDb.TienNhaHang ?? 0) + InputGhiNo.SoTienThem;
                else
                    hdDb.TienDichVu = (hdDb.TienDichVu ?? 0) + InputGhiNo.SoTienThem;

                TinhLaiTongHoaDon(hdDb);
                await _context.SaveChangesAsync();
                ThongBao = $"Đã ghi nợ +{InputGhiNo.SoTienThem:N0} ₫ vào hóa đơn [{hdDb.MaHoaDon}].";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi ghi nợ dịch vụ: " + ex.Message;
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Tách chi phí Dịch vụ & Nhà hàng sang hóa đơn phụ (Split Folio) trong Transaction nguyên tử
        /// </summary>
        public async Task<IActionResult> OnPostTachHoaDonAsync(string maHoaDonGoc)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hdGoc = await _context.HOA_DON.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDonGoc);
                if (hdGoc == null || hdGoc.TrangThaiThanhToan == "Đã thanh toán")
                {
                    ThongBaoLoi = "Hóa đơn không hợp lệ để tách.";
                    return RedirectToPage();
                }

                decimal dv = hdGoc.TienDichVu ?? 0;
                decimal nh = hdGoc.TienNhaHang ?? 0;
                if (dv + nh <= 0)
                {
                    ThongBaoLoi = "Không có chi phí dịch vụ/nhà hàng để tách.";
                    return RedirectToPage();
                }

                decimal vatPhu = Math.Round((dv + nh) * 0.10m, 0);
                var hdPhu = new HOA_DON
                {
                    MaHoaDon = "HD" + DateTime.Now.ToString("yyMMddHHmmss") + "S",
                    KHACH_HANGMaKH = hdGoc.KHACH_HANGMaKH,
                    NHAN_VIENMaNV = hdGoc.NHAN_VIENMaNV,
                    DAT_PHONGMaDatPhong = hdGoc.DAT_PHONGMaDatPhong,
                    NgayLap = DateTime.Now,
                    TienPhong = 0,
                    TienDichVu = dv,
                    TienNhaHang = nh,
                    TienGiamGia = 0,
                    TienThueVAT = vatPhu,
                    TienCocDaTru = 0,
                    TongTienThanhToan = dv + nh + vatPhu,
                    TrangThaiThanhToan = "Chưa thanh toán"
                };

                hdGoc.TienDichVu = 0;
                hdGoc.TienNhaHang = 0;
                TinhLaiTongHoaDon(hdGoc);

                _context.HOA_DON.Add(hdPhu);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Tách hóa đơn thành công! Hóa đơn phụ mới: [{hdPhu.MaHoaDon}].";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ThongBaoLoi = "Lỗi khi tách hóa đơn: " + ex.Message;
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Chia đều 1 hóa đơn chưa thanh toán thành N phần (2–10 hóa đơn) trong Transaction nguyên tử
        /// </summary>
        public async Task<IActionResult> OnPostChiaHoaDonAsync()
        {
            if (InputChia.SoPhanChia < 2 || InputChia.SoPhanChia > 10)
            {
                ThongBaoLoi = "Số phần chia phải nằm trong khoảng từ 2 đến 10.";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hdGoc = await _context.HOA_DON.FirstOrDefaultAsync(h => h.MaHoaDon == InputChia.MaHoaDonGoc);
                if (hdGoc == null || hdGoc.TrangThaiThanhToan == "Đã thanh toán")
                {
                    ThongBaoLoi = "Chỉ được chia hóa đơn đang ở trạng thái Chưa thanh toán.";
                    return RedirectToPage();
                }

                int n = InputChia.SoPhanChia;
                decimal origPhong = hdGoc.TienPhong ?? 0;
                decimal origDV = hdGoc.TienDichVu ?? 0;
                decimal origNH = hdGoc.TienNhaHang ?? 0;
                decimal origGiam = hdGoc.TienGiamGia ?? 0;
                decimal origVAT = hdGoc.TienThueVAT ?? 0;
                decimal origCoc = hdGoc.TienCocDaTru ?? 0;
                decimal origTong = hdGoc.TongTienThanhToan ?? 0;

                // Tính phần nguyên cho (n - 1) hóa đơn con mới
                decimal partPhong = Math.Floor(origPhong / n);
                decimal partDV = Math.Floor(origDV / n);
                decimal partNH = Math.Floor(origNH / n);
                decimal partGiam = Math.Floor(origGiam / n);
                decimal partVAT = Math.Floor(origVAT / n);
                decimal partCoc = Math.Floor(origCoc / n);
                decimal partTong = Math.Floor(origTong / n);

                string prefixMa = "HD" + DateTime.Now.ToString("yyMMddHHmmss");
                var dsMaMoi = new List<string>();

                // Độ dài mã tối đa 17 ký tự (đạt chuẩn VARCHAR(20) trong CSDL)
                for (int i = 2; i <= n; i++)
                {
                    string maMoi = $"{prefixMa}C{i}";
                    dsMaMoi.Add(maMoi);

                    _context.HOA_DON.Add(new HOA_DON
                    {
                        MaHoaDon = maMoi,
                        KHACH_HANGMaKH = hdGoc.KHACH_HANGMaKH,
                        NHAN_VIENMaNV = hdGoc.NHAN_VIENMaNV,
                        DAT_PHONGMaDatPhong = hdGoc.DAT_PHONGMaDatPhong,
                        NgayLap = DateTime.Now,
                        TienPhong = partPhong,
                        TienDichVu = partDV,
                        TienNhaHang = partNH,
                        TienGiamGia = partGiam,
                        TienThueVAT = partVAT,
                        TienCocDaTru = partCoc,
                        TongTienThanhToan = partTong,
                        TrangThaiThanhToan = "Chưa thanh toán"
                    });
                }

                // Hóa đơn gốc giữ phần còn lại để đảm bảo tổng tiền sau khi chia khớp tuyệt đối 100%
                hdGoc.TienPhong = origPhong - partPhong * (n - 1);
                hdGoc.TienDichVu = origDV - partDV * (n - 1);
                hdGoc.TienNhaHang = origNH - partNH * (n - 1);
                hdGoc.TienGiamGia = origGiam - partGiam * (n - 1);
                hdGoc.TienThueVAT = origVAT - partVAT * (n - 1);
                hdGoc.TienCocDaTru = origCoc - partCoc * (n - 1);
                hdGoc.TongTienThanhToan = origTong - partTong * (n - 1);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Đã chia hóa đơn [{hdGoc.MaHoaDon}] thành {n} phần (~{partTong:N0} ₫/phần). Hóa đơn mới: {string.Join(", ", dsMaMoi)}.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ThongBaoLoi = "Lỗi khi chia hóa đơn: " + ex.Message;
            }
            return RedirectToPage();
        }

        /// <summary>
        /// Ghép 2 hoặc NHIỀU hóa đơn chưa thanh toán cùng lúc thành 1 hóa đơn duy nhất bằng Transaction nguyên tử
        /// </summary>
        public async Task<IActionResult> OnPostGhepNhieuHoaDonAsync()
        {
            var danhSachHopLe = DanhSachMaHoaDonGhep
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .ToList();

            if (danhSachHopLe.Count < 2)
            {
                ThongBaoLoi = "Vui lòng chọn ít nhất 2 hóa đơn chưa thanh toán để ghép.";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cacHoaDon = await _context.HOA_DON
                    .Where(h => danhSachHopLe.Contains(h.MaHoaDon))
                    .ToListAsync();

                if (cacHoaDon.Count < 2)
                {
                    ThongBaoLoi = "Không tìm thấy đủ các hóa đơn hợp lệ trong CSDL.";
                    return RedirectToPage();
                }

                if (cacHoaDon.Any(h => h.TrangThaiThanhToan == "Đã thanh toán"))
                {
                    ThongBaoLoi = "Không thể ghép vì danh sách có chứa hóa đơn đã thanh toán.";
                    return RedirectToPage();
                }

                // Chọn hóa đơn chính giữ lại (theo lựa chọn của người dùng hoặc mặc định là hóa đơn đầu tiên)
                HOA_DON hdChinh = (!string.IsNullOrEmpty(MaHoaDonGiuLai) && cacHoaDon.Any(h => h.MaHoaDon == MaHoaDonGiuLai))
                    ? cacHoaDon.First(h => h.MaHoaDon == MaHoaDonGiuLai)
                    : cacHoaDon.First();

                var cacHoaDonPhu = cacHoaDon.Where(h => h.MaHoaDon != hdChinh.MaHoaDon).ToList();
                var maHoaDonPhuList = cacHoaDonPhu.Select(h => h.MaHoaDon).ToList();

                // Cộng dồn toàn bộ hạng mục doanh thu, giảm giá và cọc từ tất cả hóa đơn phụ vào hóa đơn chính
                foreach (var hdPhu in cacHoaDonPhu)
                {
                    hdChinh.TienPhong = (hdChinh.TienPhong ?? 0) + (hdPhu.TienPhong ?? 0);
                    hdChinh.TienDichVu = (hdChinh.TienDichVu ?? 0) + (hdPhu.TienDichVu ?? 0);
                    hdChinh.TienNhaHang = (hdChinh.TienNhaHang ?? 0) + (hdPhu.TienNhaHang ?? 0);
                    hdChinh.TienGiamGia = (hdChinh.TienGiamGia ?? 0) + (hdPhu.TienGiamGia ?? 0);
                    hdChinh.TienCocDaTru = (hdChinh.TienCocDaTru ?? 0) + (hdPhu.TienCocDaTru ?? 0);
                }

                TinhLaiTongHoaDon(hdChinh);

                // Chuyển liên kết các bản ghi lịch sử THANH_TOAN cũ (nếu có, VD: giao dịch đã hoàn tiền) sang hóa đơn chính để tránh lỗi khóa ngoại
                var giaoDichCu = await _context.THANH_TOAN
                    .Where(t => t.HOA_DONMaHoaDon != null && maHoaDonPhuList.Contains(t.HOA_DONMaHoaDon))
                    .ToListAsync();

                foreach (var gd in giaoDichCu)
                {
                    gd.HOA_DONMaHoaDon = hdChinh.MaHoaDon;
                }

                // Xóa hàng loạt các hóa đơn phụ đã được gộp
                _context.HOA_DON.RemoveRange(cacHoaDonPhu);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Đã ghép thành công {cacHoaDon.Count} hóa đơn ({string.Join(", ", maHoaDonPhuList)}) vào hóa đơn chính [{hdChinh.MaHoaDon}] (Tổng mới: {hdChinh.TongTienThanhToan:N0} ₫).";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ThongBaoLoi = "Lỗi khi ghép nhiều hóa đơn: " + ex.Message;
            }
            return RedirectToPage();
        }

        private void TinhLaiTongHoaDon(HOA_DON hd)
        {
            decimal tamTinh = Math.Max(0, (hd.TienPhong ?? 0) + (hd.TienDichVu ?? 0) + (hd.TienNhaHang ?? 0) - (hd.TienGiamGia ?? 0));
            hd.TienThueVAT = Math.Round(tamTinh * 0.10m, 0);
            hd.TongTienThanhToan = Math.Max(0, tamTinh + hd.TienThueVAT.Value - (hd.TienCocDaTru ?? 0));
        }
    }
}