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
    public class QuanLyNhapKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private static readonly object _logFileLock = new object();
        private static readonly object _poFileLock = new object();

        public QuanLyNhapKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public bool XemDonPO { get; set; } = false;

        public IList<KHO_VAT_TU> DanhSachVatTu { get; set; } = new List<KHO_VAT_TU>();

        public List<PhieuNhapGroupViewModel> LichSuPhieuNhap { get; set; } = new List<PhieuNhapGroupViewModel>();

        public List<DonDatHangPOViewModel> DanhSachDonPO { get; set; } = new List<DonDatHangPOViewModel>();
        public int SoDonPOChoNhap { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime TuNgayLichSu { get; set; } = DateTime.Today.AddDays(-29);

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime DenNgayLichSu { get; set; } = DateTime.Today;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
        public string NhaCungCap { get; set; } = string.Empty;

        [BindProperty]
        public string? GhiChu { get; set; }

        [BindProperty]
        public List<NhapKhoItemInput> Items { get; set; } = new List<NhapKhoItemInput>();

        public string LoiLocNgay { get; set; } = string.Empty;

        [TempData]
        public string ThongBao { get; set; } = string.Empty;

        [TempData]
        public string ThongBaoLoi { get; set; } = string.Empty;

        public class NhapKhoItemInput
        {
            public string MaVatTu { get; set; } = string.Empty;
            public int SoLuongNhap { get; set; } = 1;
            public decimal DonGiaNhapMoi { get; set; } = 0;
        }

        public class ChiTietPhieuNhapViewModel
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiKho { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien => SoLuong * DonGia;
        }

        public class PhieuNhapGroupViewModel
        {
            public string MaPhieu { get; set; } = string.Empty;
            public string NhaCungCap { get; set; } = string.Empty;
            public string GhiChu { get; set; } = string.Empty;
            public DateTime ThoiGian { get; set; }
            public List<ChiTietPhieuNhapViewModel> ChiTietVatTu { get; set; } = new List<ChiTietPhieuNhapViewModel>();
            public decimal TongThanhTien => ChiTietVatTu.Sum(x => x.ThanhTien);
        }

        public class DonDatHangPOViewModel
        {
            public string MaPO { get; set; } = string.Empty;
            public string NhaCungCap { get; set; } = string.Empty;
            public string NguoiLap { get; set; } = string.Empty;
            public string TomTatMatHang { get; set; } = string.Empty;
            public decimal TongTienDuToan { get; set; }
            public string TrangThai { get; set; } = string.Empty;
            public string GhiChu { get; set; } = string.Empty;
            public DateTime NgayLap { get; set; }
            public string DuLieuChiTietRaw { get; set; } = string.Empty;
        }

        /// <summary>
        /// Tải dữ liệu đúng theo chế độ đang mở (Màn hình Nhập kho hoặc Toàn màn hình Nghiệm thu PO)
        /// </summary>
        public async Task OnGetAsync()
        {
            DanhSachDonPO = DocDanhSachPO();
            SoDonPOChoNhap = DanhSachDonPO.Count(x => x.TrangThai != "Đã nhận hàng & Nhập kho");

            if (XemDonPO)
            {
                return;
            }

            await LoadDataAsync();

            if (!Items.Any())
            {
                Items.Add(new NhapKhoItemInput { SoLuongNhap = 1, DonGiaNhapMoi = 0 });
            }
        }

        /// <summary>
        /// Xử lý xác nhận nhập kho từ Đơn đặt hàng (PO)
        /// </summary>
        public async Task<IActionResult> OnPostXacNhanNhapPOAsync(string maPO)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maPO))
                {
                    ThongBaoLoi = "Mã đơn đặt hàng (PO) không hợp lệ.";
                    return RedirectToPage(new { XemDonPO = true });
                }

                string poFilePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_dondathang_po_meta.txt");
                if (!System.IO.File.Exists(poFilePath))
                {
                    ThongBaoLoi = "Không tìm thấy tệp dữ liệu đơn đặt hàng (PO).";
                    return RedirectToPage(new { XemDonPO = true });
                }

                List<string> poLines;
                lock (_poFileLock)
                {
                    poLines = System.IO.File.ReadAllLines(poFilePath).ToList();
                }

                int viTriDong = -1;
                string[]? cotPO = null;

                for (int i = 0; i < poLines.Count; i++)
                {
                    var p = poLines[i].Split('|');
                    if (p.Length >= 9 && p[0].Trim() == maPO.Trim())
                    {
                        if (p[5].Trim() == "Đã nhận hàng & Nhập kho")
                        {
                            ThongBaoLoi = $"Đơn [{maPO}] đã được xác nhận nhập kho trước đó.";
                            return RedirectToPage(new { XemDonPO = true });
                        }

                        viTriDong = i;
                        cotPO = p;
                        break;
                    }
                }

                if (viTriDong < 0 || cotPO == null)
                {
                    ThongBaoLoi = $"Không tìm thấy đơn đặt hàng [{maPO}].";
                    return RedirectToPage(new { XemDonPO = true });
                }

                string nhaCungCap = cotPO[1].Trim();
                string rawItems = cotPO[8].Trim();
                var itemTokens = rawItems.Split(';', StringSplitOptions.RemoveEmptyEntries);

                var cacMucPO = new List<(string MaVT, int SoLuong, decimal DonGia)>();
                foreach (var token in itemTokens)
                {
                    var sub = token.Split('^');
                    if (sub.Length >= 3
                        && int.TryParse(sub[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int slNhap)
                        && decimal.TryParse(sub[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal donGia)
                        && slNhap > 0)
                    {
                        cacMucPO.Add((sub[0].Trim(), slNhap, donGia));
                    }
                }

                if (!cacMucPO.Any())
                {
                    ThongBaoLoi = $"Đơn [{maPO}] không có chi tiết mặt hàng hợp lệ để nhập kho.";
                    return RedirectToPage(new { XemDonPO = true });
                }

                var maList = cacMucPO.Select(x => x.MaVT).Distinct().ToList();
                var vatTuDbDict = await _context.KHO_VAT_TU
                    .Where(v => maList.Contains(v.MaVatTu))
                    .ToDictionaryAsync(v => v.MaVatTu, v => v);

                var logNhapLines = new List<string>();
                string thoiGianStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                int soMatHangDaCong = 0;

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    foreach (var muc in cacMucPO)
                    {
                        if (!vatTuDbDict.TryGetValue(muc.MaVT, out var vtDb)) continue;

                        vtDb.SoLuongTon = (vtDb.SoLuongTon ?? 0) + muc.SoLuong;
                        decimal donGiaGhiNhan = muc.DonGia > 0 ? muc.DonGia : (vtDb.DonGiaNhap ?? 0);
                        if (muc.DonGia > 0)
                        {
                            vtDb.DonGiaNhap = muc.DonGia;
                        }

                        soMatHangDaCong++;

                        string dongLog = string.Join("|",
                            "PN_" + maPO,
                            "NHAP",
                            vtDb.MaVatTu,
                            (vtDb.TenVatTu ?? "Vật tư").Replace("|", "/"),
                            (vtDb.LoaiVatTu ?? "Kho chung").Replace("|", "/"),
                            (vtDb.DonViTinh ?? "Cái").Replace("|", "/"),
                            muc.SoLuong.ToString(CultureInfo.InvariantCulture),
                            donGiaGhiNhan.ToString(CultureInfo.InvariantCulture),
                            nhaCungCap.Replace("|", "/"),
                            $"Xác nhận nhập kho từ đơn {maPO}",
                            thoiGianStr
                        );
                        logNhapLines.Add(dongLog);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }

                lock (_poFileLock)
                {
                    var latestLines = System.IO.File.ReadAllLines(poFilePath).ToList();
                    for (int i = 0; i < latestLines.Count; i++)
                    {
                        var p = latestLines[i].Split('|');
                        if (p.Length >= 9 && p[0].Trim() == maPO.Trim())
                        {
                            p[5] = "Đã nhận hàng & Nhập kho";
                            latestLines[i] = string.Join("|", p);
                            break;
                        }
                    }
                    System.IO.File.WriteAllLines(poFilePath, latestLines);
                }

                if (logNhapLines.Any())
                {
                    string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                    lock (_logFileLock)
                    {
                        System.IO.File.AppendAllLines(logFilePath, logNhapLines);
                    }
                }

                ThongBao = $"Đã xác nhận nhập kho đơn [{maPO}] thành công ({soMatHangDaCong} mặt hàng đã được cộng vào tồn kho)!";
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi xác nhận nhập kho đơn PO: " + ex.Message;
            }

            return RedirectToPage(new
            {
                XemDonPO = true,
                TuNgayLichSu = TuNgayLichSu.ToString("yyyy-MM-dd"),
                DenNgayLichSu = DenNgayLichSu.ToString("yyyy-MM-dd")
            });
        }

        /// <summary>
        /// Lưu phiếu nhập thủ công gồm nhiều vật tư vào KHO_VAT_TU trong Transaction
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (TuNgayLichSu == default || DenNgayLichSu == default || TuNgayLichSu.Date > DenNgayLichSu.Date)
            {
                TuNgayLichSu = DateTime.Today.AddDays(-29);
                DenNgayLichSu = DateTime.Today;
            }

            XemDonPO = false;
            DanhSachDonPO = DocDanhSachPO();
            SoDonPOChoNhap = DanhSachDonPO.Count(x => x.TrangThai != "Đã nhận hàng & Nhập kho");

            try
            {
                if (string.IsNullOrWhiteSpace(NhaCungCap))
                {
                    ThongBaoLoi = "Vui lòng nhập tên nhà cung cấp / nguồn nhập.";
                    await LoadDataAsync();
                    if (!Items.Any()) Items.Add(new NhapKhoItemInput());
                    return Page();
                }

                var dongHopLe = (Items ?? new List<NhapKhoItemInput>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.MaVatTu))
                    .ToList();

                if (!dongHopLe.Any())
                {
                    ThongBaoLoi = "Vui lòng chọn ít nhất 1 vật tư để lập phiếu nhập kho.";
                    await LoadDataAsync();
                    if (!Items.Any()) Items.Add(new NhapKhoItemInput());
                    return Page();
                }

                if (dongHopLe.Any(x => x.SoLuongNhap <= 0 || x.DonGiaNhapMoi < 0))
                {
                    ThongBaoLoi = "Số lượng nhập phải lớn hơn 0 và đơn giá không được âm.";
                    await LoadDataAsync();
                    return Page();
                }

                var maList = dongHopLe.Select(x => x.MaVatTu).Distinct().ToList();
                var vatTuDbDict = await _context.KHO_VAT_TU
                    .Where(v => maList.Contains(v.MaVatTu))
                    .ToDictionaryAsync(v => v.MaVatTu, v => v);

                var maKhongTonTai = maList.Where(m => !vatTuDbDict.ContainsKey(m)).ToList();
                if (maKhongTonTai.Any())
                {
                    ThongBaoLoi = "Không tìm thấy vật tư có mã: " + string.Join(", ", maKhongTonTai);
                    await LoadDataAsync();
                    return Page();
                }

                string maPhieu = "PN" + DateTime.Now.ToString("yyMMddHHmmss");
                string thoiGianStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string cleanNcc = NhaCungCap.Replace("|", "/").Trim();
                string cleanGhiChu = (GhiChu ?? "").Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();

                var logLines = new List<string>();
                int tongSoLuongNhap = 0;
                decimal tongGiaTriPhieu = 0;

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    foreach (var item in dongHopLe)
                    {
                        var vtDb = vatTuDbDict[item.MaVatTu];

                        vtDb.SoLuongTon = (vtDb.SoLuongTon ?? 0) + item.SoLuongNhap;

                        decimal donGiaGhiNhan = item.DonGiaNhapMoi > 0
                            ? item.DonGiaNhapMoi
                            : (vtDb.DonGiaNhap ?? 0);

                        if (item.DonGiaNhapMoi > 0)
                        {
                            vtDb.DonGiaNhap = item.DonGiaNhapMoi;
                        }

                        tongSoLuongNhap += item.SoLuongNhap;
                        tongGiaTriPhieu += item.SoLuongNhap * donGiaGhiNhan;

                        string dongLog = string.Join("|",
                            maPhieu,
                            "NHAP",
                            vtDb.MaVatTu,
                            (vtDb.TenVatTu ?? "Vật tư").Replace("|", "/"),
                            (vtDb.LoaiVatTu ?? "Kho chung").Replace("|", "/"),
                            (vtDb.DonViTinh ?? "Cái").Replace("|", "/"),
                            item.SoLuongNhap.ToString(CultureInfo.InvariantCulture),
                            donGiaGhiNhan.ToString(CultureInfo.InvariantCulture),
                            cleanNcc,
                            cleanGhiChu,
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
                    canhBaoLog = $" (Lưu ý: Đã cộng tồn SQL nhưng chưa ghi được nhật ký: {logEx.Message})";
                }

                ThongBao = $"Lập phiếu nhập [{maPhieu}] thành công với {dongHopLe.Count} dòng vật tư (tổng {tongSoLuongNhap:N0} đơn vị, giá trị {tongGiaTriPhieu:N0} ₫).{canhBaoLog}";
                return RedirectToPage(new
                {
                    TuNgayLichSu = TuNgayLichSu.ToString("yyyy-MM-dd"),
                    DenNgayLichSu = DenNgayLichSu.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi hệ thống khi lập phiếu nhập kho: " + ex.Message;
                await LoadDataAsync();
                if (!Items.Any()) Items.Add(new NhapKhoItemInput());
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
                    LichSuPhieuNhap = DocLichSuPhieuNhap(TuNgayLichSu, DenNgayLichSu);
                }
                else
                {
                    LichSuPhieuNhap = new List<PhieuNhapGroupViewModel>();
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tải dữ liệu nhập kho: " + ex.Message;
            }
        }

        private List<DonDatHangPOViewModel> DocDanhSachPO()
        {
            var list = new List<DonDatHangPOViewModel>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_dondathang_po_meta.txt");
                if (!System.IO.File.Exists(filePath)) return list;

                string[] lines;
                lock (_poFileLock)
                {
                    lines = System.IO.File.ReadAllLines(filePath);
                }

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var p = line.Split('|');
                    if (p.Length >= 9)
                    {
                        decimal.TryParse(p[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tong);
                        bool okNgay = DateTime.TryParseExact(
                                          p[7].Trim(),
                                          "yyyy-MM-dd HH:mm:ss",
                                          CultureInfo.InvariantCulture,
                                          DateTimeStyles.None,
                                          out DateTime ngay)
                                      || DateTime.TryParse(p[7].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out ngay);

                        list.Add(new DonDatHangPOViewModel
                        {
                            MaPO = p[0].Trim(),
                            NhaCungCap = p[1].Trim(),
                            NguoiLap = p[2].Trim(),
                            TomTatMatHang = p[3].Trim(),
                            TongTienDuToan = tong,
                            TrangThai = p[5].Trim(),
                            GhiChu = p[6].Trim(),
                            NgayLap = okNgay ? ngay : DateTime.MinValue,
                            DuLieuChiTietRaw = p[8].Trim()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Không thể đọc danh sách đơn đặt hàng (PO): " + ex.Message;
            }

            return list
                .OrderBy(x => x.TrangThai == "Đã nhận hàng & Nhập kho" ? 1 : 0)
                .ThenByDescending(x => x.NgayLap)
                .ToList();
        }

        private List<PhieuNhapGroupViewModel> DocLichSuPhieuNhap(DateTime tuNgay, DateTime denNgay)
        {
            var result = new List<PhieuNhapGroupViewModel>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                if (!System.IO.File.Exists(filePath)) return result;

                DateTime startDate = tuNgay.Date;
                DateTime endDate = denNgay.Date.AddDays(1).AddTicks(-1);

                var rawList = new List<(string MaPhieu, string Ncc, string GhiChu, DateTime ThoiGian, ChiTietPhieuNhapViewModel Item)>();

                foreach (var line in System.IO.File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var p = line.Split('|');
                    if (p.Length < 11 || p[1].Trim() != "NHAP") continue;

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
                        new ChiTietPhieuNhapViewModel
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
                    .Select(g => new PhieuNhapGroupViewModel
                    {
                        MaPhieu = g.Key,
                        NhaCungCap = g.First().Ncc,
                        GhiChu = g.First().GhiChu,
                        ThoiGian = g.Max(x => x.ThoiGian),
                        ChiTietVatTu = g.Select(x => x.Item).ToList()
                    })
                    .OrderByDescending(x => x.ThoiGian)
                    .ToList();
            }
            catch (Exception ex)
            {
                LoiLocNgay = "Không thể đọc lịch sử nhập kho: " + ex.Message;
            }

            return result;
        }
    }
}