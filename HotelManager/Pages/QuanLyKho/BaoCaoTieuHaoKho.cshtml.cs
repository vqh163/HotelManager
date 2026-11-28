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
using System.Text;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyKho
{
    [Authorize]
    public class BaoCaoTieuHaoKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoTieuHaoKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-29);

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime DenNgay { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public string LoaiKhoFilter { get; set; } = "ALL";

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SapXepTheo { get; set; } = "TIEU_HAO_DESC";

        [BindProperty(SupportsGet = true)]
        public string CheDoXem { get; set; } = "ALL";

        public int DemTatCa { get; set; }
        public int DemCoPhatSinh { get; set; }
        public int DemCoHaoHut { get; set; }

        public int TongSoMatHang { get; set; }
        public int SoMatHangSapHet { get; set; }
        public decimal TongGiaTriTonKho { get; set; }
        public int TongSoLuongNhap { get; set; }
        public decimal TongChiPhiNhapTrongKy { get; set; }
        public int TongSoLuongXuat { get; set; }
        public decimal TongGiaTriTieuHaoTrongKy { get; set; }
        public int TongSoLuongHaoHutKiemKe { get; set; }
        public int TongSoLuongThuaKiemKe { get; set; }
        public decimal TongGiaTriHaoHutKiemKe { get; set; }
        public double TyLeTieuHaoTrenVon { get; set; }

        public List<BaoCaoVatTuViewModel> DanhSachBaoCaoVatTu { get; set; } = new List<BaoCaoVatTuViewModel>();
        public List<ThongKeBoPhanViewModel> ThongKeTheoBoPhan { get; set; } = new List<ThongKeBoPhanViewModel>();
        public List<ThongKePhanKhuViewModel> ThongKeTheoPhanKhu { get; set; } = new List<ThongKePhanKhuViewModel>();

        public string ThongBaoLoi { get; set; } = string.Empty;
        public string CanhBaoDuLieu { get; set; } = string.Empty;

        public class BaoCaoVatTuViewModel
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiVatTu { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public bool DaXoaKhoiDanhMuc { get; set; }
            public int TonHienTai { get; set; }
            public int TonToiThieu { get; set; }
            public decimal DonGiaHienTai { get; set; }
            public decimal GiaTriTonHienTai => TonHienTai * DonGiaHienTai;
            public int SoLuongNhapKyNay { get; set; }
            public decimal ChiPhiNhapKyNay { get; set; }
            public int SoLuongXuatKyNay { get; set; }
            public decimal ChiPhiTieuHaoKyNay { get; set; }
            public int SoLuongHaoHutKiemKe { get; set; }
            public decimal ChiPhiHaoHutKiemKe { get; set; }
            public int SoLuongThuaKiemKe { get; set; }
            public bool CoPhatSinhTrongKy =>
                SoLuongNhapKyNay > 0 || SoLuongXuatKyNay > 0 || SoLuongHaoHutKiemKe > 0 || SoLuongThuaKiemKe > 0;
        }

        public class ThongKeBoPhanViewModel
        {
            public string TenBoPhan { get; set; } = string.Empty;
            public int SoLuotXuat { get; set; }
            public decimal TongChiPhiTieuHao { get; set; }
        }

        public class ThongKePhanKhuViewModel
        {
            public string TenPhanKhu { get; set; } = string.Empty;
            public int SoMatHang { get; set; }
            public decimal TongChiPhiNhap { get; set; }
            public decimal TongChiPhiTieuHao { get; set; }
            public decimal TongGiaTriTon { get; set; }
        }

        private class GiaoDichLogRaw
        {
            public string LoaiGiaoDich { get; set; } = string.Empty;
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiKho { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien => SoLuong * DonGia;
            public string DoiTacHoacBoPhan { get; set; } = string.Empty;
            public DateTime ThoiGian { get; set; }
        }

        public async Task OnGetAsync()
        {
            await TongHopBaoCaoAsync();
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            if (TuNgay.Date > DenNgay.Date)
            {
                ThongBaoLoi = "Khoảng thời gian báo cáo không hợp lệ: 'Từ ngày' không được lớn hơn 'Đến ngày'.";
                return Page();
            }

            await TongHopBaoCaoAsync();

            var sb = new StringBuilder();
            sb.AppendLine("MaVatTu,TenVatTu,PhanKhu,DonViTinh,SLNhap,ChiPhiNhap,SLXuat,ChiPhiTieuHao,HaoHutKiemKe,ChiPhiHaoHut,TonCuoi,GiaTriTon");

            foreach (var item in DanhSachBaoCaoVatTu)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(item.MaVatTu),
                    CsvEscape(item.TenVatTu + (item.DaXoaKhoiDanhMuc ? " (Da xoa)" : "")),
                    CsvEscape(item.LoaiVatTu),
                    CsvEscape(item.DonViTinh),
                    item.SoLuongNhapKyNay,
                    item.ChiPhiNhapKyNay.ToString("0", CultureInfo.InvariantCulture),
                    item.SoLuongXuatKyNay,
                    item.ChiPhiTieuHaoKyNay.ToString("0", CultureInfo.InvariantCulture),
                    item.SoLuongHaoHutKiemKe,
                    item.ChiPhiHaoHutKiemKe.ToString("0", CultureInfo.InvariantCulture),
                    item.TonHienTai,
                    item.GiaTriTonHienTai.ToString("0", CultureInfo.InvariantCulture)
                ));
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            string fileName = $"BaoCaoTieuHaoKho_{TuNgay:yyyyMMdd}_{DenNgay:yyyyMMdd}.csv";

            return File(preamble.Concat(contentBytes).ToArray(), "text/csv; charset=utf-8", fileName);
        }

        private async Task TongHopBaoCaoAsync()
        {
            try
            {
                if (TuNgay == default) TuNgay = DateTime.Today.AddDays(-29);
                if (DenNgay == default) DenNgay = DateTime.Today;

                if (TuNgay.Date > DenNgay.Date)
                {
                    ThongBaoLoi = "Khoảng thời gian báo cáo không hợp lệ: 'Từ ngày' không được lớn hơn 'Đến ngày'.";
                    return;
                }

                var toanBoKho = await _context.KHO_VAT_TU
                    .AsNoTracking()
                    .OrderBy(v => v.TenVatTu)
                    .ToListAsync();

                var dictKho = toanBoKho.ToDictionary(v => v.MaVatTu, v => v);

                DateTime startDate = TuNgay.Date;
                DateTime endDate = DenNgay.Date.AddDays(1).AddTicks(-1);
                var tatCaLogTrongKy = DocLogGiaoDichTrongKy(startDate, endDate);

                foreach (var log in tatCaLogTrongKy)
                {
                    if (dictKho.TryGetValue(log.MaVatTu, out var vtHienTai))
                    {
                        log.LoaiKho = vtHienTai.LoaiVatTu ?? log.LoaiKho;
                        log.TenVatTu = vtHienTai.TenVatTu ?? log.TenVatTu;
                        log.DonViTinh = vtHienTai.DonViTinh ?? log.DonViTinh;
                    }
                }

                string keyword = TuKhoa?.Trim() ?? string.Empty;

                bool KhopBoLoc(string ma, string ten, string loaiKho)
                {
                    if (!string.IsNullOrEmpty(LoaiKhoFilter) && LoaiKhoFilter != "ALL" && loaiKho != LoaiKhoFilter)
                        return false;

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        return ma.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                            || ten.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                    }
                    return true;
                }

                var khoDaLoc = toanBoKho
                    .Where(v => KhopBoLoc(v.MaVatTu, v.TenVatTu ?? "", v.LoaiVatTu ?? "Kho chung"))
                    .ToList();

                var logsDaLoc = tatCaLogTrongKy
                    .Where(l => KhopBoLoc(l.MaVatTu, l.TenVatTu, l.LoaiKho))
                    .ToList();

                var logTheoVatTu = logsDaLoc
                    .GroupBy(l => l.MaVatTu)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var danhSachDayDu = new List<BaoCaoVatTuViewModel>();

                foreach (var vt in khoDaLoc)
                {
                    logTheoVatTu.TryGetValue(vt.MaVatTu, out var logsCuaVT);
                    logsCuaVT ??= new List<GiaoDichLogRaw>();

                    danhSachDayDu.Add(TaoDongBaoCao(
                        vt.MaVatTu,
                        vt.TenVatTu ?? "Chưa cập nhật",
                        vt.LoaiVatTu ?? "Kho chung",
                        vt.DonViTinh ?? "Cái",
                        daXoa: false,
                        tonHienTai: vt.SoLuongTon ?? 0,
                        tonToiThieu: vt.SoLuongTonToiThieu ?? 0,
                        donGiaHienTai: vt.DonGiaNhap ?? 0,
                        logsCuaVT));
                }

                var maDaCo = new HashSet<string>(khoDaLoc.Select(v => v.MaVatTu));
                foreach (var nhom in logTheoVatTu)
                {
                    if (maDaCo.Contains(nhom.Key) || nhom.Key == "ALL") continue;

                    var mau = nhom.Value.OrderByDescending(x => x.ThoiGian).First();
                    danhSachDayDu.Add(TaoDongBaoCao(
                        nhom.Key,
                        mau.TenVatTu,
                        mau.LoaiKho,
                        mau.DonViTinh,
                        daXoa: true,
                        tonHienTai: 0,
                        tonToiThieu: 0,
                        donGiaHienTai: mau.DonGia,
                        nhom.Value));
                }

                DemTatCa = danhSachDayDu.Count;
                DemCoPhatSinh = danhSachDayDu.Count(x => x.CoPhatSinhTrongKy);
                DemCoHaoHut = danhSachDayDu.Count(x => x.SoLuongHaoHutKiemKe > 0);

                IEnumerable<BaoCaoVatTuViewModel> ketQua = CheDoXem switch
                {
                    "PHAT_SINH" => danhSachDayDu.Where(x => x.CoPhatSinhTrongKy),
                    "HAO_HUT" => danhSachDayDu.Where(x => x.SoLuongHaoHutKiemKe > 0),
                    _ => danhSachDayDu
                };

                DanhSachBaoCaoVatTu = SapXepTheo switch
                {
                    "TON_DESC" => ketQua.OrderByDescending(x => x.GiaTriTonHienTai).ThenBy(x => x.TenVatTu).ToList(),
                    "NHAP_DESC" => ketQua.OrderByDescending(x => x.ChiPhiNhapKyNay).ThenBy(x => x.TenVatTu).ToList(),
                    "HAO_HUT_DESC" => ketQua.OrderByDescending(x => x.ChiPhiHaoHutKiemKe).ThenBy(x => x.TenVatTu).ToList(),
                    "TEN_ASC" => ketQua.OrderBy(x => x.TenVatTu).ToList(),
                    _ => ketQua.OrderByDescending(x => x.ChiPhiTieuHaoKyNay).ThenByDescending(x => x.GiaTriTonHienTai).ToList()
                };

                TongSoMatHang = DanhSachBaoCaoVatTu.Count(x => !x.DaXoaKhoiDanhMuc);
                SoMatHangSapHet = DanhSachBaoCaoVatTu.Count(x => !x.DaXoaKhoiDanhMuc && x.TonHienTai <= x.TonToiThieu);
                TongGiaTriTonKho = DanhSachBaoCaoVatTu.Sum(x => x.GiaTriTonHienTai);

                TongSoLuongNhap = DanhSachBaoCaoVatTu.Sum(x => x.SoLuongNhapKyNay);
                TongChiPhiNhapTrongKy = DanhSachBaoCaoVatTu.Sum(x => x.ChiPhiNhapKyNay);

                TongSoLuongXuat = DanhSachBaoCaoVatTu.Sum(x => x.SoLuongXuatKyNay);
                TongGiaTriTieuHaoTrongKy = DanhSachBaoCaoVatTu.Sum(x => x.ChiPhiTieuHaoKyNay);

                TongSoLuongHaoHutKiemKe = DanhSachBaoCaoVatTu.Sum(x => x.SoLuongHaoHutKiemKe);
                TongSoLuongThuaKiemKe = DanhSachBaoCaoVatTu.Sum(x => x.SoLuongThuaKiemKe);
                TongGiaTriHaoHutKiemKe = DanhSachBaoCaoVatTu.Sum(x => x.ChiPhiHaoHutKiemKe);

                decimal tongVonThamChieu = TongGiaTriTonKho + TongGiaTriTieuHaoTrongKy;
                TyLeTieuHaoTrenVon = tongVonThamChieu > 0
                    ? (double)(TongGiaTriTieuHaoTrongKy * 100 / tongVonThamChieu)
                    : 0;

                ThongKeTheoPhanKhu = DanhSachBaoCaoVatTu
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.LoaiVatTu) ? "Kho chung" : x.LoaiVatTu)
                    .Select(g => new ThongKePhanKhuViewModel
                    {
                        TenPhanKhu = g.Key,
                        SoMatHang = g.Count(x => !x.DaXoaKhoiDanhMuc),
                        TongChiPhiNhap = g.Sum(x => x.ChiPhiNhapKyNay),
                        TongChiPhiTieuHao = g.Sum(x => x.ChiPhiTieuHaoKyNay),
                        TongGiaTriTon = g.Sum(x => x.GiaTriTonHienTai)
                    })
                    .OrderByDescending(x => x.TongGiaTriTon)
                    .ToList();

                var maDangHienThi = new HashSet<string>(DanhSachBaoCaoVatTu.Select(x => x.MaVatTu));
                var logsXuat = logsDaLoc
                    .Where(x => x.LoaiGiaoDich == "XUAT" && maDangHienThi.Contains(x.MaVatTu))
                    .ToList();

                if (logsXuat.Any())
                {
                    ThongKeTheoBoPhan = logsXuat
                        .GroupBy(x => string.IsNullOrWhiteSpace(x.DoiTacHoacBoPhan) ? "Bộ phận khác" : x.DoiTacHoacBoPhan)
                        .Select(g => new ThongKeBoPhanViewModel
                        {
                            TenBoPhan = g.Key,
                            SoLuotXuat = g.Count(),
                            TongChiPhiTieuHao = g.Sum(x => x.ThanhTien)
                        })
                        .OrderByDescending(x => x.TongChiPhiTieuHao)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                ThongBaoLoi = "Lỗi khi tổng hợp báo cáo tiêu hao kho: " + ex.Message;
            }
        }

        private static BaoCaoVatTuViewModel TaoDongBaoCao(
            string maVT, string tenVT, string loaiVT, string dvt,
            bool daXoa, int tonHienTai, int tonToiThieu, decimal donGiaHienTai,
            List<GiaoDichLogRaw> logs)
        {
            var nhap = logs.Where(x => x.LoaiGiaoDich == "NHAP").ToList();
            var xuat = logs.Where(x => x.LoaiGiaoDich == "XUAT").ToList();
            var haoHut = logs.Where(x => x.LoaiGiaoDich == "KIEM_KE_THIEU").ToList();
            var thuaKK = logs.Where(x => x.LoaiGiaoDich == "KIEM_KE_THUA").ToList();

            return new BaoCaoVatTuViewModel
            {
                MaVatTu = maVT,
                TenVatTu = tenVT,
                LoaiVatTu = loaiVT,
                DonViTinh = dvt,
                DaXoaKhoiDanhMuc = daXoa,
                TonHienTai = tonHienTai,
                TonToiThieu = tonToiThieu,
                DonGiaHienTai = donGiaHienTai,
                SoLuongNhapKyNay = nhap.Sum(x => x.SoLuong),
                ChiPhiNhapKyNay = nhap.Sum(x => x.ThanhTien),
                SoLuongXuatKyNay = xuat.Sum(x => x.SoLuong),
                ChiPhiTieuHaoKyNay = xuat.Sum(x => x.ThanhTien),
                SoLuongHaoHutKiemKe = haoHut.Sum(x => x.SoLuong),
                ChiPhiHaoHutKiemKe = haoHut.Sum(x => x.ThanhTien),
                SoLuongThuaKiemKe = thuaKK.Sum(x => x.SoLuong)
            };
        }

        private List<GiaoDichLogRaw> DocLogGiaoDichTrongKy(DateTime tuNgay, DateTime denNgay)
        {
            var list = new List<GiaoDichLogRaw>();
            int soDongLoi = 0;

            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                if (!System.IO.File.Exists(filePath)) return list;

                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var p = line.Split('|');
                    if (p.Length < 11)
                    {
                        soDongLoi++;
                        continue;
                    }

                    string loai = p[1].Trim();
                    if (loai == "KIEM_KE_KHOP") continue;

                    bool okSl = int.TryParse(p[6].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sl);
                    bool okGia = decimal.TryParse(p[7].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal gia);
                    bool okTg = DateTime.TryParseExact(
                                    p[10].Trim(),
                                    "yyyy-MM-dd HH:mm:ss",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out DateTime tg)
                                || DateTime.TryParse(p[10].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out tg);

                    if (!okSl || !okGia || !okTg)
                    {
                        soDongLoi++;
                        continue;
                    }

                    if (tg >= tuNgay && tg <= denNgay)
                    {
                        list.Add(new GiaoDichLogRaw
                        {
                            LoaiGiaoDich = loai,
                            MaVatTu = p[2].Trim(),
                            TenVatTu = string.IsNullOrWhiteSpace(p[3]) ? "Vật tư" : p[3].Trim(),
                            LoaiKho = string.IsNullOrWhiteSpace(p[4]) ? "Kho chung" : p[4].Trim(),
                            DonViTinh = string.IsNullOrWhiteSpace(p[5]) ? "Cái" : p[5].Trim(),
                            SoLuong = sl,
                            DonGia = gia,
                            DoiTacHoacBoPhan = p[8].Trim(),
                            ThoiGian = tg
                        });
                    }
                }

                if (soDongLoi > 0)
                {
                    CanhBaoDuLieu = $"Đã bỏ qua {soDongLoi} dòng nhật ký không đúng định dạng khi tổng hợp báo cáo.";
                }
            }
            catch (Exception ex)
            {
                CanhBaoDuLieu = "Không thể đọc tệp nhật ký nhập/xuất kho: " + ex.Message;
            }

            return list;
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}