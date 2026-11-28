using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyBaoCaoThongKe
{
    public class DuBaoCongSuatModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DuBaoCongSuatModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dữ liệu dự báo từng ngày
        public class DailyForecastItem
        {
            public DateTime Ngay { get; set; }
            public string ThuTrongTuan { get; set; } = string.Empty;
            public bool LaCuoiTuan { get; set; }
            public int TongSoPhong { get; set; }
            public int SoPhongDaDat { get; set; }
            public int SoPhongTrong => Math.Max(0, TongSoPhong - SoPhongDaDat);
            public decimal TyLeLapDay { get; set; } // %
            public decimal GiaGocTrungBinh { get; set; }
            public int PhanTramDieuChinh { get; set; } // VD: +25, +15, 0, -10
            public decimal GiaGoiYDong { get; set; }
            public decimal DoanhThuDaChot { get; set; }
            public decimal DoanhThuTiemNang => DoanhThuDaChot + (SoPhongTrong * GiaGoiYDong);
            public string MucDo { get; set; } = string.Empty; // "Cao điểm", "Nhu cầu cao", "Ổn định", "Thấp điểm"
            public string KhuyenNghi { get; set; } = string.Empty;
        }

        // Bảng định giá động theo từng Loại phòng
        public class RoomTypePricingSuggestion
        {
            public string LoaiPhong { get; set; } = string.Empty;
            public int TongPhong { get; set; }
            public decimal CongSuatTrungBinh { get; set; }
            public decimal GiaHienTai { get; set; }
            public int PhanTramGoiY { get; set; }
            public decimal GiaDeXuat { get; set; }
            public string LyDo { get; set; } = string.Empty;
        }

        // --- THAM SỐ LỌC ---
        [BindProperty(SupportsGet = true)]
        public DateTime? TuNgay { get; set; }

        [BindProperty(SupportsGet = true)]
        public int SoNgayDuBao { get; set; } = 14; // 7, 14, 30 ngày

        [BindProperty(SupportsGet = true)]
        public string? LoaiPhongLoc { get; set; }

        // --- DỮ LIỆU ĐỔ RA VIEW ---
        public List<DailyForecastItem> DanhSachDuBao { get; set; } = new();
        public List<RoomTypePricingSuggestion> BangGiaTheoLoaiPhong { get; set; } = new();
        public List<string> DanhSachLoaiPhong { get; set; } = new();

        // --- KPI TỔNG QUAN ---
        public int TongSoPhongKhaoSat { get; set; }
        public decimal CongSuatTrungBinhKy { get; set; }
        public int SoNgayCaoDiem { get; set; }
        public decimal GiaGoiYTrungBinh { get; set; }
        public decimal TongDoanhThuDaChot { get; set; }
        public decimal TongDoanhThuTiemNang { get; set; }

        [TempData]
        public string? ThongBao { get; set; }

        public async Task OnGetAsync()
        {
            TuNgay ??= DateTime.Today;
            if (SoNgayDuBao != 7 && SoNgayDuBao != 14 && SoNgayDuBao != 30)
                SoNgayDuBao = 14;

            await LoadForecastDataAsync();
        }

        // Áp dụng mức giá động mới vào cột GiaTheoNgay của tất cả phòng thuộc Loại phòng được chọn
        public async Task<IActionResult> OnPostApDungGiaAsync(string loaiPhong, decimal giaMoi)
        {
            if (string.IsNullOrWhiteSpace(loaiPhong) || giaMoi <= 0)
            {
                ThongBao = "Lỗi: Vui lòng nhập mức giá hợp lệ lớn hơn 0.";
                return RedirectToPage(new { TuNgay = TuNgay?.ToString("yyyy-MM-dd"), SoNgayDuBao, LoaiPhongLoc });
            }

            var phongCanCapNhat = await _context.PHONG
                .Where(p => p.LoaiPhong == loaiPhong)
                .ToListAsync();

            if (!phongCanCapNhat.Any())
            {
                ThongBao = $"Không tìm thấy phòng nào thuộc loại '{loaiPhong}'.";
                return RedirectToPage(new { TuNgay = TuNgay?.ToString("yyyy-MM-dd"), SoNgayDuBao, LoaiPhongLoc });
            }

            foreach (var p in phongCanCapNhat)
            {
                p.GiaTheoNgay = giaMoi;
            }

            await _context.SaveChangesAsync();
            ThongBao = $"Đã cập nhật giá động cho {phongCanCapNhat.Count} phòng loại \"{loaiPhong}\" thành {giaMoi:N0} ₫/đêm.";

            return RedirectToPage(new { TuNgay = TuNgay?.ToString("yyyy-MM-dd"), SoNgayDuBao, LoaiPhongLoc });
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            TuNgay ??= DateTime.Today;
            if (SoNgayDuBao != 7 && SoNgayDuBao != 14 && SoNgayDuBao != 30)
                SoNgayDuBao = 14;

            await LoadForecastDataAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Ngày,Thứ,Tổng phòng,Phòng đã đặt,Phòng trống,Công suất dự báo (%),Giá gốc TB (VNĐ),Điều chỉnh (%),Giá gợi ý động (VNĐ),Doanh thu đã chốt (VNĐ),Doanh thu tiềm năng (VNĐ),Khuyến nghị");

            foreach (var item in DanhSachDuBao)
            {
                sb.AppendLine(string.Join(",",
                    item.Ngay.ToString("dd/MM/yyyy"),
                    EscapeCsv(item.ThuTrongTuan),
                    item.TongSoPhong,
                    item.SoPhongDaDat,
                    item.SoPhongTrong,
                    item.TyLeLapDay,
                    item.GiaGocTrungBinh,
                    $"{(item.PhanTramDieuChinh > 0 ? "+" : "")}{item.PhanTramDieuChinh}%",
                    item.GiaGoiYDong,
                    item.DoanhThuDaChot,
                    item.DoanhThuTiemNang,
                    EscapeCsv(item.KhuyenNghi)
                ));
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"DuBaoCongSuat_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private async Task LoadForecastDataAsync()
        {
            var dsPhongAll = await _context.PHONG.AsNoTracking().ToListAsync();
            var dsDatPhong = await _context.DAT_PHONG.AsNoTracking().ToListAsync();
            var dsChiTietDP = await _context.CHI_TIET_DAT_PHONG.AsNoTracking().ToListAsync();

            DanhSachLoaiPhong = dsPhongAll
                .Where(p => !string.IsNullOrWhiteSpace(p.LoaiPhong))
                .Select(p => p.LoaiPhong!.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var dsPhongLoc = string.IsNullOrWhiteSpace(LoaiPhongLoc)
                ? dsPhongAll
                : dsPhongAll.Where(p => string.Equals(p.LoaiPhong?.Trim(), LoaiPhongLoc.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            var phongMapAll = dsPhongAll.ToDictionary(p => p.MaPhong);
            var maPhongLocSet = new HashSet<string>(dsPhongLoc.Select(p => p.MaPhong));

            // Chỉ giữ các đơn đặt phòng chưa hủy
            var datPhongHopLe = dsDatPhong
                .Where(dp => string.IsNullOrEmpty(dp.TrangThaiDatPhong) ||
                             !dp.TrangThaiDatPhong.Contains("Hủy", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var chiTietTheoDatPhong = dsChiTietDP
                .Where(c => !string.IsNullOrEmpty(c.DAT_PHONGMaDatPhong))
                .GroupBy(c => c.DAT_PHONGMaDatPhong!)
                .ToDictionary(g => g.Key, g => g.ToList());

            DateTime startDate = (TuNgay ?? DateTime.Today).Date;
            TongSoPhongKhaoSat = dsPhongLoc.Count;

            decimal giaGocTB = dsPhongLoc.Any()
                ? Math.Round(dsPhongLoc.Average(p => p.GiaTheoNgay ?? 0m), 0)
                : 0m;

            // =========================================================================
            // 1. TÍNH DỰ BÁO CÔNG SUẤT & ĐỊNH GIÁ ĐỘNG TỪNG NGÀY
            // =========================================================================
            DanhSachDuBao = new List<DailyForecastItem>();

            for (int i = 0; i < SoNgayDuBao; i++)
            {
                DateTime ngay = startDate.AddDays(i);
                bool laCuoiTuan = ngay.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday or DayOfWeek.Sunday;

                var phongDaDatTrongNgay = new HashSet<string>();
                int demBookingKhongGanPhong = 0;
                decimal doanhThuChotTrongNgay = 0m;

                foreach (var dp in datPhongHopLe)
                {
                    if (!IsBookingActiveOnDate(dp, ngay))
                        continue;

                    if (chiTietTheoDatPhong.TryGetValue(dp.MaDatPhong, out var chiTiets) && chiTiets.Any())
                    {
                        foreach (var ct in chiTiets)
                        {
                            if (string.IsNullOrEmpty(ct.PHONGMaPhong) || !maPhongLocSet.Contains(ct.PHONGMaPhong))
                                continue;

                            if (phongDaDatTrongNgay.Add(ct.PHONGMaPhong))
                            {
                                decimal donGiaDem = ct.DonGia ?? (phongMapAll.TryGetValue(ct.PHONGMaPhong, out var p) ? p.GiaTheoNgay ?? 0m : 0m);
                                doanhThuChotTrongNgay += donGiaDem;
                            }
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(LoaiPhongLoc))
                    {
                        // Dự phòng trường hợp đơn đặt phòng chưa gán mã phòng cụ thể
                        demBookingKhongGanPhong++;
                        doanhThuChotTrongNgay += giaGocTB;
                    }
                }

                int soPhongDaDat = Math.Min(TongSoPhongKhaoSat, phongDaDatTrongNgay.Count + demBookingKhongGanPhong);
                decimal tyLeLapDay = TongSoPhongKhaoSat > 0
                    ? Math.Round((decimal)soPhongDaDat * 100m / TongSoPhongKhaoSat, 1)
                    : 0m;

                var (phanTram, mucDo, khuyenNghi) = TinhDeXuatGiaDong(tyLeLapDay, laCuoiTuan);
                decimal giaGoiY = LamTronNghinDong(giaGocTB * (1m + phanTram / 100m));

                DanhSachDuBao.Add(new DailyForecastItem
                {
                    Ngay = ngay,
                    ThuTrongTuan = GetTenThu(ngay.DayOfWeek),
                    LaCuoiTuan = laCuoiTuan,
                    TongSoPhong = TongSoPhongKhaoSat,
                    SoPhongDaDat = soPhongDaDat,
                    TyLeLapDay = tyLeLapDay,
                    GiaGocTrungBinh = giaGocTB,
                    PhanTramDieuChinh = phanTram,
                    GiaGoiYDong = giaGoiY,
                    DoanhThuDaChot = doanhThuChotTrongNgay,
                    MucDo = mucDo,
                    KhuyenNghi = khuyenNghi
                });
            }

            // =========================================================================
            // 2. BẢNG ĐỊNH GIÁ ĐỘNG THEO TỪNG LOẠI PHÒNG (CHO PHÉP ÁP DỤNG XUỐNG DB)
            // =========================================================================
            BangGiaTheoLoaiPhong = new List<RoomTypePricingSuggestion>();
            var nhomLoaiPhong = dsPhongLoc
                .GroupBy(p => string.IsNullOrWhiteSpace(p.LoaiPhong) ? "Chưa phân loại" : p.LoaiPhong.Trim())
                .OrderBy(g => g.Key);

            foreach (var grp in nhomLoaiPhong)
            {
                var maPhongNhom = new HashSet<string>(grp.Select(p => p.MaPhong));
                int tongPhongNhom = grp.Count();
                decimal tongTyLe = 0m;

                for (int i = 0; i < SoNgayDuBao; i++)
                {
                    DateTime ngay = startDate.AddDays(i);
                    var daDatNhom = new HashSet<string>();

                    foreach (var dp in datPhongHopLe)
                    {
                        if (!IsBookingActiveOnDate(dp, ngay)) continue;
                        if (chiTietTheoDatPhong.TryGetValue(dp.MaDatPhong, out var chiTiets))
                        {
                            foreach (var ct in chiTiets)
                            {
                                if (!string.IsNullOrEmpty(ct.PHONGMaPhong) && maPhongNhom.Contains(ct.PHONGMaPhong))
                                    daDatNhom.Add(ct.PHONGMaPhong);
                            }
                        }
                    }

                    tongTyLe += tongPhongNhom > 0 ? (decimal)daDatNhom.Count * 100m / tongPhongNhom : 0m;
                }

                decimal congSuatTB = SoNgayDuBao > 0 ? Math.Round(tongTyLe / SoNgayDuBao, 1) : 0m;
                decimal giaHienTai = Math.Round(grp.Average(p => p.GiaTheoNgay ?? 0m), 0);
                var (phanTramGoiY, _, lyDo) = TinhDeXuatGiaDong(congSuatTB, false);
                decimal giaDeXuat = LamTronNghinDong(giaHienTai * (1m + phanTramGoiY / 100m));

                BangGiaTheoLoaiPhong.Add(new RoomTypePricingSuggestion
                {
                    LoaiPhong = grp.Key,
                    TongPhong = tongPhongNhom,
                    CongSuatTrungBinh = congSuatTB,
                    GiaHienTai = giaHienTai,
                    PhanTramGoiY = phanTramGoiY,
                    GiaDeXuat = giaDeXuat,
                    LyDo = lyDo
                });
            }

            // =========================================================================
            // 3. TÍNH KPI TỔNG QUAN
            // =========================================================================
            if (DanhSachDuBao.Any())
            {
                CongSuatTrungBinhKy = Math.Round(DanhSachDuBao.Average(x => x.TyLeLapDay), 1);
                SoNgayCaoDiem = DanhSachDuBao.Count(x => x.TyLeLapDay >= 70m);
                GiaGoiYTrungBinh = LamTronNghinDong(DanhSachDuBao.Average(x => x.GiaGoiYDong));
                TongDoanhThuDaChot = DanhSachDuBao.Sum(x => x.DoanhThuDaChot);
                TongDoanhThuTiemNang = DanhSachDuBao.Sum(x => x.DoanhThuTiemNang);
            }
        }

        private static bool IsBookingActiveOnDate(DAT_PHONG dp, DateTime ngay)
        {
            if (!dp.NgayNhan.HasValue)
                return false;

            DateTime nhan = dp.NgayNhan.Value.Date;
            DateTime tra = dp.NgayTra.HasValue ? dp.NgayTra.Value.Date : nhan.AddDays(1);

            if (tra <= nhan)
                return ngay == nhan;

            return ngay >= nhan && ngay < tra;
        }

        private static (int PhanTram, string MucDo, string KhuyenNghi) TinhDeXuatGiaDong(decimal tyLeLapDay, bool laCuoiTuan)
        {
            int phanTram;
            string mucDo;
            string khuyenNghi;

            if (tyLeLapDay >= 85m)
            {
                phanTram = 20;
                mucDo = "Cao điểm";
                khuyenNghi = "Sắp kín phòng — Tăng giá mạnh, hạn chế chiết khấu";
            }
            else if (tyLeLapDay >= 70m)
            {
                phanTram = 10;
                mucDo = "Nhu cầu cao";
                khuyenNghi = "Nhu cầu tốt — Nên điều chỉnh tăng giá bán";
            }
            else if (tyLeLapDay >= 40m)
            {
                phanTram = 0;
                mucDo = "Ổn định";
                khuyenNghi = "Công suất ổn định — Giữ nguyên giá niêm yết";
            }
            else
            {
                phanTram = -10;
                mucDo = "Thấp điểm";
                khuyenNghi = "Phòng trống nhiều — Tung ưu đãi / giảm giá kích cầu";
            }

            if (laCuoiTuan)
            {
                phanTram += 5;
                khuyenNghi += " (+5% cuối tuần)";
            }

            return (phanTram, mucDo, khuyenNghi);
        }

        private static decimal LamTronNghinDong(decimal gia)
        {
            if (gia <= 0) return 0m;
            return Math.Round(gia / 1000m, MidpointRounding.AwayFromZero) * 1000m;
        }

        private static string GetTenThu(DayOfWeek dow) => dow switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            _ => "Chủ nhật"
        };

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
    }
}