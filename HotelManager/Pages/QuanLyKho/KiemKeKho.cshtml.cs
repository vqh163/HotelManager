using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyKho
{
    [Authorize]
    public class KiemKeKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private static readonly object _logFileLock = new object();

        public KiemKeKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public bool XemLichSu { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public string LoaiVatTuFilter { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime TuNgayLichSu { get; set; } = DateTime.Today.AddDays(-29);

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime DenNgayLichSu { get; set; } = DateTime.Today;

        [BindProperty]
        public string? GhiChuKiemKe { get; set; }

        [BindProperty]
        public bool XacNhanGhiDe { get; set; } = false;

        [BindProperty]
        public List<KiemKeItemInput> ItemsKiemKe { get; set; } = new List<KiemKeItemInput>();

        public List<DotKiemKeTomTat> LichSuKiemKe { get; set; } = new List<DotKiemKeTomTat>();

        public bool CoXungDotTonKho { get; set; } = false;
        public string ChiTietXungDot { get; set; } = string.Empty;

        public string LoiLocNgay { get; set; } = string.Empty;

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        public class KiemKeItemInput
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiVatTu { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public decimal DonGiaNhap { get; set; }
            public int TonHeThong { get; set; }
            public int TonThucTe { get; set; }
            public string? GhiChuDong { get; set; }
        }

        public class DotKiemKeTomTat
        {
            public string MaPhieu { get; set; } = string.Empty;
            public DateTime ThoiGian { get; set; }
            public string NguoiThucHien { get; set; } = string.Empty;
            public int SoMatHangDieuChinh { get; set; }
            public decimal TongGiaTriThieu { get; set; }
            public decimal TongGiaTriThua { get; set; }
            public string GhiChu { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            try
            {
                if (TuNgayLichSu == default) TuNgayLichSu = DateTime.Today.AddDays(-29);
                if (DenNgayLichSu == default) DenNgayLichSu = DateTime.Today;

                if (XemLichSu)
                {
                    if (KiemTraKhoangNgayLichSu())
                    {
                        LichSuKiemKe = DocLichSuKiemKeTheoKhoangNgay(TuNgayLichSu, DenNgayLichSu);
                    }
                    else
                    {
                        LichSuKiemKe = new List<DotKiemKeTomTat>();
                    }
                    return;
                }

                var query = _context.KHO_VAT_TU.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(LoaiVatTuFilter) && LoaiVatTuFilter != "ALL")
                {
                    query = query.Where(v => v.LoaiVatTu == LoaiVatTuFilter);
                }

                var danhSachDb = await query
                    .OrderBy(v => v.LoaiVatTu)
                    .ThenBy(v => v.TenVatTu)
                    .ToListAsync();

                ItemsKiemKe = danhSachDb.Select(v => new KiemKeItemInput
                {
                    MaVatTu = v.MaVatTu,
                    TenVatTu = v.TenVatTu ?? "Chưa đặt tên",
                    LoaiVatTu = v.LoaiVatTu ?? "Chưa phân loại",
                    DonViTinh = v.DonViTinh ?? "Cái",
                    DonGiaNhap = v.DonGiaNhap ?? 0,
                    TonHeThong = v.SoLuongTon ?? 0,
                    TonThucTe = v.SoLuongTon ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải dữ liệu kiểm kê kho: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TuNgayLichSu == default || DenNgayLichSu == default || TuNgayLichSu.Date > DenNgayLichSu.Date)
            {
                TuNgayLichSu = DateTime.Today.AddDays(-29);
                DenNgayLichSu = DateTime.Today;
            }

            XemLichSu = false;

            try
            {
                if (ItemsKiemKe == null || !ItemsKiemKe.Any())
                {
                    ThongBaoLoi = "Không có dữ liệu vật tư nào để cân bằng kho.";
                    return RedirectToPage(new
                    {
                        LoaiVatTuFilter,
                        TuNgayLichSu = TuNgayLichSu.ToString("yyyy-MM-dd"),
                        DenNgayLichSu = DenNgayLichSu.ToString("yyyy-MM-dd")
                    });
                }

                if (ItemsKiemKe.Any(x => x.TonThucTe < 0))
                {
                    ThongBaoLoi = "Số lượng kiểm đếm thực tế không được nhỏ hơn 0.";
                    return Page();
                }

                var maVatTuList = ItemsKiemKe.Select(x => x.MaVatTu).Distinct().ToList();
                var vatTuDbDict = await _context.KHO_VAT_TU
                    .Where(v => maVatTuList.Contains(v.MaVatTu))
                    .ToDictionaryAsync(v => v.MaVatTu, v => v);

                if (!XacNhanGhiDe)
                {
                    var xungDotList = new List<string>();
                    foreach (var item in ItemsKiemKe)
                    {
                        if (!vatTuDbDict.TryGetValue(item.MaVatTu, out var vtDb)) continue;
                        int tonDbHienTai = vtDb.SoLuongTon ?? 0;

                        if (tonDbHienTai != item.TonHeThong)
                        {
                            xungDotList.Add($"{item.MaVatTu} (lúc mở: {item.TonHeThong}, hiện tại DB: {tonDbHienTai})");
                        }
                    }

                    if (xungDotList.Any())
                    {
                        CoXungDotTonKho = true;
                        ChiTietXungDot = string.Join("; ", xungDotList.Take(5))
                            + (xungDotList.Count > 5 ? $" và {xungDotList.Count - 5} mã khác." : ".");
                        return Page();
                    }
                }

                int soMatHangCapNhat = 0;
                int soMatHangThieu = 0;
                int soMatHangThua = 0;
                var cacDongLogKiemKe = new List<string>();

                string maPhieuKK = "KK" + DateTime.Now.ToString("yyMMddHHmmss");
                string nguoiKiemKe = User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name)
                    ? User.Identity.Name
                    : "Thủ kho";
                string thoiGianStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    foreach (var item in ItemsKiemKe)
                    {
                        if (!vatTuDbDict.TryGetValue(item.MaVatTu, out var vtDb)) continue;

                        int tonCu = vtDb.SoLuongTon ?? 0;
                        int tonMoi = item.TonThucTe;
                        int doLech = tonMoi - tonCu;

                        if (doLech != 0)
                        {
                            vtDb.SoLuongTon = tonMoi;
                            soMatHangCapNhat++;

                            string loaiLog = doLech < 0 ? "KIEM_KE_THIEU" : "KIEM_KE_THUA";
                            if (doLech < 0) soMatHangThieu++; else soMatHangThua++;

                            string ghiChuHopNhat = string.Join(" - ", new[]
                            {
                                string.IsNullOrWhiteSpace(GhiChuKiemKe) ? "Kiểm kê định kỳ" : GhiChuKiemKe.Trim(),
                                item.GhiChuDong?.Trim()
                            }.Where(s => !string.IsNullOrWhiteSpace(s)));

                            string dongLog = string.Join("|",
                                maPhieuKK,
                                loaiLog,
                                vtDb.MaVatTu,
                                (vtDb.TenVatTu ?? "Vật tư").Replace("|", "/"),
                                (vtDb.LoaiVatTu ?? "Kho chung").Replace("|", "/"),
                                (vtDb.DonViTinh ?? "Cái").Replace("|", "/"),
                                Math.Abs(doLech).ToString(CultureInfo.InvariantCulture),
                                (vtDb.DonGiaNhap ?? 0).ToString(CultureInfo.InvariantCulture),
                                nguoiKiemKe.Replace("|", "/"),
                                ghiChuHopNhat.Replace("|", "/").Replace("\r", " ").Replace("\n", " "),
                                thoiGianStr
                            );

                            cacDongLogKiemKe.Add(dongLog);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }

                if (soMatHangCapNhat == 0)
                {
                    string ghiChuKhop = string.IsNullOrWhiteSpace(GhiChuKiemKe)
                        ? "Kiểm kê định kỳ (Khớp 100%)"
                        : GhiChuKiemKe.Trim();

                    string phamViKho = string.IsNullOrWhiteSpace(LoaiVatTuFilter) || LoaiVatTuFilter == "ALL"
                        ? "Tất cả kho"
                        : LoaiVatTuFilter;

                    string dongLogKhop = string.Join("|",
                        maPhieuKK,
                        "KIEM_KE_KHOP",
                        "ALL",
                        "Đối soát toàn bộ vật tư",
                        phamViKho.Replace("|", "/"),
                        "-",
                        "0",
                        "0",
                        nguoiKiemKe.Replace("|", "/"),
                        ghiChuKhop.Replace("|", "/").Replace("\r", " ").Replace("\n", " "),
                        thoiGianStr
                    );

                    cacDongLogKiemKe.Add(dongLogKhop);
                }

                string canhBaoLog = string.Empty;
                if (cacDongLogKiemKe.Any())
                {
                    try
                    {
                        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                        lock (_logFileLock)
                        {
                            System.IO.File.AppendAllLines(filePath, cacDongLogKiemKe);
                        }
                    }
                    catch (Exception logEx)
                    {
                        canhBaoLog = $" (Lưu ý: Đã lưu SQL nhưng chưa ghi được nhật ký kiểm kê: {logEx.Message})";
                    }
                }

                ThongBao = soMatHangCapNhat > 0
                    ? $"Phiếu [{maPhieuKK}] đã cân bằng tồn kho cho {soMatHangCapNhat}/{ItemsKiemKe.Count} mặt hàng ({soMatHangThieu} thiếu, {soMatHangThua} thừa).{canhBaoLog}"
                    : $"Phiếu [{maPhieuKK}] đã đối soát {ItemsKiemKe.Count} mặt hàng (khớp 100% sổ sách) và lưu vào lịch sử.{canhBaoLog}";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi cập nhật kiểm kê kho: " + ex.Message;
            }

            return RedirectToPage(new
            {
                LoaiVatTuFilter,
                TuNgayLichSu = TuNgayLichSu.ToString("yyyy-MM-dd"),
                DenNgayLichSu = DenNgayLichSu.ToString("yyyy-MM-dd")
            });
        }

        private bool KiemTraKhoangNgayLichSu()
        {
            if (TuNgayLichSu == default) TuNgayLichSu = DateTime.Today.AddDays(-29);
            if (DenNgayLichSu == default) DenNgayLichSu = DateTime.Today;

            if (TuNgayLichSu.Date > DenNgayLichSu.Date)
            {
                LoiLocNgay = "Khoảng ngày lọc lịch sử không hợp lệ: 'Từ ngày' không được lớn hơn 'Đến ngày'.";
                return false;
            }

            return true;
        }

        private List<DotKiemKeTomTat> DocLichSuKiemKeTheoKhoangNgay(DateTime tuNgay, DateTime denNgay)
        {
            var result = new List<DotKiemKeTomTat>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                if (!System.IO.File.Exists(filePath)) return result;

                DateTime startDate = tuNgay.Date;
                DateTime endDate = denNgay.Date.AddDays(1).AddTicks(-1);

                var cacDong = new List<(string MaPhieu, string Loai, decimal ThanhTien, string Nguoi, string GhiChu, DateTime ThoiGian)>();

                foreach (var line in System.IO.File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var p = line.Split('|');
                    if (p.Length < 11) continue;

                    string loai = p[1].Trim();
                    if (loai != "KIEM_KE_THIEU" && loai != "KIEM_KE_THUA" && loai != "KIEM_KE_KHOP") continue;

                    if (!int.TryParse(p[6].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sl)) continue;
                    if (!decimal.TryParse(p[7].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal gia)) continue;

                    bool okTg = DateTime.TryParseExact(
                                    p[10].Trim(),
                                    "yyyy-MM-dd HH:mm:ss",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime tg)
                                || DateTime.TryParse(p[10].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out tg);

                    if (!okTg || tg < startDate || tg > endDate) continue;

                    cacDong.Add((p[0].Trim(), loai, sl * gia, p[8].Trim(), p[9].Trim(), tg));
                }

                result = cacDong
                    .GroupBy(x => x.MaPhieu)
                    .Select(g => new DotKiemKeTomTat
                    {
                        MaPhieu = g.Key,
                        ThoiGian = g.Max(x => x.ThoiGian),
                        NguoiThucHien = g.First().Nguoi,
                        SoMatHangDieuChinh = g.Count(x => x.Loai != "KIEM_KE_KHOP"),
                        TongGiaTriThieu = g.Where(x => x.Loai == "KIEM_KE_THIEU").Sum(x => x.ThanhTien),
                        TongGiaTriThua = g.Where(x => x.Loai == "KIEM_KE_THUA").Sum(x => x.ThanhTien),
                        GhiChu = g.First().GhiChu
                    })
                    .OrderByDescending(x => x.ThoiGian)
                    .ToList();
            }
            catch (Exception ex)
            {
                LoiLocNgay = "Không thể đọc lịch sử kiểm kê: " + ex.Message;
            }

            return result;
        }
    }
}