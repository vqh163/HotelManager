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
    public class BaoCaoSalesB2BModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoSalesB2BModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Chi tiết từng booking của đối tác (hiển thị trong Modal)
        public class PartnerBookingDetail
        {
            public string MaDatPhong { get; set; } = string.Empty;
            public string TenKhach { get; set; } = string.Empty;
            public string Phong { get; set; } = string.Empty;
            public string NgayNhan { get; set; } = string.Empty;
            public string NgayTra { get; set; } = string.Empty;
            public int SoDemPhong { get; set; }
            public decimal DoanhThu { get; set; }
            public decimal HoaHong { get; set; }
            public string TrangThai { get; set; } = string.Empty;
        }

        // Dòng tổng hợp theo từng đối tác B2B
        public class SalesB2BItem
        {
            public string MaDoiTac { get; set; } = string.Empty;
            public string TenDoiTac { get; set; } = string.Empty;
            public string LoaiDoiTac { get; set; } = string.Empty;
            public string NguoiLienHe { get; set; } = string.Empty;
            public string SDT { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;

            public int SoBooking { get; set; }
            public int SoBookingHuy { get; set; }
            public int TongDemPhong { get; set; }
            public int TongSoKhach { get; set; }

            public decimal TongDoanhThu { get; set; }
            public decimal TyLeHoaHong { get; set; } // % (VD: 10, 15)
            public decimal TienHoaHong { get; set; }
            public decimal DoanhThuThucNhan => Math.Max(0m, TongDoanhThu - TienHoaHong);

            public decimal HanMucCongNo { get; set; }
            public decimal SoDuCongNo { get; set; }
            public bool VuotHanMuc => HanMucCongNo > 0 && SoDuCongNo > HanMucCongNo;

            public List<PartnerBookingDetail> DanhSachBooking { get; set; } = new();
        }

        // --- THAM SỐ LỌC ---
        [BindProperty(SupportsGet = true)]
        public DateTime? TuNgay { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DenNgay { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? LoaiDoiTacLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ChiHienCoPhatSinh { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public string NhomTheo { get; set; } = "LoaiDoiTac"; // "LoaiDoiTac", "KhongNhom"

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "doanhthu_desc";

        // --- DỮ LIỆU VIEW ---
        public List<SalesB2BItem> DanhSachBaoCao { get; set; } = new();
        public List<IGrouping<string, SalesB2BItem>> DuLieuTheoNhom { get; set; } = new();
        public List<string> DanhSachLoaiDoiTac { get; set; } = new();

        // --- KPI & TỔNG CỘNG ---
        public int TongSoDoiTac { get; set; }
        public int SoDoiTacCoBooking { get; set; }
        public int TongBooking { get; set; }
        public int TongDemPhong { get; set; }
        public int TongKhach { get; set; }
        public decimal TongDoanhThuB2B { get; set; }
        public decimal TongTienHoaHong { get; set; }
        public decimal TongDoanhThuThucNhan { get; set; }
        public decimal TongCongNo { get; set; }

        public async Task OnGetAsync()
        {
            TuNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DenNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));

            await LoadB2BDataAsync();
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            TuNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DenNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));

            await LoadB2BDataAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Mã đối tác,Tên đối tác,Loại đối tác,Người liên hệ,SĐT,Số booking,Số đơn hủy,Số đêm phòng,Tổng lượt khách,Doanh thu B2B (VNĐ),Tỷ lệ hoa hồng (%),Tiền hoa hồng (VNĐ),Doanh thu thực nhận (VNĐ),Công nợ hiện tại (VNĐ),Hạn mức công nợ (VNĐ)");

            foreach (var item in DanhSachBaoCao)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(item.MaDoiTac),
                    EscapeCsv(item.TenDoiTac),
                    EscapeCsv(item.LoaiDoiTac),
                    EscapeCsv(item.NguoiLienHe),
                    EscapeCsv(item.SDT),
                    item.SoBooking,
                    item.SoBookingHuy,
                    item.TongDemPhong,
                    item.TongSoKhach,
                    item.TongDoanhThu,
                    item.TyLeHoaHong,
                    item.TienHoaHong,
                    item.DoanhThuThucNhan,
                    item.SoDuCongNo,
                    item.HanMucCongNo
                ));
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"BaoCaoSalesB2B_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private async Task LoadB2BDataAsync()
        {
            var dsDoiTac = await _context.DOI_TAC.AsNoTracking().ToListAsync();
            var dsDatPhong = await _context.DAT_PHONG.AsNoTracking().ToListAsync();
            var dsChiTietDP = await _context.CHI_TIET_DAT_PHONG.AsNoTracking().ToListAsync();
            var dsPhong = await _context.PHONG.AsNoTracking().ToDictionaryAsync(p => p.MaPhong);
            var dsKhachHang = await _context.KHACH_HANG.AsNoTracking().ToDictionaryAsync(k => k.MaKH);
            var dsHoaDon = await _context.HOA_DON.AsNoTracking().ToListAsync();

            DateTime tuNgayStart = (TuNgay ?? DateTime.MinValue).Date;
            DateTime denNgayEnd = (DenNgay ?? DateTime.MaxValue).Date.AddDays(1).AddTicks(-1);

            // Lọc danh sách đặt phòng trong khoảng thời gian đã chọn
            var datPhongTrongKy = dsDatPhong.Where(dp =>
            {
                DateTime? mocNgay = dp.NgayNhan ?? dp.NgayDat;
                return mocNgay.HasValue && mocNgay.Value >= tuNgayStart && mocNgay.Value <= denNgayEnd;
            }).ToList();

            var allItems = new List<SalesB2BItem>();

            foreach (var dt in dsDoiTac)
            {
                // Chuẩn hóa % chiết khấu/hoa hồng (hỗ trợ cả dạng lưu 15 hoặc 0.15)
                decimal tyLeHoaHong = 0m;
                if (dt.ChietKhau.HasValue && dt.ChietKhau.Value > 0)
                {
                    tyLeHoaHong = dt.ChietKhau.Value < 1
                        ? Math.Round((decimal)(dt.ChietKhau.Value * 100), 2)
                        : Math.Round((decimal)dt.ChietKhau.Value, 2);
                }

                // Tìm các booking thuộc đối tác này (ưu tiên khóa ngoại DOI_TACMaDoiTac, dự phòng theo KenhDatPhong)
                var bookingsCuaDoiTac = datPhongTrongKy.Where(dp =>
                    (!string.IsNullOrWhiteSpace(dp.DOI_TACMaDoiTac) &&
                     string.Equals(dp.DOI_TACMaDoiTac.Trim(), dt.MaDoiTac.Trim(), StringComparison.OrdinalIgnoreCase))
                    ||
                    (string.IsNullOrWhiteSpace(dp.DOI_TACMaDoiTac) &&
                     !string.IsNullOrWhiteSpace(dp.KenhDatPhong) &&
                     !string.IsNullOrWhiteSpace(dt.TenDoiTac) &&
                     string.Equals(dp.KenhDatPhong.Trim(), dt.TenDoiTac.Trim(), StringComparison.OrdinalIgnoreCase))
                ).ToList();

                int soBookingHuy = 0;
                int soBookingHopLe = 0;
                int tongDemPhong = 0;
                int tongKhach = 0;
                decimal tongDoanhThu = 0m;
                var chiTietList = new List<PartnerBookingDetail>();

                foreach (var dp in bookingsCuaDoiTac)
                {
                    bool biHuy = !string.IsNullOrEmpty(dp.TrangThaiDatPhong) &&
                                 dp.TrangThaiDatPhong.Contains("Hủy", StringComparison.OrdinalIgnoreCase);

                    if (biHuy)
                    {
                        soBookingHuy++;
                        continue;
                    }

                    soBookingHopLe++;
                    tongKhach += dp.SoLuongKhach ?? 1;

                    var chiTiets = dsChiTietDP.Where(c => c.DAT_PHONGMaDatPhong == dp.MaDatPhong).ToList();
                    var hoaDon = dsHoaDon
                        .Where(h => h.DAT_PHONGMaDatPhong == dp.MaDatPhong)
                        .OrderByDescending(h => h.NgayLap)
                        .FirstOrDefault();

                    int demCuaBooking = 0;
                    decimal tienTamTinh = 0m;
                    var tenPhongList = new List<string>();

                    if (chiTiets.Any())
                    {
                        foreach (var ct in chiTiets)
                        {
                            PHONG? p = (!string.IsNullOrEmpty(ct.PHONGMaPhong) && dsPhong.TryGetValue(ct.PHONGMaPhong, out var pFound))
                                ? pFound
                                : null;

                            if (p != null && !string.IsNullOrWhiteSpace(p.SoPhong))
                                tenPhongList.Add(p.SoPhong.Trim());

                            int soNgay = ct.SoNgayO > 0
                                ? ct.SoNgayO
                                : Math.Max(1, ((dp.NgayTra ?? DateTime.Now).Date - (dp.NgayNhan ?? DateTime.Now).Date).Days);

                            demCuaBooking += soNgay;
                            decimal donGia = ct.DonGia ?? p?.GiaTheoNgay ?? 0m;
                            tienTamTinh += donGia * soNgay;
                        }
                    }
                    else
                    {
                        demCuaBooking = Math.Max(1, ((dp.NgayTra ?? DateTime.Now).Date - (dp.NgayNhan ?? DateTime.Now).Date).Days);
                    }

                    decimal doanhThuBooking = hoaDon?.TongTienThanhToan
                        ?? (hoaDon != null
                            ? Math.Max(0m, (hoaDon.TienPhong ?? 0m) + (hoaDon.TienDichVu ?? 0m) + (hoaDon.TienNhaHang ?? 0m) - (hoaDon.TienGiamGia ?? 0m))
                            : tienTamTinh);

                    decimal hoaHongBooking = Math.Round(doanhThuBooking * (tyLeHoaHong / 100m), 0);

                    tongDemPhong += demCuaBooking;
                    tongDoanhThu += doanhThuBooking;

                    string tenKhach = "Khách đoàn / B2B";
                    if (!string.IsNullOrEmpty(dp.KHACH_HANGMaKH) &&
                        dsKhachHang.TryGetValue(dp.KHACH_HANGMaKH, out var kh) &&
                        !string.IsNullOrWhiteSpace(kh.HoTen))
                    {
                        tenKhach = kh.HoTen.Trim();
                    }

                    chiTietList.Add(new PartnerBookingDetail
                    {
                        MaDatPhong = dp.MaDatPhong,
                        TenKhach = tenKhach,
                        Phong = tenPhongList.Any() ? string.Join(", ", tenPhongList.Distinct()) : "—",
                        NgayNhan = dp.NgayNhan?.ToString("dd/MM/yyyy") ?? dp.NgayDat?.ToString("dd/MM/yyyy") ?? "—",
                        NgayTra = dp.NgayTra?.ToString("dd/MM/yyyy") ?? "—",
                        SoDemPhong = demCuaBooking,
                        DoanhThu = doanhThuBooking,
                        HoaHong = hoaHongBooking,
                        TrangThai = string.IsNullOrWhiteSpace(dp.TrangThaiDatPhong) ? "Đang xử lý" : dp.TrangThaiDatPhong
                    });
                }

                decimal tienHoaHong = Math.Round(tongDoanhThu * (tyLeHoaHong / 100m), 0);

                allItems.Add(new SalesB2BItem
                {
                    MaDoiTac = dt.MaDoiTac,
                    TenDoiTac = string.IsNullOrWhiteSpace(dt.TenDoiTac) ? dt.MaDoiTac : dt.TenDoiTac.Trim(),
                    LoaiDoiTac = string.IsNullOrWhiteSpace(dt.LoaiDoiTac) ? "Đối tác khác" : dt.LoaiDoiTac.Trim(),
                    NguoiLienHe = dt.NguoiLienHe?.Trim() ?? "—",
                    SDT = dt.SDT?.Trim() ?? "",
                    Email = dt.Email?.Trim() ?? "",
                    SoBooking = soBookingHopLe,
                    SoBookingHuy = soBookingHuy,
                    TongDemPhong = tongDemPhong,
                    TongSoKhach = tongKhach,
                    TongDoanhThu = tongDoanhThu,
                    TyLeHoaHong = tyLeHoaHong,
                    TienHoaHong = tienHoaHong,
                    HanMucCongNo = dt.HanMucCongNo ?? 0m,
                    SoDuCongNo = dt.SoDuCongNo ?? 0m,
                    DanhSachBooking = chiTietList
                });
            }

            DanhSachLoaiDoiTac = allItems
                .Select(x => x.LoaiDoiTac)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Áp dụng bộ lọc
            IEnumerable<SalesB2BItem> query = allItems;

            if (!string.IsNullOrWhiteSpace(LoaiDoiTacLoc))
                query = query.Where(x => x.LoaiDoiTac == LoaiDoiTacLoc);

            if (ChiHienCoPhatSinh)
                query = query.Where(x => x.SoBooking > 0 || x.TongDoanhThu > 0);

            if (!string.IsNullOrWhiteSpace(TuKhoa))
            {
                string kw = TuKhoa.Trim().ToLower();
                query = query.Where(x =>
                    x.MaDoiTac.ToLower().Contains(kw) ||
                    x.TenDoiTac.ToLower().Contains(kw) ||
                    x.NguoiLienHe.ToLower().Contains(kw) ||
                    x.SDT.ToLower().Contains(kw));
            }

            query = SortBy switch
            {
                "hoahong_desc" => query.OrderByDescending(x => x.TienHoaHong).ThenBy(x => x.TenDoiTac),
                "demphong_desc" => query.OrderByDescending(x => x.TongDemPhong).ThenBy(x => x.TenDoiTac),
                "congno_desc" => query.OrderByDescending(x => x.SoDuCongNo).ThenBy(x => x.TenDoiTac),
                "ten_asc" => query.OrderBy(x => x.TenDoiTac),
                _ => query.OrderByDescending(x => x.TongDoanhThu).ThenByDescending(x => x.SoBooking).ThenBy(x => x.TenDoiTac)
            };

            DanhSachBaoCao = query.ToList();

            DuLieuTheoNhom = NhomTheo switch
            {
                "KhongNhom" => DanhSachBaoCao.GroupBy(x => "Tất cả").ToList(),
                _ => DanhSachBaoCao.GroupBy(x => x.LoaiDoiTac).ToList()
            };

            // Tính KPI & Tổng cộng
            TongSoDoiTac = DanhSachBaoCao.Count;
            SoDoiTacCoBooking = DanhSachBaoCao.Count(x => x.SoBooking > 0);
            TongBooking = DanhSachBaoCao.Sum(x => x.SoBooking);
            TongDemPhong = DanhSachBaoCao.Sum(x => x.TongDemPhong);
            TongKhach = DanhSachBaoCao.Sum(x => x.TongSoKhach);
            TongDoanhThuB2B = DanhSachBaoCao.Sum(x => x.TongDoanhThu);
            TongTienHoaHong = DanhSachBaoCao.Sum(x => x.TienHoaHong);
            TongDoanhThuThucNhan = DanhSachBaoCao.Sum(x => x.DoanhThuThucNhan);
            TongCongNo = DanhSachBaoCao.Sum(x => x.SoDuCongNo);
        }

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
    }
}