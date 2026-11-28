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
    public class QuanLyXuatKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private static readonly object _logFileLock = new object();

        public QuanLyXuatKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();

        public List<PhieuXuatGroupViewModel> LichSuPhieuXuat { get; set; } = new List<PhieuXuatGroupViewModel>();

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime TuNgayLichSu { get; set; } = DateTime.Today.AddDays(-29);

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime DenNgayLichSu { get; set; } = DateTime.Today;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng chọn bộ phận tiếp nhận.")]
        public string BoPhanNhan { get; set; } = "Bộ phận Buồng phòng";

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập lý do xuất kho.")]
        public string LyDoXuat { get; set; } = string.Empty;

        [BindProperty]
        public List<XuatKhoItemInput> Items { get; set; } = new List<XuatKhoItemInput>();

        public string LoiLocNgay { get; set; } = string.Empty;

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        public class XuatKhoItemInput
        {
            public string MaVatTu { get; set; } = string.Empty;
            public int SoLuongXuat { get; set; } = 1;
        }

        public class ChiTietPhieuXuatViewModel
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiKho { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien => SoLuong * DonGia;
        }

        public class PhieuXuatGroupViewModel
        {
            public string MaPhieu { get; set; } = string.Empty;
            public string BoPhanNhan { get; set; } = string.Empty;
            public string LyDoXuat { get; set; } = string.Empty;
            public DateTime ThoiGian { get; set; }
            public List<ChiTietPhieuXuatViewModel> ChiTietVatTu { get; set; } = new List<ChiTietPhieuXuatViewModel>();
            public decimal TongGiaTriXuat => ChiTietVatTu.Sum(x => x.ThanhTien);
        }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();

            if (!Items.Any())
            {
                Items.Add(new XuatKhoItemInput { SoLuongXuat = 1 });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TuNgayLichSu == default || DenNgayLichSu == default || TuNgayLichSu.Date > DenNgayLichSu.Date)
            {
                TuNgayLichSu = DateTime.Today.AddDays(-29);
                DenNgayLichSu = DateTime.Today;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(BoPhanNhan) || string.IsNullOrWhiteSpace(LyDoXuat))
                {
                    ThongBaoLoi = "Vui lòng chọn bộ phận tiếp nhận và nhập lý do xuất kho.";
                    await LoadDataAsync();
                    if (!Items.Any()) Items.Add(new XuatKhoItemInput());
                    return Page();
                }

                var dongHopLe = (Items ?? new List<XuatKhoItemInput>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.MaVatTu))
                    .ToList();

                if (!dongHopLe.Any())
                {
                    ThongBaoLoi = "Vui lòng chọn ít nhất 1 vật tư để lập phiếu xuất kho.";
                    await LoadDataAsync();
                    if (!Items.Any()) Items.Add(new XuatKhoItemInput());
                    return Page();
                }

                if (dongHopLe.Any(x => x.SoLuongXuat <= 0))
                {
                    ThongBaoLoi = "Số lượng xuất của từng vật tư phải lớn hơn 0.";
                    await LoadDataAsync();
                    return Page();
                }

                var nhomXuat = dongHopLe
                    .GroupBy(x => x.MaVatTu)
                    .Select(g => new { MaVatTu = g.Key, TongSoLuong = g.Sum(x => x.SoLuongXuat) })
                    .ToList();

                var maList = nhomXuat.Select(x => x.MaVatTu).ToList();
                var vatTuDbDict = await _context.KHO_VAT_TU
                    .Where(v => maList.Contains(v.MaVatTu))
                    .ToDictionaryAsync(v => v.MaVatTu, v => v);

                var danhSachThieu = new List<string>();
                foreach (var yc in nhomXuat)
                {
                    if (!vatTuDbDict.TryGetValue(yc.MaVatTu, out var vtDb))
                    {
                        danhSachThieu.Add($"[{yc.MaVatTu}] không tồn tại trong kho");
                        continue;
                    }

                    int tonHienTai = vtDb.SoLuongTon ?? 0;
                    if (yc.TongSoLuong > tonHienTai)
                    {
                        danhSachThieu.Add($"'{vtDb.TenVatTu}' (cần xuất {yc.TongSoLuong}, chỉ còn {tonHienTai} {vtDb.DonViTinh})");
                    }
                }

                if (danhSachThieu.Any())
                {
                    ThongBaoLoi = "Không đủ hàng để xuất kho: " + string.Join("; ", danhSachThieu) + ".";
                    await LoadDataAsync();
                    return Page();
                }

                string maPhieu = "PX" + DateTime.Now.ToString("yyMMddHHmmss");
                string thoiGianStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string cleanBoPhan = BoPhanNhan.Replace("|", "/").Trim();
                string cleanLyDo = LyDoXuat.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();

                var logLines = new List<string>();
                var canhBaoChamMin = new List<string>();
                int tongSoLuongXuat = 0;

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    foreach (var yc in nhomXuat)
                    {
                        var vtDb = vatTuDbDict[yc.MaVatTu];
                        int tonMoi = (vtDb.SoLuongTon ?? 0) - yc.TongSoLuong;
                        vtDb.SoLuongTon = tonMoi;

                        tongSoLuongXuat += yc.TongSoLuong;

                        if (tonMoi <= (vtDb.SoLuongTonToiThieu ?? 0))
                        {
                            canhBaoChamMin.Add($"{vtDb.TenVatTu} (còn {tonMoi})");
                        }

                        string dongLog = string.Join("|",
                            maPhieu,
                            "XUAT",
                            vtDb.MaVatTu,
                            (vtDb.TenVatTu ?? "Vật tư").Replace("|", "/"),
                            (vtDb.LoaiVatTu ?? "Kho chung").Replace("|", "/"),
                            (vtDb.DonViTinh ?? "Cái").Replace("|", "/"),
                            yc.TongSoLuong.ToString(CultureInfo.InvariantCulture),
                            (vtDb.DonGiaNhap ?? 0).ToString(CultureInfo.InvariantCulture),
                            cleanBoPhan,
                            cleanLyDo,
                            thoiGianStr
                        );
                        logLines.Add(dongLog);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }

                string canhBaoLog = string.Empty;
                try
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                    lock (_logFileLock)
                    {
                        System.IO.File.AppendAllLines(filePath, logLines);
                    }
                }
                catch (Exception logEx)
                {
                    canhBaoLog = $" (Lưu ý: Đã trừ tồn SQL nhưng chưa ghi được nhật ký: {logEx.Message})";
                }

                string thongTinMin = canhBaoChamMin.Any()
                    ? $" — Cảnh báo chạm định mức tối thiểu: {string.Join(", ", canhBaoChamMin)}."
                    : "";

                ThongBao = $"Lập phiếu xuất [{maPhieu}] cho {BoPhanNhan} thành công ({nhomXuat.Count} mặt hàng, tổng {tongSoLuongXuat:N0} đơn vị).{thongTinMin}{canhBaoLog}";
                return RedirectToPage(new
                {
                    TuNgayLichSu = TuNgayLichSu.ToString("yyyy-MM-dd"),
                    DenNgayLichSu = DenNgayLichSu.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi lập phiếu xuất kho: " + ex.Message;
                await LoadDataAsync();
                if (!Items.Any()) Items.Add(new XuatKhoItemInput());
                return Page();
            }
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

        private async Task LoadDataAsync()
        {
            try
            {
                DanhSachVatTu = await _context.KHO_VAT_TU
                    .AsNoTracking()
                    .OrderBy(v => v.TenVatTu)
                    .ToListAsync();

                if (KiemTraKhoangNgayLichSu())
                {
                    LichSuPhieuXuat = DocLichSuPhieuXuat(TuNgayLichSu, DenNgayLichSu);
                }
                else
                {
                    LichSuPhieuXuat = new List<PhieuXuatGroupViewModel>();
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải dữ liệu xuất kho: " + ex.Message;
            }
        }

        private List<PhieuXuatGroupViewModel> DocLichSuPhieuXuat(DateTime tuNgay, DateTime denNgay)
        {
            var result = new List<PhieuXuatGroupViewModel>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                if (!System.IO.File.Exists(filePath)) return result;

                DateTime startDate = tuNgay.Date;
                DateTime endDate = denNgay.Date.AddDays(1).AddTicks(-1);

                var rawList = new List<(string MaPhieu, string BoPhan, string LyDo, DateTime ThoiGian, ChiTietPhieuXuatViewModel Item)>();

                foreach (var line in System.IO.File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var p = line.Split('|');
                    if (p.Length < 11 || p[1].Trim() != "XUAT") continue;

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

                    rawList.Add((
                        p[0].Trim(),
                        p[8].Trim(),
                        p[9].Trim(),
                        tg,
                        new ChiTietPhieuXuatViewModel
                        {
                            MaVatTu = p[2].Trim(),
                            TenVatTu = p[3].Trim(),
                            LoaiKho = p[4].Trim(),
                            DonViTinh = p[5].Trim(),
                            SoLuong = sl,
                            DonGia = gia
                        }
                    ));
                }

                result = rawList
                    .GroupBy(x => x.MaPhieu)
                    .Select(g => new PhieuXuatGroupViewModel
                    {
                        MaPhieu = g.Key,
                        BoPhanNhan = g.First().BoPhan,
                        LyDoXuat = g.First().LyDo,
                        ThoiGian = g.Max(x => x.ThoiGian),
                        ChiTietVatTu = g.Select(x => x.Item).ToList()
                    })
                    .OrderByDescending(x => x.ThoiGian)
                    .ToList();
            }
            catch (Exception ex)
            {
                LoiLocNgay = "Không thể đọc lịch sử xuất kho: " + ex.Message;
            }

            return result;
        }
    }
}