using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyBaoCaoThongKe
{
    public class DashboardBaoCaoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DashboardBaoCaoModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Cấu trúc lưu ngân sách & chi phí vận hành theo từng tháng (file ngansach_meta.json)
        public class MonthlyBudgetConfig
        {
            public string KyThang { get; set; } = string.Empty; // "yyyy-MM"
            public decimal MucTieuDoanhThu { get; set; } = 150000000m;
            public decimal NganSachChiPhi { get; set; } = 70000000m;
            public decimal ChiPhiNhanSuVanHanh { get; set; } = 0m;
            public decimal ChiPhiVatTuThucTe { get; set; } = 0m;
            public string GhiChu { get; set; } = string.Empty;
        }

        public class DailyChartPoint
        {
            public string NhanNgay { get; set; } = string.Empty;
            public decimal DoanhThu { get; set; }
            public decimal ThucThu { get; set; }
        }

        public class RoomTypeRevenueRow
        {
            public string LoaiPhong { get; set; } = string.Empty;
            public int SoPhong { get; set; }
            public int DemPhongBan { get; set; }
            public decimal CongSuat { get; set; }
            public decimal DoanhThu { get; set; }
        }

        // --- THAM SỐ LỌC KỲ THÁNG ---
        [BindProperty(SupportsGet = true)]
        public int Thang { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Nam { get; set; }

        // --- FORM LẬP NGÂN SÁCH ---
        [BindProperty]
        public MonthlyBudgetConfig NganSachHienTai { get; set; } = new();

        [TempData]
        public string? ThongBao { get; set; }

        // --- CHỈ SỐ KINH DOANH (KPI) ---
        public int TongSoPhong { get; set; }
        public int TongDemPhongKhaDung { get; set; }
        public int TongDemPhongDaBan { get; set; }
        public decimal TyLeLapDay { get; set; }       // Occupancy (%)
        public decimal ADR { get; set; }              // Giá phòng bình quân
        public decimal RevPAR { get; set; }           // Doanh thu / phòng khả dụng

        // --- DOANH THU & DÒNG TIỀN ---
        public decimal DoanhThuPhong { get; set; }
        public decimal DoanhThuDichVu { get; set; }
        public decimal DoanhThuNhaHang { get; set; }
        public decimal TongDoanhThu { get; set; }
        public decimal DongTienThucThu { get; set; }
        public decimal CongNoPhaiThu { get; set; }
        public decimal TongGiamGia { get; set; }

        // --- CHI PHÍ & LỢI NHUẬN ---
        public decimal ChiPhiHoaHongB2B { get; set; }
        public decimal GiaTriTonKhoHienTai { get; set; }
        public int SoVatTuCanhBaoKho { get; set; }
        public decimal TongChiPhiKy { get; set; }
        public decimal LoiNhuanUocTinh { get; set; }
        public decimal BienLoiNhuan { get; set; } // %

        // --- TIẾN ĐỘ NGÂN SÁCH ---
        public decimal TyLeDatDoanhThu { get; set; }  // %
        public decimal TyLeDungNganSach { get; set; } // %

        // --- DỮ LIỆU BIỂU ĐỒ & BẢNG PHÂN TÍCH ---
        public List<DailyChartPoint> BieuDoTheoNgay { get; set; } = new();
        public List<RoomTypeRevenueRow> PhanTichLoaiPhong { get; set; } = new();

        public async Task OnGetAsync()
        {
            ChuanHoaKyThang();
            await LoadDashboardAsync();
        }

        // Lưu cấu hình Ngân sách & Chi phí thực tế theo tháng
        public async Task<IActionResult> OnPostLuuNganSachAsync()
        {
            ChuanHoaKyThang();
            string kyKey = $"{Nam:D4}-{Thang:D2}";

            var allBudgets = await ReadBudgetsAsync();
            var existing = allBudgets.FirstOrDefault(x => x.KyThang == kyKey);

            if (existing == null)
            {
                existing = new MonthlyBudgetConfig { KyThang = kyKey };
                allBudgets.Add(existing);
            }

            existing.MucTieuDoanhThu = Math.Max(0m, NganSachHienTai.MucTieuDoanhThu);
            existing.NganSachChiPhi = Math.Max(0m, NganSachHienTai.NganSachChiPhi);
            existing.ChiPhiNhanSuVanHanh = Math.Max(0m, NganSachHienTai.ChiPhiNhanSuVanHanh);
            existing.ChiPhiVatTuThucTe = Math.Max(0m, NganSachHienTai.ChiPhiVatTuThucTe);
            existing.GhiChu = NganSachHienTai.GhiChu?.Trim() ?? "";

            await SaveBudgetsAsync(allBudgets);
            ThongBao = $"Đã lưu kế hoạch ngân sách & chi phí cho kỳ Tháng {Thang:D2}/{Nam}.";

            return RedirectToPage(new { Thang, Nam });
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            ChuanHoaKyThang();
            await LoadDashboardAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"BÁO CÁO TỔNG HỢP KINH DOANH & LỢI NHUẬN - THÁNG {Thang:D2}/{Nam}");
            sb.AppendLine("Chỉ tiêu,Giá trị");
            sb.AppendLine($"Tổng doanh thu (VNĐ),{TongDoanhThu}");
            sb.AppendLine($"Doanh thu phòng (VNĐ),{DoanhThuPhong}");
            sb.AppendLine($"Doanh thu dịch vụ (VNĐ),{DoanhThuDichVu}");
            sb.AppendLine($"Doanh thu nhà hàng (VNĐ),{DoanhThuNhaHang}");
            sb.AppendLine($"Dòng tiền thực thu (VNĐ),{DongTienThucThu}");
            sb.AppendLine($"Công nợ phải thu (VNĐ),{CongNoPhaiThu}");
            sb.AppendLine($"Công suất phòng (%),{TyLeLapDay}");
            sb.AppendLine($"ADR - Giá phòng bình quân (VNĐ),{ADR}");
            sb.AppendLine($"RevPAR (VNĐ),{RevPAR}");
            sb.AppendLine($"Chi phí hoa hồng B2B (VNĐ),{ChiPhiHoaHongB2B}");
            sb.AppendLine($"Chi phí nhân sự & vận hành (VNĐ),{NganSachHienTai.ChiPhiNhanSuVanHanh}");
            sb.AppendLine($"Chi phí vật tư thực tế (VNĐ),{NganSachHienTai.ChiPhiVatTuThucTe}");
            sb.AppendLine($"Tổng chi phí kỳ (VNĐ),{TongChiPhiKy}");
            sb.AppendLine($"Lợi nhuận ước tính (VNĐ),{LoiNhuanUocTinh}");
            sb.AppendLine($"Biên lợi nhuận (%),{BienLoiNhuan}");
            sb.AppendLine($"Mục tiêu doanh thu tháng (VNĐ),{NganSachHienTai.MucTieuDoanhThu}");
            sb.AppendLine($"Tỷ lệ đạt mục tiêu doanh thu (%),{TyLeDatDoanhThu}");

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"DashboardKinhDoanh_{Nam}_{Thang:D2}.csv");
        }

        private void ChuanHoaKyThang()
        {
            if (Thang < 1 || Thang > 12) Thang = DateTime.Today.Month;
            if (Nam < 2020 || Nam > 2100) Nam = DateTime.Today.Year;
        }

        private async Task LoadDashboardAsync()
        {
            int soNgayTrongThang = DateTime.DaysInMonth(Nam, Thang);
            DateTime tuNgay = new DateTime(Nam, Thang, 1, 0, 0, 0);
            DateTime denNgay = new DateTime(Nam, Thang, soNgayTrongThang, 23, 59, 59);
            string kyKey = $"{Nam:D4}-{Thang:D2}";

            // Đọc cấu hình ngân sách tháng từ file JSON
            var allBudgets = await ReadBudgetsAsync();
            NganSachHienTai = allBudgets.FirstOrDefault(x => x.KyThang == kyKey)
                ?? new MonthlyBudgetConfig { KyThang = kyKey };

            // Tải dữ liệu thật từ SQL Server
            var dsPhong = await _context.PHONG.AsNoTracking().ToListAsync();
            var dsDatPhong = await _context.DAT_PHONG.AsNoTracking().ToListAsync();
            var dsChiTietDP = await _context.CHI_TIET_DAT_PHONG.AsNoTracking().ToListAsync();
            var dsChiTietDV = await _context.CHI_TIET_DICH_VU.AsNoTracking().ToListAsync();
            var dsHoaDon = await _context.HOA_DON.AsNoTracking().ToListAsync();
            var dsHoaDonNH = await _context.HOA_DON_NHA_HANG.AsNoTracking().ToListAsync();
            var dsThanhToan = await _context.THANH_TOAN.AsNoTracking().ToListAsync();
            var dsDoiTac = await _context.DOI_TAC.AsNoTracking().ToListAsync();
            var dsKho = await _context.KHO_VAT_TU.AsNoTracking().ToListAsync();

            var phongMap = dsPhong.ToDictionary(p => p.MaPhong);
            var doiTacMap = dsDoiTac.ToDictionary(d => d.MaDoiTac, StringComparer.OrdinalIgnoreCase);

            TongSoPhong = dsPhong.Count;
            TongDemPhongKhaDung = TongSoPhong * soNgayTrongThang;

            // Tồn kho tham chiếu
            GiaTriTonKhoHienTai = dsKho.Sum(v => (v.SoLuongTon ?? 0) * (v.DonGiaNhap ?? 0m));
            SoVatTuCanhBaoKho = dsKho.Count(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 10));

            // Khởi tạo mảng biểu đồ theo từng ngày trong tháng
            var doanhThuNgay = new decimal[soNgayTrongThang + 1];
            var thucThuNgay = new decimal[soNgayTrongThang + 1];

            // Thống kê theo loại phòng
            var demPhongTheoLoai = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var doanhThuTheoLoai = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            var maDatPhongDaGopNH = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // =========================================================================
            // 1. TỔNG HỢP PHÂN HỆ LỄ TÂN (PHÒNG + DỊCH VỤ + HOA HỒNG B2B)
            // =========================================================================
            foreach (var dp in dsDatPhong)
            {
                if (!string.IsNullOrEmpty(dp.TrangThaiDatPhong) &&
                    dp.TrangThaiDatPhong.Contains("Hủy", StringComparison.OrdinalIgnoreCase))
                    continue;

                var hoaDon = dsHoaDon
                    .Where(h => h.DAT_PHONGMaDatPhong == dp.MaDatPhong)
                    .OrderByDescending(h => h.NgayLap)
                    .FirstOrDefault();

                var chiTiets = dsChiTietDP.Where(c => c.DAT_PHONGMaDatPhong == dp.MaDatPhong).ToList();

                // Tính số đêm phòng thực sự rơi vào tháng đang xem (cho Occupancy)
                DateTime nhan = (dp.NgayNhan ?? dp.NgayDat ?? DateTime.MinValue).Date;
                DateTime tra = (dp.NgayTra ?? nhan.AddDays(1)).Date;
                if (tra <= nhan) tra = nhan.AddDays(1);

                DateTime overlapStart = nhan > tuNgay.Date ? nhan : tuNgay.Date;
                DateTime overlapEnd = tra < denNgay.Date.AddDays(1) ? tra : denNgay.Date.AddDays(1);
                int soDemTrongThang = overlapEnd > overlapStart ? (overlapEnd - overlapStart).Days : 0;

                if (soDemTrongThang > 0)
                {
                    int soPhongCuaDon = Math.Max(1, chiTiets.Count);
                    TongDemPhongDaBan += soDemTrongThang * soPhongCuaDon;

                    foreach (var ct in chiTiets)
                    {
                        if (!string.IsNullOrEmpty(ct.PHONGMaPhong) && phongMap.TryGetValue(ct.PHONGMaPhong, out var pObj))
                        {
                            string loai = string.IsNullOrWhiteSpace(pObj.LoaiPhong) ? "Chưa phân loại" : pObj.LoaiPhong.Trim();
                            demPhongTheoLoai[loai] = demPhongTheoLoai.GetValueOrDefault(loai) + soDemTrongThang;
                        }
                    }
                }

                // Kiểm tra ngày ghi nhận doanh thu có nằm trong tháng đang xem không
                DateTime? ngayGhiNhan = hoaDon?.NgayLap ?? dp.NgayTra ?? dp.NgayDat;
                if (!ngayGhiNhan.HasValue || ngayGhiNhan.Value < tuNgay || ngayGhiNhan.Value > denNgay)
                    continue;

                bool daCheckOutHoacTT =
                    string.Equals(dp.TrangThaiDatPhong, "Đã check-out", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dp.TrangThaiDatPhong, "Đã trả phòng", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dp.TrangThaiDatPhong, "Đã hoàn tất", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dp.TrangThaiDatPhong, "Hoàn tất", StringComparison.OrdinalIgnoreCase) ||
                    (hoaDon != null && string.Equals(hoaDon.TrangThaiThanhToan, "Đã thanh toán", StringComparison.OrdinalIgnoreCase));

                decimal tienCoc = dp.TienCoc ?? 0m;
                if (!daCheckOutHoacTT && hoaDon == null && tienCoc <= 0)
                    continue;

                if (hoaDon != null && (hoaDon.TienNhaHang ?? 0m) > 0)
                    maDatPhongDaGopNH.Add(dp.MaDatPhong);

                decimal tienPhongTamTinh = 0m;
                var maPhongList = new List<string>();
                foreach (var ct in chiTiets)
                {
                    PHONG? p = (!string.IsNullOrEmpty(ct.PHONGMaPhong) && phongMap.TryGetValue(ct.PHONGMaPhong, out var pFound))
                        ? pFound
                        : null;

                    if (p != null) maPhongList.Add(p.MaPhong);

                    int soNgay = ct.SoNgayO > 0 ? ct.SoNgayO : Math.Max(1, (tra - nhan).Days);
                    decimal donGia = ct.DonGia ?? p?.GiaTheoNgay ?? 0m;
                    tienPhongTamTinh += donGia * soNgay;
                }

                DateTime mocBatDau = (dp.NgayNhan ?? dp.NgayDat ?? DateTime.MinValue).AddHours(-6);
                DateTime mocKetThuc = (hoaDon?.NgayLap ?? dp.NgayTra ?? DateTime.Now).AddHours(6);

                decimal tienDvPhatSinh = dsChiTietDV
                    .Where(dv => !string.IsNullOrEmpty(dv.PHONGMaPhong)
                              && maPhongList.Contains(dv.PHONGMaPhong)
                              && (!dv.ThoiGianSuDung.HasValue || (dv.ThoiGianSuDung.Value >= mocBatDau && dv.ThoiGianSuDung.Value <= mocKetThuc)))
                    .Sum(dv => dv.ThanhTien ?? ((dv.DonGia ?? 0m) * (dv.SoLuong ?? 0)));

                decimal tienPhong = hoaDon?.TienPhong ?? tienPhongTamTinh;
                decimal tienDV = hoaDon?.TienDichVu ?? tienDvPhatSinh;
                decimal tienNHGop = hoaDon?.TienNhaHang ?? 0m;
                decimal giamGia = hoaDon?.TienGiamGia ?? 0m;
                decimal thueVat = hoaDon?.TienThueVAT ?? Math.Round((tienPhong + tienDV + tienNHGop) * 0.10m, 0);

                decimal tongDon = hoaDon?.TongTienThanhToan
                    ?? Math.Max(0m, tienPhong + tienDV + tienNHGop + thueVat - giamGia);

                decimal daThuGiaoDich = hoaDon != null
                    ? dsThanhToan
                        .Where(t => t.HOA_DONMaHoaDon == hoaDon.MaHoaDon && t.TrangThaiGiaoDich != "Thất bại")
                        .Sum(t => t.SoTien ?? 0m)
                    : 0m;

                decimal tienDaThu = daThuGiaoDich > 0
                    ? daThuGiaoDich
                    : (daCheckOutHoacTT ? tongDon : tienCoc);

                DoanhThuPhong += tienPhong;
                DoanhThuDichVu += tienDV;
                DoanhThuNhaHang += tienNHGop;
                TongGiamGia += giamGia;
                TongDoanhThu += tongDon;
                DongTienThucThu += tienDaThu;

                int dayIndex = ngayGhiNhan.Value.Day;
                if (dayIndex >= 1 && dayIndex <= soNgayTrongThang)
                {
                    doanhThuNgay[dayIndex] += tongDon;
                    thucThuNgay[dayIndex] += tienDaThu;
                }

                // Phân bổ doanh thu phòng theo loại phòng
                if (chiTiets.Any())
                {
                    decimal chiaDeu = tienPhong / chiTiets.Count;
                    foreach (var ct in chiTiets)
                    {
                        if (!string.IsNullOrEmpty(ct.PHONGMaPhong) && phongMap.TryGetValue(ct.PHONGMaPhong, out var pObj))
                        {
                            string loai = string.IsNullOrWhiteSpace(pObj.LoaiPhong) ? "Chưa phân loại" : pObj.LoaiPhong.Trim();
                            doanhThuTheoLoai[loai] = doanhThuTheoLoai.GetValueOrDefault(loai) + chiaDeu;
                        }
                    }
                }

                // Tính chi phí hoa hồng đối tác B2B nếu đơn đến từ đối tác
                DOI_TAC? doiTac = null;
                if (!string.IsNullOrWhiteSpace(dp.DOI_TACMaDoiTac) && doiTacMap.TryGetValue(dp.DOI_TACMaDoiTac.Trim(), out var dtFound))
                {
                    doiTac = dtFound;
                }
                else if (!string.IsNullOrWhiteSpace(dp.KenhDatPhong))
                {
                    doiTac = dsDoiTac.FirstOrDefault(d =>
                        !string.IsNullOrWhiteSpace(d.TenDoiTac) &&
                        string.Equals(d.TenDoiTac.Trim(), dp.KenhDatPhong.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (doiTac != null && doiTac.ChietKhau.HasValue && doiTac.ChietKhau.Value > 0)
                {
                    decimal tyLe = doiTac.ChietKhau.Value < 1
                        ? (decimal)doiTac.ChietKhau.Value
                        : (decimal)(doiTac.ChietKhau.Value / 100.0);
                    ChiPhiHoaHongB2B += Math.Round(tongDon * tyLe, 0);
                }
            }

            // =========================================================================
            // 2. TỔNG HỢP PHÂN HỆ NHÀ HÀNG (HÓA ĐƠN THANH TOÁN ĐỘC LẬP)
            // =========================================================================
            var dsHdNHHopLe = dsHoaDonNH.Where(h =>
                !string.IsNullOrWhiteSpace(h.HinhThucThanhToan) &&
                !h.HinhThucThanhToan.Contains("Chưa", StringComparison.OrdinalIgnoreCase) &&
                !h.HinhThucThanhToan.Contains("Hủy", StringComparison.OrdinalIgnoreCase) &&
                !h.HinhThucThanhToan.Contains("Ghi nợ", StringComparison.OrdinalIgnoreCase));

            foreach (var hd in dsHdNHHopLe)
            {
                if (!string.IsNullOrEmpty(hd.DAT_PHONGMaDatPhong) && maDatPhongDaGopNH.Contains(hd.DAT_PHONGMaDatPhong))
                    continue;

                var gd = dsThanhToan
                    .Where(t => t.HOA_DON_NHA_HANGMaHoaDonNhaHang == hd.MaHoaDonNhaHang && t.TrangThaiGiaoDich != "Thất bại")
                    .OrderByDescending(t => t.ThoiGianGiaoDich)
                    .FirstOrDefault();

                DateTime? ngayTT = gd?.ThoiGianGiaoDich ?? hd.ThoiGianTao;
                if (!ngayTT.HasValue || ngayTT.Value < tuNgay || ngayTT.Value > denNgay)
                    continue;

                decimal tamTinh = TinhTamTinhTuJson(hd.DanhSachMonJson, hd.TongTienMonAn ?? 0m);
                decimal giamGia = hd.GiamGia ?? 0m;
                decimal tongNH = (hd.ThanhTien.HasValue && hd.ThanhTien.Value > 0)
                    ? hd.ThanhTien.Value
                    : Math.Max(0m, tamTinh + Math.Round(tamTinh * 0.15m, 0) - giamGia);

                decimal daThu = (gd?.SoTien ?? 0m) > 0 ? gd!.SoTien!.Value : tongNH;

                DoanhThuNhaHang += tongNH;
                TongGiamGia += giamGia;
                TongDoanhThu += tongNH;
                DongTienThucThu += daThu;

                int dayIndex = ngayTT.Value.Day;
                if (dayIndex >= 1 && dayIndex <= soNgayTrongThang)
                {
                    doanhThuNgay[dayIndex] += tongNH;
                    thucThuNgay[dayIndex] += daThu;
                }
            }

            // =========================================================================
            // 3. TÍNH CÁC CHỈ SỐ KPI KHÁCH SẠN & LỢI NHUẬN / NGÂN SÁCH
            // =========================================================================
            CongNoPhaiThu = Math.Max(0m, TongDoanhThu - DongTienThucThu);

            TyLeLapDay = TongDemPhongKhaDung > 0
                ? Math.Min(100m, Math.Round((decimal)TongDemPhongDaBan * 100m / TongDemPhongKhaDung, 1))
                : 0m;

            ADR = TongDemPhongDaBan > 0
                ? Math.Round(DoanhThuPhong / TongDemPhongDaBan, 0)
                : 0m;

            RevPAR = TongDemPhongKhaDung > 0
                ? Math.Round(DoanhThuPhong / TongDemPhongKhaDung, 0)
                : 0m;

            TongChiPhiKy = ChiPhiHoaHongB2B
                         + NganSachHienTai.ChiPhiNhanSuVanHanh
                         + NganSachHienTai.ChiPhiVatTuThucTe;

            LoiNhuanUocTinh = TongDoanhThu - TongChiPhiKy;

            BienLoiNhuan = TongDoanhThu > 0
                ? Math.Round(LoiNhuanUocTinh * 100m / TongDoanhThu, 1)
                : 0m;

            TyLeDatDoanhThu = NganSachHienTai.MucTieuDoanhThu > 0
                ? Math.Round(TongDoanhThu * 100m / NganSachHienTai.MucTieuDoanhThu, 1)
                : 0m;

            TyLeDungNganSach = NganSachHienTai.NganSachChiPhi > 0
                ? Math.Round(TongChiPhiKy * 100m / NganSachHienTai.NganSachChiPhi, 1)
                : 0m;

            // Dữ liệu biểu đồ theo ngày
            BieuDoTheoNgay = new List<DailyChartPoint>();
            for (int d = 1; d <= soNgayTrongThang; d++)
            {
                BieuDoTheoNgay.Add(new DailyChartPoint
                {
                    NhanNgay = $"{d:D2}/{Thang:D2}",
                    DoanhThu = doanhThuNgay[d],
                    ThucThu = thucThuNgay[d]
                });
            }

            // Bảng phân tích theo từng loại phòng
            PhanTichLoaiPhong = dsPhong
                .GroupBy(p => string.IsNullOrWhiteSpace(p.LoaiPhong) ? "Chưa phân loại" : p.LoaiPhong.Trim())
                .Select(g =>
                {
                    int soPhongLoai = g.Count();
                    int demBan = demPhongTheoLoai.GetValueOrDefault(g.Key);
                    int demKhaDung = soPhongLoai * soNgayTrongThang;
                    return new RoomTypeRevenueRow
                    {
                        LoaiPhong = g.Key,
                        SoPhong = soPhongLoai,
                        DemPhongBan = demBan,
                        CongSuat = demKhaDung > 0 ? Math.Min(100m, Math.Round((decimal)demBan * 100m / demKhaDung, 1)) : 0m,
                        DoanhThu = Math.Round(doanhThuTheoLoai.GetValueOrDefault(g.Key), 0)
                    };
                })
                .OrderByDescending(x => x.DoanhThu)
                .ToList();
        }

        private string BudgetFilePath => Path.Combine(_env.ContentRootPath, "ngansach_meta.json");

        private async Task<List<MonthlyBudgetConfig>> ReadBudgetsAsync()
        {
            if (!System.IO.File.Exists(BudgetFilePath))
                return new List<MonthlyBudgetConfig>();

            try
            {
                string json = await System.IO.File.ReadAllTextAsync(BudgetFilePath);
                return JsonSerializer.Deserialize<List<MonthlyBudgetConfig>>(json) ?? new List<MonthlyBudgetConfig>();
            }
            catch
            {
                return new List<MonthlyBudgetConfig>();
            }
        }

        private async Task SaveBudgetsAsync(List<MonthlyBudgetConfig> list)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(list, opts);
            await System.IO.File.WriteAllTextAsync(BudgetFilePath, json, Encoding.UTF8);
        }

        private static decimal TinhTamTinhTuJson(string? jsonMonAn, decimal fallback)
        {
            if (string.IsNullOrWhiteSpace(jsonMonAn) || jsonMonAn == "[]")
                return fallback;

            try
            {
                decimal sum = 0m;
                using var doc = JsonDocument.Parse(jsonMonAn);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    int soLuong = 1;
                    if (el.TryGetProperty("soLuong", out var s1) || el.TryGetProperty("SoLuong", out s1))
                    {
                        if (s1.ValueKind == JsonValueKind.Number && s1.TryGetInt32(out int slNum)) soLuong = slNum;
                        else if (s1.ValueKind == JsonValueKind.String && int.TryParse(s1.GetString(), out int slStr)) soLuong = slStr;
                    }

                    decimal donGia = 0m;
                    if (el.TryGetProperty("donGia", out var d1) || el.TryGetProperty("DonGia", out d1))
                    {
                        if (d1.ValueKind == JsonValueKind.Number && d1.TryGetDecimal(out decimal dgNum)) donGia = dgNum;
                        else if (d1.ValueKind == JsonValueKind.String && decimal.TryParse(d1.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dgStr)) donGia = dgStr;
                    }

                    sum += soLuong * donGia;
                }
                return sum > 0 ? sum : fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }
}