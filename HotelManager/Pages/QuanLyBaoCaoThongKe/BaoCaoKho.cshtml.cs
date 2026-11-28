using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelManager.Data;

namespace HotelManager.Pages.QuanLyBaoCaoThongKe
{
    public class BaoCaoKhoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BaoCaoKhoModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public class KhoReportItem
        {
            public string MaVatTu { get; set; } = string.Empty;
            public string TenVatTu { get; set; } = string.Empty;
            public string DanhMuc { get; set; } = string.Empty;
            public string DonViTinh { get; set; } = string.Empty;
            public int SoLuongTon { get; set; }
            public int SoLuongTonToiThieu { get; set; }
            public decimal DonGiaNhap { get; set; }
            public decimal ThanhTienTon => SoLuongTon * DonGiaNhap;
            public string TrangThaiTon { get; set; } = string.Empty;
        }

        // --- THAM SỐ LỌC ---
        [BindProperty(SupportsGet = true)]
        public string? DanhMucLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string NhomTheo { get; set; } = "DanhMuc"; // "DanhMuc", "TrangThai", "KhongNhom"

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "ton_asc";

        // --- DỮ LIỆU VIEW ---
        public List<KhoReportItem> DanhSachBaoCao { get; set; } = new();
        public List<IGrouping<string, KhoReportItem>> DuLieuTheoNhom { get; set; } = new();
        public List<string> DanhSachDanhMuc { get; set; } = new();
        public List<string> LichSuKiemKe { get; set; } = new();

        // --- KPI & TỔNG CỘNG ---
        public int TongMatHang { get; set; }
        public int TongSoLuongTon { get; set; }
        public decimal TongGiaTriTon { get; set; }
        public int SoMatHangSapHet { get; set; }
        public int SoMatHangHetHang { get; set; }

        public async Task OnGetAsync()
        {
            await LoadKhoDataAsync();
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            await LoadKhoDataAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Mã vật tư,Tên vật tư,Danh mục,Đơn vị tính,Số lượng tồn,Định mức tối thiểu,Đơn giá nhập (VNĐ),Thành tiền tồn (VNĐ),Trạng thái");

            foreach (var item in DanhSachBaoCao)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(item.MaVatTu),
                    EscapeCsv(item.TenVatTu),
                    EscapeCsv(item.DanhMuc),
                    EscapeCsv(item.DonViTinh),
                    item.SoLuongTon,
                    item.SoLuongTonToiThieu,
                    item.DonGiaNhap,
                    item.ThanhTienTon,
                    EscapeCsv(item.TrangThaiTon)
                ));
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"BaoCaoKho_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        private async Task LoadKhoDataAsync()
        {
            var dsVatTuDb = await _context.KHO_VAT_TU.AsNoTracking().ToListAsync();

            var allItems = dsVatTuDb.Select(v =>
            {
                int slTon = v.SoLuongTon ?? 0;
                int slToiThieu = v.SoLuongTonToiThieu ?? 10;

                string trangThai = slTon <= 0
                    ? "Hết hàng"
                    : slTon <= slToiThieu
                        ? "Sắp hết"
                        : "Đủ hàng";

                return new KhoReportItem
                {
                    MaVatTu = v.MaVatTu ?? "",
                    TenVatTu = string.IsNullOrWhiteSpace(v.TenVatTu) ? "Chưa đặt tên" : v.TenVatTu.Trim(),
                    DanhMuc = string.IsNullOrWhiteSpace(v.LoaiVatTu) ? "Vật tư chung" : v.LoaiVatTu.Trim(),
                    DonViTinh = string.IsNullOrWhiteSpace(v.DonViTinh) ? "Cái" : v.DonViTinh.Trim(),
                    SoLuongTon = slTon,
                    SoLuongTonToiThieu = slToiThieu,
                    DonGiaNhap = v.DonGiaNhap ?? 0m,
                    TrangThaiTon = trangThai
                };
            }).ToList();

            DanhSachDanhMuc = allItems
                .Select(x => x.DanhMuc)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Áp dụng bộ lọc
            IEnumerable<KhoReportItem> query = allItems;

            if (!string.IsNullOrWhiteSpace(DanhMucLoc))
                query = query.Where(x => x.DanhMuc == DanhMucLoc);

            if (!string.IsNullOrWhiteSpace(TrangThaiLoc))
                query = query.Where(x => x.TrangThaiTon == TrangThaiLoc);

            if (!string.IsNullOrWhiteSpace(TuKhoa))
            {
                string kw = TuKhoa.Trim().ToLower();
                query = query.Where(x =>
                    x.MaVatTu.ToLower().Contains(kw) ||
                    x.TenVatTu.ToLower().Contains(kw) ||
                    x.DanhMuc.ToLower().Contains(kw));
            }

            query = SortBy switch
            {
                "ton_desc" => query.OrderByDescending(x => x.SoLuongTon).ThenBy(x => x.TenVatTu),
                "giatri_desc" => query.OrderByDescending(x => x.ThanhTienTon),
                "ten_asc" => query.OrderBy(x => x.TenVatTu),
                _ => query.OrderBy(x => x.SoLuongTon).ThenBy(x => x.TenVatTu)
            };

            DanhSachBaoCao = query.ToList();

            DuLieuTheoNhom = NhomTheo switch
            {
                "TrangThai" => DanhSachBaoCao.GroupBy(x => x.TrangThaiTon).ToList(),
                "KhongNhom" => DanhSachBaoCao.GroupBy(x => "Tất cả").ToList(),
                _ => DanhSachBaoCao.GroupBy(x => x.DanhMuc).ToList()
            };

            // Tính KPI & Tổng cộng
            TongMatHang = DanhSachBaoCao.Count;
            TongSoLuongTon = DanhSachBaoCao.Sum(x => x.SoLuongTon);
            TongGiaTriTon = DanhSachBaoCao.Sum(x => x.ThanhTienTon);
            SoMatHangSapHet = DanhSachBaoCao.Count(x => x.TrangThaiTon == "Sắp hết");
            SoMatHangHetHang = DanhSachBaoCao.Count(x => x.TrangThaiTon == "Hết hàng");

            // Đọc nhật ký kiểm kê nếu có file lichsu_kiemke_meta.txt
            string metaPath = Path.Combine(_env.ContentRootPath, "lichsu_kiemke_meta.txt");
            if (System.IO.File.Exists(metaPath))
            {
                LichSuKiemKe = (await System.IO.File.ReadAllLinesAsync(metaPath))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Reverse()
                    .ToList();
            }
        }

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
    }
}