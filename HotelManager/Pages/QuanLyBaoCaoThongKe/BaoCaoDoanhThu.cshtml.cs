using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyBaoCaoThongKe
{
    public class BaoCaoDoanhThuModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoDoanhThuModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ViewModel hiển thị từng dòng doanh thu thật từ các phân hệ
        public class RevenueReportItem
        {
            public string MaChungTu { get; set; } = string.Empty;
            public string PhanHe { get; set; } = string.Empty;
            public string LoaiPhong { get; set; } = string.Empty;
            public string TenPhong { get; set; } = string.Empty;
            public string TenKhach { get; set; } = string.Empty;
            public DateTime? NgayDen { get; set; }
            public DateTime? NgayDi { get; set; }
            public decimal TienPhong { get; set; }
            public decimal DichVu { get; set; }
            public decimal ChietKhau { get; set; }
            public decimal TongDoanhThu { get; set; }
            public decimal NoTruoc { get; set; }
            public decimal TienDaThu { get; set; }
            public string TrangThai { get; set; } = string.Empty;
        }

        // --- THAM SỐ LỌC TỪ GIAO DIỆN ---
        [BindProperty(SupportsGet = true)]
        public bool BaoGomThuePhi { get; set; } = true;

        [BindProperty(SupportsGet = true)]
        public string CheDoGhiNhan { get; set; } = "NgayTraPhong"; // "NgayTraPhong" hoặc "MoiNgay"

        [BindProperty(SupportsGet = true)]
        public DateTime? TuNgay { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DenNgay { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? LoaiPhongLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PhanHeLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string NhomTheo { get; set; } = "PhanHe"; // "PhanHe", "LoaiPhong", "KhongNhom"

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "desc";

        // --- DỮ LIỆU ĐỔ RA VIEW ---
        public List<RevenueReportItem> DanhSachBaoCao { get; set; } = new();
        public List<IGrouping<string, RevenueReportItem>> DuLieuTheoNhom { get; set; } = new();

        public List<string> DanhSachLoaiPhong { get; set; } = new();
        public List<string> DanhSachPhanHe { get; set; } = new();
        public List<string> DanhSachTrangThai { get; set; } = new();

        // --- TỔNG CỘNG CUỐI BẢNG ---
        public decimal TongTienPhong { get; set; }
        public decimal TongDichVu { get; set; }
        public decimal TongChietKhau { get; set; }
        public decimal TongCongDoanhThu { get; set; }
        public decimal TongNoTruoc { get; set; }
        public decimal TongTienDaThu { get; set; }

        public async Task OnGetAsync()
        {
            TuNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0);
            DenNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month), 23, 59, 0);

            await LoadRealDataAsync();
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            TuNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0);
            DenNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month), 23, 59, 0);

            await LoadRealDataAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Mã chứng từ,Phân hệ,Loại phòng / Khu vực,Phòng / Bàn,Họ tên khách hàng,Ngày nhận / Tạo,Ngày trả / Thanh toán,Trạng thái,Tiền phòng,Dịch vụ / Món ăn,Giảm giá,Tổng doanh thu,Còn nợ,Tiền đã thu");

            foreach (var item in DanhSachBaoCao)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(item.MaChungTu),
                    EscapeCsv(item.PhanHe),
                    EscapeCsv(item.LoaiPhong),
                    EscapeCsv(item.TenPhong),
                    EscapeCsv(item.TenKhach),
                    item.NgayDen?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    item.NgayDi?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    EscapeCsv(item.TrangThai),
                    item.TienPhong,
                    item.DichVu,
                    item.ChietKhau,
                    item.TongDoanhThu,
                    item.NoTruoc,
                    item.TienDaThu
                ));
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private async Task LoadRealDataAsync()
        {
            var allItems = new List<RevenueReportItem>();

            // Tải các bảng liên quan từ SQL Server
            var dsDatPhong = await _context.DAT_PHONG.AsNoTracking().ToListAsync();
            var dsChiTietDP = await _context.CHI_TIET_DAT_PHONG.AsNoTracking().ToListAsync();
            var dsPhong = await _context.PHONG.AsNoTracking().ToDictionaryAsync(p => p.MaPhong);
            var dsKhachHang = await _context.KHACH_HANG.AsNoTracking().ToDictionaryAsync(k => k.MaKH);
            var dsChiTietDV = await _context.CHI_TIET_DICH_VU.AsNoTracking().ToListAsync();
            var dsHoaDonPhong = await _context.HOA_DON.AsNoTracking().ToListAsync();
            var dsHoaDonNhaHang = await _context.HOA_DON_NHA_HANG.AsNoTracking().ToListAsync();
            var dsBanNhaHang = await _context.BAN_NHA_HANG.AsNoTracking().ToDictionaryAsync(b => b.MaBan);
            var dsThanhToan = await _context.THANH_TOAN.AsNoTracking().ToListAsync();

            // Lưu các mã đặt phòng đã tính tiền nhà hàng gộp trong HOA_DON để tránh đếm trùng
            var maDatPhongDaGopNhaHang = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // =========================================================================
            // 1. PHÂN HỆ LỄ TÂN (ĐẶT PHÒNG / LƯU TRÚ / CHECK-OUT / THANH TOÁN PHÒNG)
            // =========================================================================
            foreach (var dp in dsDatPhong)
            {
                if (!string.IsNullOrEmpty(dp.TrangThaiDatPhong) &&
                    dp.TrangThaiDatPhong.Contains("Hủy", StringComparison.OrdinalIgnoreCase))
                    continue;

                var hoaDon = dsHoaDonPhong
                    .Where(h => h.DAT_PHONGMaDatPhong == dp.MaDatPhong)
                    .OrderByDescending(h => h.NgayLap)
                    .FirstOrDefault();

                if (hoaDon != null && (hoaDon.TienNhaHang ?? 0m) > 0 && !string.IsNullOrEmpty(dp.MaDatPhong))
                {
                    maDatPhongDaGopNhaHang.Add(dp.MaDatPhong);
                }

                bool daCheckOutHoacThanhToan =
                    string.Equals(dp.TrangThaiDatPhong, "Đã check-out", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dp.TrangThaiDatPhong, "Đã trả phòng", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dp.TrangThaiDatPhong, "Đã hoàn tất", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dp.TrangThaiDatPhong, "Hoàn tất", StringComparison.OrdinalIgnoreCase) ||
                    (hoaDon != null && string.Equals(hoaDon.TrangThaiThanhToan, "Đã thanh toán", StringComparison.OrdinalIgnoreCase));

                decimal tienCoc = dp.TienCoc ?? 0m;

                // Chỉ đưa vào báo cáo khi đơn đã Check-out / có Hóa đơn / đã có tiền cọc
                if (!daCheckOutHoacThanhToan && hoaDon == null && tienCoc <= 0)
                    continue;

                var chiTiets = dsChiTietDP.Where(c => c.DAT_PHONGMaDatPhong == dp.MaDatPhong).ToList();
                var maPhongList = chiTiets
                    .Where(c => !string.IsNullOrEmpty(c.PHONGMaPhong))
                    .Select(c => c.PHONGMaPhong!)
                    .Distinct()
                    .ToList();

                var tenPhongList = new List<string>();
                var loaiPhongList = new List<string>();
                decimal tienPhongTamTinh = 0m;

                foreach (var ct in chiTiets)
                {
                    PHONG? p = (!string.IsNullOrEmpty(ct.PHONGMaPhong) && dsPhong.TryGetValue(ct.PHONGMaPhong, out var phongFound))
                        ? phongFound
                        : null;

                    if (p != null)
                    {
                        if (!string.IsNullOrWhiteSpace(p.SoPhong))
                        {
                            string tenP = p.SoPhong.Trim();
                            if (!tenP.StartsWith("Phòng", StringComparison.OrdinalIgnoreCase) &&
                                !tenP.StartsWith("P.", StringComparison.OrdinalIgnoreCase))
                            {
                                tenP = "Phòng " + tenP;
                            }
                            tenPhongList.Add(tenP);
                        }

                        if (!string.IsNullOrWhiteSpace(p.LoaiPhong))
                            loaiPhongList.Add(p.LoaiPhong.Trim());
                    }

                    int soNgay = ct.SoNgayO > 0
                        ? ct.SoNgayO
                        : Math.Max(1, ((dp.NgayTra ?? DateTime.Now).Date - (dp.NgayNhan ?? DateTime.Now).Date).Days);

                    decimal donGia = ct.DonGia ?? p?.GiaTheoNgay ?? 0m;
                    tienPhongTamTinh += donGia * soNgay;
                }

                // Tính tổng dịch vụ / minibar phát sinh trong thời gian lưu trú
                DateTime mocBatDau = (dp.NgayNhan ?? dp.NgayDat ?? DateTime.MinValue).AddHours(-6);
                DateTime mocKetThuc = (hoaDon?.NgayLap ?? dp.NgayTra ?? DateTime.Now).AddHours(6);

                decimal tienDichVuPhatSinh = dsChiTietDV
                    .Where(dv => !string.IsNullOrEmpty(dv.PHONGMaPhong)
                              && maPhongList.Contains(dv.PHONGMaPhong)
                              && (!dv.ThoiGianSuDung.HasValue || (dv.ThoiGianSuDung.Value >= mocBatDau && dv.ThoiGianSuDung.Value <= mocKetThuc)))
                    .Sum(dv => dv.ThanhTien ?? ((dv.DonGia ?? 0m) * (dv.SoLuong ?? 0)));

                decimal tienPhongGoc = hoaDon?.TienPhong ?? tienPhongTamTinh;
                decimal tienDichVuGoc = hoaDon != null
                    ? (hoaDon.TienDichVu ?? 0m) + (hoaDon.TienNhaHang ?? 0m)
                    : tienDichVuPhatSinh;
                decimal chietKhau = hoaDon?.TienGiamGia ?? 0m;
                decimal thueVat = hoaDon?.TienThueVAT ?? Math.Round((tienPhongGoc + tienDichVuGoc) * 0.10m, 0);

                decimal tongPhaiThuCoThue = hoaDon?.TongTienThanhToan
                    ?? Math.Max(0m, tienPhongGoc + tienDichVuGoc + thueVat - chietKhau);

                // Phân bổ thuế VAT nhất quán theo lựa chọn "Bao gồm thuế phí"
                decimal tienPhongHienThi = tienPhongGoc;
                decimal tienDichVuHienThi = tienDichVuGoc;
                decimal tongDoanhThu;

                if (BaoGomThuePhi)
                {
                    decimal tongGoc = tienPhongGoc + tienDichVuGoc;
                    decimal phanThueCanCong = Math.Max(0m, (tongPhaiThuCoThue + chietKhau) - tongGoc);

                    if (tongGoc > 0 && phanThueCanCong > 0)
                    {
                        decimal thuePhong = Math.Round(phanThueCanCong * (tienPhongGoc / tongGoc), 0);
                        decimal thueDichVu = phanThueCanCong - thuePhong;
                        tienPhongHienThi = tienPhongGoc + thuePhong;
                        tienDichVuHienThi = tienDichVuGoc + thueDichVu;
                    }

                    tongDoanhThu = tongPhaiThuCoThue;
                }
                else
                {
                    tongDoanhThu = Math.Max(0m, tienPhongGoc + tienDichVuGoc - chietKhau);
                }

                // Tiền đã thu từ bảng THANH_TOAN hoặc hóa đơn đã thanh toán / tiền cọc
                decimal daThuTuGiaoDich = hoaDon != null
                    ? dsThanhToan
                        .Where(t => t.HOA_DONMaHoaDon == hoaDon.MaHoaDon && t.TrangThaiGiaoDich != "Thất bại")
                        .Sum(t => t.SoTien ?? 0m)
                    : 0m;

                decimal tienDaThu;
                if (daThuTuGiaoDich > 0)
                    tienDaThu = daThuTuGiaoDich;
                else if (daCheckOutHoacThanhToan)
                    tienDaThu = tongPhaiThuCoThue;
                else
                    tienDaThu = tienCoc;

                decimal conNo = Math.Max(0m, tongPhaiThuCoThue - tienDaThu);

                string tenKhach = "Khách lẻ";
                if (!string.IsNullOrEmpty(dp.KHACH_HANGMaKH) &&
                    dsKhachHang.TryGetValue(dp.KHACH_HANGMaKH, out var khachObj) &&
                    !string.IsNullOrWhiteSpace(khachObj.HoTen))
                {
                    tenKhach = khachObj.HoTen.Trim();
                }

                string trangThaiHienThi = (daCheckOutHoacThanhToan && conNo == 0)
                    ? "Đã thanh toán"
                    : (daCheckOutHoacThanhToan && conNo > 0)
                        ? "Thanh toán một phần"
                        : (string.IsNullOrWhiteSpace(dp.TrangThaiDatPhong) ? "Đã đặt cọc" : dp.TrangThaiDatPhong);

                allItems.Add(new RevenueReportItem
                {
                    MaChungTu = hoaDon?.MaHoaDon ?? dp.MaDatPhong,
                    PhanHe = "Lễ tân",
                    LoaiPhong = loaiPhongList.Any() ? string.Join(", ", loaiPhongList.Distinct()) : "Phòng lưu trú",
                    TenPhong = tenPhongList.Any() ? string.Join(", ", tenPhongList.Distinct()) : "Chưa gán phòng",
                    TenKhach = tenKhach,
                    NgayDen = dp.NgayNhan ?? dp.NgayDat,
                    NgayDi = hoaDon?.NgayLap ?? dp.NgayTra ?? dp.NgayDat,
                    TienPhong = tienPhongHienThi,
                    DichVu = tienDichVuHienThi,
                    ChietKhau = chietKhau,
                    TongDoanhThu = tongDoanhThu,
                    NoTruoc = conNo,
                    TienDaThu = tienDaThu,
                    TrangThai = trangThaiHienThi
                });
            }

            // =========================================================================
            // 2. PHÂN HỆ NHÀ HÀNG (HÓA ĐƠN ĐỒ ĂN ĐÃ THANH TOÁN TẠI QUẦY THU NGÂN)
            // =========================================================================
            var dsHdNhaHangDaThanhToan = dsHoaDonNhaHang
                .Where(h =>
                    !string.IsNullOrWhiteSpace(h.HinhThucThanhToan) &&
                    !h.HinhThucThanhToan.Contains("Chưa", StringComparison.OrdinalIgnoreCase) &&
                    !h.HinhThucThanhToan.Contains("Hủy", StringComparison.OrdinalIgnoreCase) &&
                    !h.HinhThucThanhToan.Contains("Ghi nợ", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var hd in dsHdNhaHangDaThanhToan)
            {
                // Bỏ qua nếu hóa đơn này đã được gộp vào HOA_DON của phòng để không bị đếm trùng
                if (!string.IsNullOrEmpty(hd.DAT_PHONGMaDatPhong) &&
                    maDatPhongDaGopNhaHang.Contains(hd.DAT_PHONGMaDatPhong))
                    continue;

                var maBanList = (hd.BAN_NHA_HANGMaBan ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                var tenBanList = new List<string>();
                string tenKhachBan = string.Empty;

                foreach (var mb in maBanList)
                {
                    if (dsBanNhaHang.TryGetValue(mb, out var ban))
                    {
                        tenBanList.Add(!string.IsNullOrWhiteSpace(ban.TenBan) ? ban.TenBan.Trim() : mb);
                        if (string.IsNullOrWhiteSpace(tenKhachBan) && !string.IsNullOrWhiteSpace(ban.TenKhachDat))
                            tenKhachBan = ban.TenKhachDat.Trim();
                    }
                    else
                    {
                        tenBanList.Add(mb);
                    }
                }

                // Nếu hóa đơn nhà hàng có gắn mã đặt phòng thì ưu tiên tên khách đặt phòng
                if (string.IsNullOrWhiteSpace(tenKhachBan) && !string.IsNullOrEmpty(hd.DAT_PHONGMaDatPhong))
                {
                    var dpLienKet = dsDatPhong.FirstOrDefault(d => d.MaDatPhong == hd.DAT_PHONGMaDatPhong);
                    if (dpLienKet != null &&
                        !string.IsNullOrEmpty(dpLienKet.KHACH_HANGMaKH) &&
                        dsKhachHang.TryGetValue(dpLienKet.KHACH_HANGMaKH, out var kh) &&
                        !string.IsNullOrWhiteSpace(kh.HoTen))
                    {
                        tenKhachBan = kh.HoTen.Trim();
                    }
                }

                decimal tamTinh = TinhTamTinhTuJson(hd.DanhSachMonJson, hd.TongTienMonAn ?? 0m);
                decimal giamGia = hd.GiamGia ?? 0m;
                decimal phiPhucVu = Math.Round(tamTinh * 0.05m, 0);
                decimal thueVat = Math.Round(tamTinh * 0.10m, 0);

                // Ưu tiên lấy đúng ThanhTien đã chốt trong DB nếu có
                decimal tongCoThue = (hd.ThanhTien.HasValue && hd.ThanhTien.Value > 0)
                    ? hd.ThanhTien.Value
                    : Math.Max(0m, tamTinh + phiPhucVu + thueVat - giamGia);

                decimal tongKhongThue = Math.Max(0m, tamTinh - giamGia);

                var giaoDichNH = dsThanhToan
                    .Where(t => t.HOA_DON_NHA_HANGMaHoaDonNhaHang == hd.MaHoaDonNhaHang && t.TrangThaiGiaoDich != "Thất bại")
                    .OrderByDescending(t => t.ThoiGianGiaoDich)
                    .FirstOrDefault();

                decimal tienDaThu = (giaoDichNH?.SoTien ?? 0m) > 0 ? giaoDichNH!.SoTien!.Value : tongCoThue;
                DateTime? ngayThanhToan = giaoDichNH?.ThoiGianGiaoDich ?? hd.ThoiGianTao;
                decimal conNoNH = Math.Max(0m, tongCoThue - tienDaThu);

                allItems.Add(new RevenueReportItem
                {
                    MaChungTu = hd.MaHoaDonNhaHang,
                    PhanHe = "Nhà hàng",
                    LoaiPhong = "Khu vực Nhà hàng",
                    TenPhong = tenBanList.Any() ? string.Join(", ", tenBanList.Distinct()) : "Tại quầy",
                    TenKhach = string.IsNullOrWhiteSpace(tenKhachBan) ? "Khách lẻ (Nhà hàng)" : tenKhachBan,
                    NgayDen = hd.ThoiGianTao,
                    NgayDi = ngayThanhToan,
                    TienPhong = 0m,
                    DichVu = BaoGomThuePhi ? (tongCoThue + giamGia) : tamTinh,
                    ChietKhau = giamGia,
                    TongDoanhThu = BaoGomThuePhi ? tongCoThue : tongKhongThue,
                    NoTruoc = conNoNH,
                    TienDaThu = tienDaThu,
                    TrangThai = conNoNH == 0 ? "Đã thanh toán" : "Thanh toán một phần"
                });
            }

            // Danh sách cho các dropdown bộ lọc
            DanhSachLoaiPhong = allItems.Where(x => !string.IsNullOrEmpty(x.LoaiPhong)).Select(x => x.LoaiPhong).Distinct().OrderBy(x => x).ToList();
            DanhSachPhanHe = allItems.Where(x => !string.IsNullOrEmpty(x.PhanHe)).Select(x => x.PhanHe).Distinct().OrderBy(x => x).ToList();
            DanhSachTrangThai = allItems.Where(x => !string.IsNullOrEmpty(x.TrangThai)).Select(x => x.TrangThai).Distinct().OrderBy(x => x).ToList();

            // =========================================================================
            // 3. ÁP DỤNG BỘ LỌC, SẮP XẾP VÀ GOM NHÓM
            // =========================================================================
            IEnumerable<RevenueReportItem> query = allItems;

            if (TuNgay.HasValue && DenNgay.HasValue)
            {
                DateTime denNgayCuoiPhut = DenNgay.Value.AddSeconds(59);

                if (CheDoGhiNhan == "NgayTraPhong")
                {
                    query = query.Where(x => x.NgayDi.HasValue && x.NgayDi.Value >= TuNgay.Value && x.NgayDi.Value <= denNgayCuoiPhut);
                }
                else
                {
                    query = query.Where(x =>
                        (x.NgayDen ?? x.NgayDi).HasValue &&
                        (x.NgayDi ?? x.NgayDen).HasValue &&
                        (x.NgayDen ?? x.NgayDi)!.Value <= denNgayCuoiPhut &&
                        (x.NgayDi ?? x.NgayDen)!.Value >= TuNgay.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(TrangThaiLoc))
                query = query.Where(x => x.TrangThai == TrangThaiLoc);

            if (!string.IsNullOrWhiteSpace(LoaiPhongLoc))
                query = query.Where(x => x.LoaiPhong == LoaiPhongLoc);

            if (!string.IsNullOrWhiteSpace(PhanHeLoc))
                query = query.Where(x => x.PhanHe == PhanHeLoc);

            if (!string.IsNullOrWhiteSpace(TuKhoa))
            {
                string kw = TuKhoa.Trim().ToLower();
                query = query.Where(x =>
                    x.MaChungTu.ToLower().Contains(kw) ||
                    x.TenKhach.ToLower().Contains(kw) ||
                    x.TenPhong.ToLower().Contains(kw));
            }

            query = SortOrder == "asc"
                ? query.OrderBy(x => x.NgayDi)
                : query.OrderByDescending(x => x.NgayDi);

            DanhSachBaoCao = query.ToList();

            DuLieuTheoNhom = NhomTheo switch
            {
                "LoaiPhong" => DanhSachBaoCao.GroupBy(x => x.LoaiPhong).ToList(),
                "KhongNhom" => DanhSachBaoCao.GroupBy(x => "Tất cả").ToList(),
                _ => DanhSachBaoCao.GroupBy(x => x.PhanHe).ToList()
            };

            TongTienPhong = DanhSachBaoCao.Sum(x => x.TienPhong);
            TongDichVu = DanhSachBaoCao.Sum(x => x.DichVu);
            TongChietKhau = DanhSachBaoCao.Sum(x => x.ChietKhau);
            TongCongDoanhThu = DanhSachBaoCao.Sum(x => x.TongDoanhThu);
            TongNoTruoc = DanhSachBaoCao.Sum(x => x.NoTruoc);
            TongTienDaThu = DanhSachBaoCao.Sum(x => x.TienDaThu);
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

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
    }
}