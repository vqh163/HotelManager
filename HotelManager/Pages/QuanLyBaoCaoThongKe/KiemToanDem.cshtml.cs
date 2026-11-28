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
    public class KiemToanDemModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public KiemToanDemModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Cảnh báo tồn đọng cần rà soát trước khi chốt kiểm toán đêm
        public class AuditWarningItem
        {
            public string LoaiCanhBao { get; set; } = string.Empty;
            public string MucDo { get; set; } = "warning"; // "danger" hoặc "warning"
            public string MaChungTu { get; set; } = string.Empty;
            public string DoiTuong { get; set; } = string.Empty;
            public string MoTa { get; set; } = string.Empty;
            public decimal SoTienLienQuan { get; set; }
        }

        // Dòng bút toán định khoản chuẩn kế toán để đồng bộ MISA / FAST / SAP
        public class AccountingVoucherRow
        {
            public string SoChungTu { get; set; } = string.Empty;
            public DateTime NgayHachToan { get; set; }
            public string PhanHe { get; set; } = string.Empty;
            public string DoiTuong { get; set; } = string.Empty;
            public string DienGiai { get; set; } = string.Empty;
            public string TaiKhoanNo { get; set; } = string.Empty;
            public string TaiKhoanCoDoanhThu { get; set; } = "5113";
            public string TaiKhoanCoThue { get; set; } = "3331";
            public decimal TienTruocThue { get; set; }
            public decimal TienThueVAT { get; set; }
            public decimal GiamGia { get; set; }
            public decimal TongCong { get; set; }
            public decimal DaThu { get; set; }
            public decimal ConNo { get; set; }
            public string TrangThai { get; set; } = string.Empty;
        }

        // --- THAM SỐ KIỂM TOÁN ĐÊM ---
        [BindProperty(SupportsGet = true)]
        public DateTime? NgayKiemToan { get; set; }

        [BindProperty]
        public string? GhiChuKiemToan { get; set; }

        // --- THAM SỐ ĐỒNG BỘ KẾ TOÁN ---
        [BindProperty(SupportsGet = true)]
        public DateTime? TuNgayKeToan { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DenNgayKeToan { get; set; }

        [BindProperty(SupportsGet = true)]
        public string PhanMemKeToan { get; set; } = "MISA AMIS / SME";

        [BindProperty(SupportsGet = true)]
        public string TabActive { get; set; } = "audit"; // "audit" hoặc "accounting"

        [TempData]
        public string? ThongBao { get; set; }

        // --- SỐ LIỆU KIỂM TOÁN ĐÊM ---
        public bool DaChotSoNgayNay { get; set; }
        public NHAT_KY_HE_THONG? BanGhiChotGanNhat { get; set; }
        public int TongSoPhong { get; set; }
        public int SoPhongDangLuuTru { get; set; }
        public decimal CongSuatNgay { get; set; }
        public int SoLuotCheckInNgay { get; set; }
        public int SoLuotCheckOutNgay { get; set; }
        public decimal DoanhThuPhongNgay { get; set; }
        public decimal DoanhThuNhaHangNgay { get; set; }
        public decimal TongDoanhThuNgay => DoanhThuPhongNgay + DoanhThuNhaHangNgay;
        public decimal ThucThuTrongNgay { get; set; }
        public decimal CongNoPhatSinhNgay { get; set; }
        public List<AuditWarningItem> DanhSachCanhBao { get; set; } = new();

        // --- SỐ LIỆU ĐỒNG BỘ KẾ TOÁN ---
        public List<AccountingVoucherRow> DanhSachButToan { get; set; } = new();
        public decimal TongTienTruocThueKT { get; set; }
        public decimal TongThueVATKT { get; set; }
        public decimal TongCongChungTuKT { get; set; }
        public decimal TongDaThuKT { get; set; }
        public decimal TongConNoKT { get; set; }

        // --- LỊCH SỬ TỪ BẢNG NHAT_KY_HE_THONG ---
        public List<NHAT_KY_HE_THONG> LichSuKiemToanVaDongBo { get; set; } = new();

        public async Task OnGetAsync()
        {
            ChuanHoaNgay();
            await LoadAllDataAsync();
        }

        // 1. CHỐT KIỂM TOÁN ĐÊM -> LƯU BIÊN BẢN VÀO NHAT_KY_HE_THONG
        public async Task<IActionResult> OnPostChotKiemToanDemAsync()
        {
            ChuanHoaNgay();
            await LoadAllDataAsync();

            DateTime ngay = NgayKiemToan!.Value.Date;
            string nguoiThucHien = Truncate(User.Identity?.Name ?? "quanly", 50);

            string chiTiet =
                $"Ngày chốt: {ngay:dd/MM/yyyy} | " +
                $"Công suất: {SoPhongDangLuuTru}/{TongSoPhong} phòng ({CongSuatNgay:0.#}%) | " +
                $"Check-in: {SoLuotCheckInNgay}, Check-out: {SoLuotCheckOutNgay} | " +
                $"DT Phòng & DV: {DoanhThuPhongNgay:N0}đ, DT Nhà hàng: {DoanhThuNhaHangNgay:N0}đ, Tổng DT: {TongDoanhThuNgay:N0}đ | " +
                $"Thực thu: {ThucThuTrongNgay:N0}đ, Công nợ: {CongNoPhatSinhNgay:N0}đ | " +
                $"Cảnh báo tồn đọng: {DanhSachCanhBao.Count}" +
                (string.IsNullOrWhiteSpace(GhiChuKiemToan) ? "" : $" | Ghi chú: {GhiChuKiemToan.Trim()}");

            var log = new NHAT_KY_HE_THONG
            {
                MaNhatKy = $"NA{DateTime.Now:yyMMddHHmmss}{Random.Shared.Next(10, 99)}", // 16 ký tự <= varchar(20)
                TenDangNhap = nguoiThucHien,
                HanhDong = $"KIEM_TOAN_DEM - Chốt sổ ngày {ngay:dd/MM/yyyy}",
                ThoiGian = DateTime.Now,
                ChiTiet = chiTiet
            };

            _context.NHAT_KY_HE_THONG.Add(log);
            await _context.SaveChangesAsync();

            ThongBao = $"Đã chốt Kiểm toán đêm ngày {ngay:dd/MM/yyyy} thành công (Mã biên bản: {log.MaNhatKy}).";
            return RedirectToPage(new
            {
                NgayKiemToan = ngay.ToString("yyyy-MM-dd"),
                TuNgayKeToan = TuNgayKeToan?.ToString("yyyy-MM-dd"),
                DenNgayKeToan = DenNgayKeToan?.ToString("yyyy-MM-dd"),
                PhanMemKeToan,
                TabActive = "audit"
            });
        }

        // 2. ĐỒNG BỘ DỮ LIỆU SANG PHẦN MỀM KẾ TOÁN -> GHI NHẬN VÀO NHAT_KY_HE_THONG
        public async Task<IActionResult> OnPostDongBoKeToanAsync()
        {
            ChuanHoaNgay();
            await LoadAllDataAsync();

            if (!DanhSachButToan.Any())
            {
                ThongBao = "Không có chứng từ nào trong kỳ đã chọn để đồng bộ sang phần mềm kế toán.";
                return RedirectToPage(new
                {
                    NgayKiemToan = NgayKiemToan?.ToString("yyyy-MM-dd"),
                    TuNgayKeToan = TuNgayKeToan?.ToString("yyyy-MM-dd"),
                    DenNgayKeToan = DenNgayKeToan?.ToString("yyyy-MM-dd"),
                    PhanMemKeToan,
                    TabActive = "accounting"
                });
            }

            string nguoiThucHien = Truncate(User.Identity?.Name ?? "ketoan", 50);
            string maGoi = $"KT{DateTime.Now:yyMMddHHmmss}{Random.Shared.Next(10, 99)}"; // 16 ký tự <= varchar(20)

            string chiTiet =
                $"Phần mềm đích: {PhanMemKeToan} | " +
                $"Kỳ chứng từ: {TuNgayKeToan:dd/MM/yyyy} - {DenNgayKeToan:dd/MM/yyyy} | " +
                $"Số bút toán: {DanhSachButToan.Count} | " +
                $"Doanh thu trước thuế (TK 5113): {TongTienTruocThueKT:N0}đ | " +
                $"Thuế VAT (TK 3331): {TongThueVATKT:N0}đ | " +
                $"Tổng giá trị: {TongCongChungTuKT:N0}đ | " +
                $"Đã thu (TK 111/112): {TongDaThuKT:N0}đ | Công nợ (TK 131): {TongConNoKT:N0}đ";

            var log = new NHAT_KY_HE_THONG
            {
                MaNhatKy = maGoi,
                TenDangNhap = nguoiThucHien,
                HanhDong = Truncate($"DONG_BO_KE_TOAN - {PhanMemKeToan} ({TuNgayKeToan:dd/MM}-{DenNgayKeToan:dd/MM/yyyy})", 255),
                ThoiGian = DateTime.Now,
                ChiTiet = chiTiet
            };

            _context.NHAT_KY_HE_THONG.Add(log);
            await _context.SaveChangesAsync();

            ThongBao = $"Đã đồng bộ {DanhSachButToan.Count} bút toán sang {PhanMemKeToan} thành công (Mã gói: {maGoi}). Bạn có thể tải thêm file CSV/JSON để lưu trữ hoặc import ngoại tuyến.";
            return RedirectToPage(new
            {
                NgayKiemToan = NgayKiemToan?.ToString("yyyy-MM-dd"),
                TuNgayKeToan = TuNgayKeToan?.ToString("yyyy-MM-dd"),
                DenNgayKeToan = DenNgayKeToan?.ToString("yyyy-MM-dd"),
                PhanMemKeToan,
                TabActive = "accounting"
            });
        }

        // 3. XUẤT GÓI BÚT TOÁN ĐỊNH KHOẢN CHUẨN CSV (IMPORT MISA / FAST / EXCEL)
        public async Task<IActionResult> OnGetExportKeToanCsvAsync()
        {
            ChuanHoaNgay();
            await LoadAllDataAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Số chứng từ,Ngày hạch toán,Phân hệ,Đối tượng,Diễn giải,TK Nợ,TK Có Doanh thu,TK Có Thuế GTGT,Doanh thu chưa thuế,Thuế GTGT (10%),Giảm giá,Tổng thanh toán,Tiền đã thu,Công nợ (TK 131),Trạng thái");

            foreach (var row in DanhSachButToan)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(row.SoChungTu),
                    row.NgayHachToan.ToString("dd/MM/yyyy"),
                    EscapeCsv(row.PhanHe),
                    EscapeCsv(row.DoiTuong),
                    EscapeCsv(row.DienGiai),
                    EscapeCsv(row.TaiKhoanNo),
                    EscapeCsv(row.TaiKhoanCoDoanhThu),
                    EscapeCsv(row.TaiKhoanCoThue),
                    row.TienTruocThue,
                    row.TienThueVAT,
                    row.GiamGia,
                    row.TongCong,
                    row.DaThu,
                    row.ConNo,
                    EscapeCsv(row.TrangThai)
                ));
            }

            byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"DongBoKeToan_{TuNgayKeToan:yyyyMMdd}_{DenNgayKeToan:yyyyMMdd}.csv");
        }

        // 4. XUẤT GÓI DỮ LIỆU CHUẨN JSON (MÔ PHỎNG API PAYLOAD ĐỒNG BỘ ERP)
        public async Task<IActionResult> OnGetExportKeToanJsonAsync()
        {
            ChuanHoaNgay();
            await LoadAllDataAsync();

            var payload = new
            {
                HeThongNguon = "Grand Horizon Hotel PMS",
                PhanMemDich = PhanMemKeToan,
                ThoiGianXuat = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                KyKeToan = new
                {
                    TuNgay = TuNgayKeToan?.ToString("yyyy-MM-dd"),
                    DenNgay = DenNgayKeToan?.ToString("yyyy-MM-dd")
                },
                TongHop = new
                {
                    SoLuongChungTu = DanhSachButToan.Count,
                    DoanhThuTruocThue = TongTienTruocThueKT,
                    ThueVAT = TongThueVATKT,
                    TongThanhToan = TongCongChungTuKT,
                    DaThu = TongDaThuKT,
                    CongNo = TongConNoKT
                },
                DanhSachButToan
            };

            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json; charset=utf-8", $"AccountingSync_{TuNgayKeToan:yyyyMMdd}_{DenNgayKeToan:yyyyMMdd}.json");
        }

        private void ChuanHoaNgay()
        {
            NgayKiemToan ??= DateTime.Today;
            TuNgayKeToan ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DenNgayKeToan ??= DateTime.Today;
            if (DenNgayKeToan < TuNgayKeToan) DenNgayKeToan = TuNgayKeToan;
        }

        private async Task LoadAllDataAsync()
        {
            var dsPhong = await _context.PHONG.AsNoTracking().ToListAsync();
            var dsDatPhong = await _context.DAT_PHONG.AsNoTracking().ToListAsync();
            var dsChiTietDP = await _context.CHI_TIET_DAT_PHONG.AsNoTracking().ToListAsync();
            var dsKhachHang = await _context.KHACH_HANG.AsNoTracking().ToDictionaryAsync(k => k.MaKH);
            var dsHoaDon = await _context.HOA_DON.AsNoTracking().ToListAsync();
            var dsHoaDonNH = await _context.HOA_DON_NHA_HANG.AsNoTracking().ToListAsync();
            var dsBanNH = await _context.BAN_NHA_HANG.AsNoTracking().ToDictionaryAsync(b => b.MaBan);
            var dsThanhToan = await _context.THANH_TOAN.AsNoTracking().ToListAsync();
            var dsNhatKy = await _context.NHAT_KY_HE_THONG
                .AsNoTracking()
                .Where(n => n.HanhDong.StartsWith("KIEM_TOAN_DEM") || n.HanhDong.StartsWith("DONG_BO_KE_TOAN"))
                .OrderByDescending(n => n.ThoiGian)
                .Take(30)
                .ToListAsync();

            var phongMap = dsPhong.ToDictionary(p => p.MaPhong);
            LichSuKiemToanVaDongBo = dsNhatKy;

            // =========================================================================
            // PHẦN A: TÍNH TOÁN DỮ LIỆU KIỂM TOÁN ĐÊM (NIGHT AUDIT) CHO NGÀY ĐÃ CHỌN
            // =========================================================================
            DateTime ngayAudit = NgayKiemToan!.Value.Date;
            DateTime cuoiNgayAudit = ngayAudit.AddDays(1).AddTicks(-1);
            string chuoiNgayAudit = ngayAudit.ToString("dd/MM/yyyy");

            BanGhiChotGanNhat = dsNhatKy.FirstOrDefault(n =>
                n.HanhDong.StartsWith("KIEM_TOAN_DEM") &&
                n.HanhDong.Contains(chuoiNgayAudit));
            DaChotSoNgayNay = BanGhiChotGanNhat != null;

            TongSoPhong = dsPhong.Count;
            var phongLuuTruNgay = new HashSet<string>();
            DanhSachCanhBao = new List<AuditWarningItem>();

            foreach (var dp in dsDatPhong)
            {
                string tt = dp.TrangThaiDatPhong ?? "";
                if (tt.Contains("Hủy", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool daTraPhong =
                    tt.Contains("check-out", StringComparison.OrdinalIgnoreCase) ||
                    tt.Contains("trả phòng", StringComparison.OrdinalIgnoreCase) ||
                    tt.Contains("hoàn tất", StringComparison.OrdinalIgnoreCase);

                string tenKhach = (!string.IsNullOrEmpty(dp.KHACH_HANGMaKH) && dsKhachHang.TryGetValue(dp.KHACH_HANGMaKH, out var kh))
                    ? (kh.HoTen ?? "Khách lẻ")
                    : "Khách lẻ";

                var chiTiets = dsChiTietDP.Where(c => c.DAT_PHONGMaDatPhong == dp.MaDatPhong).ToList();
                var tenPhongList = chiTiets
                    .Where(c => !string.IsNullOrEmpty(c.PHONGMaPhong) && phongMap.ContainsKey(c.PHONGMaPhong))
                    .Select(c => phongMap[c.PHONGMaPhong!].SoPhong ?? c.PHONGMaPhong!)
                    .Distinct()
                    .ToList();
                string chuoiPhong = tenPhongList.Any() ? "Phòng " + string.Join(", ", tenPhongList) : "Chưa gán phòng";

                // Đếm Check-in / Check-out trong ngày kiểm toán
                if (dp.NgayNhan.HasValue && dp.NgayNhan.Value.Date == ngayAudit)
                    SoLuotCheckInNgay++;

                if (dp.NgayTra.HasValue && dp.NgayTra.Value.Date == ngayAudit)
                    SoLuotCheckOutNgay++;

                // Đếm phòng đang có khách trong đêm kiểm toán
                if (dp.NgayNhan.HasValue && dp.NgayNhan.Value.Date <= ngayAudit &&
                    (!dp.NgayTra.HasValue || dp.NgayTra.Value.Date > ngayAudit || !daTraPhong))
                {
                    if (!tt.Contains("Chờ", StringComparison.OrdinalIgnoreCase) && !daTraPhong)
                    {
                        foreach (var ct in chiTiets)
                        {
                            if (!string.IsNullOrEmpty(ct.PHONGMaPhong))
                                phongLuuTruNgay.Add(ct.PHONGMaPhong);
                        }
                    }
                }

                // Cảnh báo 1: Đến hạn Check-out (<= ngày kiểm toán) nhưng chưa làm thủ tục trả phòng
                if (dp.NgayTra.HasValue && dp.NgayTra.Value.Date <= ngayAudit && !daTraPhong &&
                    (tt.Contains("Đang", StringComparison.OrdinalIgnoreCase) || tt.Contains("Check-in", StringComparison.OrdinalIgnoreCase) || tt.Contains("nhận", StringComparison.OrdinalIgnoreCase)))
                {
                    DanhSachCanhBao.Add(new AuditWarningItem
                    {
                        LoaiCanhBao = "Quá hạn Check-out",
                        MucDo = "danger",
                        MaChungTu = dp.MaDatPhong,
                        DoiTuong = $"{tenKhach} ({chuoiPhong})",
                        MoTa = $"Hẹn trả phòng {dp.NgayTra:dd/MM/yyyy} nhưng trạng thái vẫn là \"{tt}\".",
                        SoTienLienQuan = dp.TienCoc ?? 0m
                    });
                }

                // Cảnh báo 2: Đơn chờ nhận phòng (No-show) quá ngày kiểm toán
                if (dp.NgayNhan.HasValue && dp.NgayNhan.Value.Date <= ngayAudit &&
                    (tt.Contains("Chờ", StringComparison.OrdinalIgnoreCase) || string.Equals(tt, "Đã đặt", StringComparison.OrdinalIgnoreCase)))
                {
                    DanhSachCanhBao.Add(new AuditWarningItem
                    {
                        LoaiCanhBao = "Chưa Check-in / No-show",
                        MucDo = "warning",
                        MaChungTu = dp.MaDatPhong,
                        DoiTuong = $"{tenKhach} ({chuoiPhong})",
                        MoTa = $"Lịch nhận phòng {dp.NgayNhan:dd/MM/yyyy}, cần xác nhận khách đến hoặc hủy No-show.",
                        SoTienLienQuan = dp.TienCoc ?? 0m
                    });
                }
            }

            SoPhongDangLuuTru = Math.Min(TongSoPhong, phongLuuTruNgay.Count);
            CongSuatNgay = TongSoPhong > 0 ? Math.Round((decimal)SoPhongDangLuuTru * 100m / TongSoPhong, 1) : 0m;

            // Cảnh báo 3 & Doanh thu Lễ tân trong ngày kiểm toán
            var maDpDaGopNH = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hd in dsHoaDon)
            {
                DateTime? ngayLap = hd.NgayLap;
                decimal tongHD = hd.TongTienThanhToan
                    ?? Math.Max(0m, (hd.TienPhong ?? 0m) + (hd.TienDichVu ?? 0m) + (hd.TienNhaHang ?? 0m) + (hd.TienThueVAT ?? 0m) - (hd.TienGiamGia ?? 0m));

                decimal daThuHD = dsThanhToan
                    .Where(t => t.HOA_DONMaHoaDon == hd.MaHoaDon && t.TrangThaiGiaoDich != "Thất bại")
                    .Sum(t => t.SoTien ?? 0m);

                bool daTT = string.Equals(hd.TrangThaiThanhToan, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);
                if (daThuHD == 0 && daTT) daThuHD = tongHD;
                decimal conNoHD = Math.Max(0m, tongHD - daThuHD);

                if ((hd.TienNhaHang ?? 0m) > 0 && !string.IsNullOrEmpty(hd.DAT_PHONGMaDatPhong))
                    maDpDaGopNH.Add(hd.DAT_PHONGMaDatPhong);

                // Hóa đơn chưa tất toán tính đến hết ngày kiểm toán
                if ((!ngayLap.HasValue || ngayLap.Value <= cuoiNgayAudit) && (!daTT || conNoHD > 0))
                {
                    DanhSachCanhBao.Add(new AuditWarningItem
                    {
                        LoaiCanhBao = "Hóa đơn phòng chưa tất toán",
                        MucDo = "danger",
                        MaChungTu = hd.MaHoaDon,
                        DoiTuong = $"Booking #{hd.DAT_PHONGMaDatPhong}",
                        MoTa = $"Trạng thái: \"{hd.TrangThaiThanhToan ?? "Chưa thanh toán"}\" — Còn thiếu {conNoHD:N0} ₫.",
                        SoTienLienQuan = conNoHD
                    });
                }

                // Doanh thu phát sinh đúng ngày kiểm toán
                if (ngayLap.HasValue && ngayLap.Value.Date == ngayAudit)
                {
                    DoanhThuPhongNgay += tongHD;
                    ThucThuTrongNgay += daThuHD;
                    CongNoPhatSinhNgay += conNoHD;
                }
            }

            // Cảnh báo 4 & Doanh thu Nhà hàng trong ngày kiểm toán
            foreach (var hdNH in dsHoaDonNH)
            {
                DateTime? tgTao = hdNH.ThoiGianTao;
                string httt = hdNH.HinhThucThanhToan ?? "";

                if (httt.Contains("Hủy", StringComparison.OrdinalIgnoreCase))
                    continue;

                decimal tamTinh = TinhTamTinhTuJson(hdNH.DanhSachMonJson, hdNH.TongTienMonAn ?? 0m);
                decimal giamGia = hdNH.GiamGia ?? 0m;
                decimal tongNH = (hdNH.ThanhTien.HasValue && hdNH.ThanhTien.Value > 0)
                    ? hdNH.ThanhTien.Value
                    : Math.Max(0m, tamTinh + Math.Round(tamTinh * 0.15m, 0) - giamGia);

                bool chuaTT = string.IsNullOrWhiteSpace(httt) ||
                              httt.Contains("Chưa", StringComparison.OrdinalIgnoreCase) ||
                              httt.Contains("Ghi nợ", StringComparison.OrdinalIgnoreCase);

                if (chuaTT)
                {
                    if (!tgTao.HasValue || tgTao.Value <= cuoiNgayAudit)
                    {
                        DanhSachCanhBao.Add(new AuditWarningItem
                        {
                            LoaiCanhBao = "Hóa đơn Nhà hàng chưa chốt",
                            MucDo = "warning",
                            MaChungTu = hdNH.MaHoaDonNhaHang,
                            DoiTuong = $"Bàn {hdNH.BAN_NHA_HANGMaBan ?? "Tại quầy"}",
                            MoTa = $"Chưa thu tiền tại Quầy thu ngân (trạng thái: \"{(string.IsNullOrWhiteSpace(httt) ? "Chưa thanh toán" : httt)}\").",
                            SoTienLienQuan = tongNH
                        });
                    }
                    continue;
                }

                if (!string.IsNullOrEmpty(hdNH.DAT_PHONGMaDatPhong) && maDpDaGopNH.Contains(hdNH.DAT_PHONGMaDatPhong))
                    continue;

                var gd = dsThanhToan
                    .Where(t => t.HOA_DON_NHA_HANGMaHoaDonNhaHang == hdNH.MaHoaDonNhaHang && t.TrangThaiGiaoDich != "Thất bại")
                    .OrderByDescending(t => t.ThoiGianGiaoDich)
                    .FirstOrDefault();

                DateTime? ngayTT = gd?.ThoiGianGiaoDich ?? tgTao;
                if (ngayTT.HasValue && ngayTT.Value.Date == ngayAudit)
                {
                    decimal daThu = (gd?.SoTien ?? 0m) > 0 ? gd!.SoTien!.Value : tongNH;
                    DoanhThuNhaHangNgay += tongNH;
                    ThucThuTrongNgay += daThu;
                    CongNoPhatSinhNgay += Math.Max(0m, tongNH - daThu);
                }
            }

            // =========================================================================
            // PHẦN B: TỔNG HỢP BÚT TOÁN ĐỊNH KHOẢN ĐỂ ĐỒNG BỘ PHẦN MỀM KẾ TOÁN
            // =========================================================================
            DateTime tuKT = TuNgayKeToan!.Value.Date;
            DateTime denKT = DenNgayKeToan!.Value.Date.AddDays(1).AddTicks(-1);
            DanhSachButToan = new List<AccountingVoucherRow>();

            // 1. Chứng từ từ HOA_DON (Lễ tân)
            foreach (var hd in dsHoaDon)
            {
                DateTime? ngayLap = hd.NgayLap;
                if (!ngayLap.HasValue || ngayLap.Value < tuKT || ngayLap.Value > denKT)
                    continue;

                var dp = dsDatPhong.FirstOrDefault(d => d.MaDatPhong == hd.DAT_PHONGMaDatPhong);
                string tenKhach = (dp != null && !string.IsNullOrEmpty(dp.KHACH_HANGMaKH) && dsKhachHang.TryGetValue(dp.KHACH_HANGMaKH, out var kh))
                    ? (kh.HoTen ?? "Khách lẻ")
                    : "Khách lưu trú";

                decimal doanhThuGoc = (hd.TienPhong ?? 0m) + (hd.TienDichVu ?? 0m) + (hd.TienNhaHang ?? 0m);
                decimal giamGia = hd.TienGiamGia ?? 0m;
                decimal tienTruocThue = Math.Max(0m, doanhThuGoc - giamGia);
                decimal thueVat = hd.TienThueVAT ?? Math.Round(tienTruocThue * 0.10m, 0);
                decimal tongCong = hd.TongTienThanhToan ?? (tienTruocThue + thueVat);

                decimal daThu = dsThanhToan
                    .Where(t => t.HOA_DONMaHoaDon == hd.MaHoaDon && t.TrangThaiGiaoDich != "Thất bại")
                    .Sum(t => t.SoTien ?? 0m);

                bool daTT = string.Equals(hd.TrangThaiThanhToan, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);
                if (daThu == 0 && daTT) daThu = tongCong;
                decimal conNo = Math.Max(0m, tongCong - daThu);

                DanhSachButToan.Add(new AccountingVoucherRow
                {
                    SoChungTu = hd.MaHoaDon,
                    NgayHachToan = ngayLap.Value,
                    PhanHe = "Lễ tân",
                    DoiTuong = tenKhach,
                    DienGiai = $"Doanh thu lưu trú & dịch vụ — Booking #{hd.DAT_PHONGMaDatPhong}",
                    TaiKhoanNo = conNo == 0 ? "111/112" : (daThu > 0 ? "111/131" : "131"),
                    TaiKhoanCoDoanhThu = "5113",
                    TaiKhoanCoThue = "3331",
                    TienTruocThue = tienTruocThue,
                    TienThueVAT = thueVat,
                    GiamGia = giamGia,
                    TongCong = tongCong,
                    DaThu = daThu,
                    ConNo = conNo,
                    TrangThai = conNo == 0 ? "Đã thanh toán" : "Còn công nợ"
                });
            }

            // 2. Chứng từ từ HOA_DON_NHA_HANG (Nhà hàng đã thanh toán độc lập)
            foreach (var hdNH in dsHoaDonNH)
            {
                string httt = hdNH.HinhThucThanhToan ?? "";
                if (string.IsNullOrWhiteSpace(httt) ||
                    httt.Contains("Chưa", StringComparison.OrdinalIgnoreCase) ||
                    httt.Contains("Hủy", StringComparison.OrdinalIgnoreCase) ||
                    httt.Contains("Ghi nợ", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(hdNH.DAT_PHONGMaDatPhong) && maDpDaGopNH.Contains(hdNH.DAT_PHONGMaDatPhong))
                    continue;

                var gd = dsThanhToan
                    .Where(t => t.HOA_DON_NHA_HANGMaHoaDonNhaHang == hdNH.MaHoaDonNhaHang && t.TrangThaiGiaoDich != "Thất bại")
                    .OrderByDescending(t => t.ThoiGianGiaoDich)
                    .FirstOrDefault();

                DateTime? ngayHT = gd?.ThoiGianGiaoDich ?? hdNH.ThoiGianTao;
                if (!ngayHT.HasValue || ngayHT.Value < tuKT || ngayHT.Value > denKT)
                    continue;

                decimal tamTinh = TinhTamTinhTuJson(hdNH.DanhSachMonJson, hdNH.TongTienMonAn ?? 0m);
                decimal giamGia = hdNH.GiamGia ?? 0m;
                decimal tongCong = (hdNH.ThanhTien.HasValue && hdNH.ThanhTien.Value > 0)
                    ? hdNH.ThanhTien.Value
                    : Math.Max(0m, tamTinh + Math.Round(tamTinh * 0.15m, 0) - giamGia);

                decimal tienTruocThue = Math.Round(tongCong / 1.10m, 0);
                decimal thueVat = Math.Max(0m, tongCong - tienTruocThue);
                decimal daThu = (gd?.SoTien ?? 0m) > 0 ? gd!.SoTien!.Value : tongCong;
                decimal conNo = Math.Max(0m, tongCong - daThu);

                string tenBan = "Tại quầy";
                if (!string.IsNullOrWhiteSpace(hdNH.BAN_NHA_HANGMaBan))
                {
                    var banList = hdNH.BAN_NHA_HANGMaBan.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => dsBanNH.TryGetValue(m.Trim(), out var b) ? (b.TenBan ?? m.Trim()) : m.Trim());
                    tenBan = string.Join(", ", banList);
                }

                DanhSachButToan.Add(new AccountingVoucherRow
                {
                    SoChungTu = hdNH.MaHoaDonNhaHang,
                    NgayHachToan = ngayHT.Value,
                    PhanHe = "Nhà hàng",
                    DoiTuong = $"Khách bàn {tenBan}",
                    DienGiai = $"Doanh thu ăn uống nhà hàng — {tenBan}",
                    TaiKhoanNo = conNo == 0 ? "111/112" : "131",
                    TaiKhoanCoDoanhThu = "5113",
                    TaiKhoanCoThue = "3331",
                    TienTruocThue = tienTruocThue,
                    TienThueVAT = thueVat,
                    GiamGia = giamGia,
                    TongCong = tongCong,
                    DaThu = daThu,
                    ConNo = conNo,
                    TrangThai = conNo == 0 ? "Đã thanh toán" : "Còn công nợ"
                });
            }

            DanhSachButToan = DanhSachButToan.OrderByDescending(x => x.NgayHachToan).ToList();

            TongTienTruocThueKT = DanhSachButToan.Sum(x => x.TienTruocThue);
            TongThueVATKT = DanhSachButToan.Sum(x => x.TienThueVAT);
            TongCongChungTuKT = DanhSachButToan.Sum(x => x.TongCong);
            TongDaThuKT = DanhSachButToan.Sum(x => x.DaThu);
            TongConNoKT = DanhSachButToan.Sum(x => x.ConNo);
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

        private static string Truncate(string val, int maxLen) =>
            val.Length <= maxLen ? val : val.Substring(0, maxLen);

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
    }
}