using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLyKho
{
    public class BaoCaoTieuHaoKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoTieuHaoKhoModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Bộ lọc khoảng thời gian báo cáo (Mặc định 30 ngày gần nhất)
        [BindProperty(SupportsGet = true)]
        public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-30);

        [BindProperty(SupportsGet = true)]
        public DateTime DenNgay { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public string LoaiKhoFilter { get; set; } = "ALL";

        // Các chỉ số KPI tổng quan
        public int TongSoMatHang { get; set; }
        public int SoMatHangSapHet { get; set; }
        public decimal TongGiaTriTonKho { get; set; }
        public int TongSoLuongNhap { get; set; }
        public decimal TongChiPhiNhapTrongKy { get; set; }
        public int TongSoLuongXuat { get; set; }
        public decimal TongGiaTriTieuHaoTrongKy { get; set; }

        // Danh sách thống kê chi tiết theo từng vật tư và theo bộ phận
        public List<BaoCaoVatTuViewModel> DanhSachBaoCaoVatTu { get; set; } = new List<BaoCaoVatTuViewModel>();
        public List<ThongKeBoPhanViewModel> ThongKeTheoBoPhan { get; set; } = new List<ThongKeBoPhanViewModel>();

        public string ThongBaoLoi { get; set; } = string.Empty;

        public class BaoCaoVatTuViewModel
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string LoaiVatTu { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int TonHienTai { get; set; }
            public decimal DonGiaHienTai { get; set; }
            public decimal GiaTriTonHienTai => TonHienTai * DonGiaHienTai;
            public int SoLuongNhapKyNay { get; set; }
            public decimal ChiPhiNhapKyNay { get; set; }
            public int SoLuongXuatKyNay { get; set; }
            public decimal ChiPhiTieuHaoKyNay { get; set; }
        }

        public class ThongKeBoPhanViewModel
        {
            public string TenBoPhan { get; set; } = string.Empty;
            public int SoLuotXuat { get; set; }
            public decimal TongChiPhiTieuHao { get; set; }
            public double TyLePhanTram { get; set; }
        }

        private class GiaoDichLogRaw
        {
            public string LoaiGiaoDich { get; set; } = string.Empty;
            public string MaVatTu { get; set; } = string.Empty;
            public string LoaiKho { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien => SoLuong * DonGia;
            public string DoiTacHoacBoPhan { get; set; } = string.Empty;
            public DateTime ThoiGian { get; set; }
        }

        /// <summary>
        /// Truy vấn bảng KHO_VAT_TU bằng LINQ và tổng hợp lịch sử Nhập - Xuất để lên Báo cáo tiêu hao & chi phí
        /// </summary>
        public async Task OnGetAsync()
        {
            try
            {
                // Đảm bảo ngày bắt đầu không lớn hơn ngày kết thúc
                if (TuNgay > DenNgay)
                {
                    var tam = TuNgay;
                    TuNgay = DenNgay;
                    DenNgay = tam;
                }

                var query = _context.KHO_VAT_TU.AsNoTracking().AsQueryable();
                if (!string.IsNullOrEmpty(LoaiKhoFilter) && LoaiKhoFilter != "ALL")
                {
                    query = query.Where(v => v.LoaiVatTu == LoaiKhoFilter);
                }

                var danhSachKho = await query.OrderBy(v => v.TenVatTu).ToListAsync();

                // Đọc dữ liệu giao dịch Nhập/Xuất trong khoảng thời gian đã chọn
                DateTime startDate = TuNgay.Date;
                DateTime endDate = DenNgay.Date.AddDays(1).AddTicks(-1);
                var logsTrongKy = DocLogGiaoDichTrongKy(startDate, endDate, LoaiKhoFilter);

                // Tính toán các chỉ số KPI tổng quan
                TongSoMatHang = danhSachKho.Count;
                SoMatHangSapHet = danhSachKho.Count(v => (v.SoLuongTon ?? 0) <= (v.SoLuongTonToiThieu ?? 0));
                TongGiaTriTonKho = danhSachKho.Sum(v => (decimal)(v.SoLuongTon ?? 0) * (v.DonGiaNhap ?? 0));

                var logsNhap = logsTrongKy.Where(x => x.LoaiGiaoDich == "NHAP").ToList();
                var logsXuat = logsTrongKy.Where(x => x.LoaiGiaoDich == "XUAT").ToList();

                TongSoLuongNhap = logsNhap.Sum(x => x.SoLuong);
                TongChiPhiNhapTrongKy = logsNhap.Sum(x => x.ThanhTien);

                TongSoLuongXuat = logsXuat.Sum(x => x.SoLuong);
                TongGiaTriTieuHaoTrongKy = logsXuat.Sum(x => x.ThanhTien);

                // Tổng hợp bảng chi tiết theo từng vật tư bằng LINQ
                DanhSachBaoCaoVatTu = danhSachKho.Select(vt =>
                {
                    var nhapVT = logsNhap.Where(l => l.MaVatTu == vt.MaVatTu).ToList();
                    var xuatVT = logsXuat.Where(l => l.MaVatTu == vt.MaVatTu).ToList();

                    return new BaoCaoVatTuViewModel
                    {
                        MaVatTu = vt.MaVatTu,
                        TenVatTu = vt.TenVatTu ?? "Chưa cập nhật",
                        LoaiVatTu = vt.LoaiVatTu ?? "Kho chung",
                        DonViTinh = vt.DonViTinh ?? "Cái",
                        TonHienTai = vt.SoLuongTon ?? 0,
                        DonGiaHienTai = vt.DonGiaNhap ?? 0,
                        SoLuongNhapKyNay = nhapVT.Sum(x => x.SoLuong),
                        ChiPhiNhapKyNay = nhapVT.Sum(x => x.ThanhTien),
                        SoLuongXuatKyNay = xuatVT.Sum(x => x.SoLuong),
                        ChiPhiTieuHaoKyNay = xuatVT.Sum(x => x.ThanhTien)
                    };
                })
                .OrderByDescending(x => x.ChiPhiTieuHaoKyNay)
                .ThenByDescending(x => x.GiaTriTonHienTai)
                .ToList();

                // Phân tích cơ cấu chi phí tiêu hao theo từng bộ phận (Buồng phòng, Nhà hàng/Bếp, Kỹ thuật...)
                if (logsXuat.Any())
                {
                    decimal tongXuat = TongGiaTriTieuHaoTrongKy > 0 ? TongGiaTriTieuHaoTrongKy : 1;

                    ThongKeTheoBoPhan = logsXuat
                        .GroupBy(x => string.IsNullOrWhiteSpace(x.DoiTacHoacBoPhan) ? "Bộ phận khác" : x.DoiTacHoacBoPhan)
                        .Select(g => new ThongKeBoPhanViewModel
                        {
                            TenBoPhan = g.Key,
                            SoLuotXuat = g.Count(),
                            TongChiPhiTieuHao = g.Sum(x => x.ThanhTien),
                            TyLePhanTram = (double)(g.Sum(x => x.ThanhTien) * 100 / tongXuat)
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

        private List<GiaoDichLogRaw> DocLogGiaoDichTrongKy(DateTime tuNgay, DateTime denNgay, string loaiKhoFilter)
        {
            var list = new List<GiaoDichLogRaw>();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "lichsu_nhapxuat_meta.txt");
                if (!System.IO.File.Exists(filePath)) return list;

                var lines = System.IO.File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var p = line.Split('|');
                    if (p.Length >= 11)
                    {
                        int.TryParse(p[6], out int sl);
                        decimal.TryParse(p[7], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal gia);
                        DateTime.TryParse(p[10], out DateTime tg);

                        if (tg >= tuNgay && tg <= denNgay)
                        {
                            if (string.IsNullOrEmpty(loaiKhoFilter) || loaiKhoFilter == "ALL" || p[4] == loaiKhoFilter)
                            {
                                list.Add(new GiaoDichLogRaw
                                {
                                    LoaiGiaoDich = p[1],
                                    MaVatTu = p[2],
                                    LoaiKho = p[4],
                                    SoLuong = sl,
                                    DonGia = gia,
                                    DoiTacHoacBoPhan = p[8],
                                    ThoiGian = tg
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            return list;
        }
    }
}