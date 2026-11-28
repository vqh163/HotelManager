using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.ThuNgan
{
    public class ChiTietMonViewModel
    {
        public string TenMon { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class HoaDonViewModel
    {
        public string STT { get; set; } = string.Empty;
        public string MaHoaDonNhaHang { get; set; } = string.Empty;
        public string MaBan { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;
        public string HinhThucThanhToan { get; set; } = string.Empty;
        public string DanhSachMonJson { get; set; } = string.Empty;
        public decimal TamTinh { get; set; }
        public decimal PhiPhucVu { get; set; }
        public decimal ThueVAT { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongCong { get; set; }
        public string GioTao { get; set; } = string.Empty;
        public string NhanVienLap { get; set; } = string.Empty;
        public string GhiChuChung { get; set; } = string.Empty;
        public bool LaHoaDonGop { get; set; } = false;
    }

    public class PhongDangOOptionDto
    {
        public string MaDatPhong { get; set; } = string.Empty;
        public string HienThi { get; set; } = string.Empty;
    }

    // Lưu vết thao tác (Audit Trail) cho Quản lý đối soát (Hủy hóa đơn, Gộp/Hủy gộp, Ghi nợ, Thanh toán)
    public class LichSuThaoTacItem
    {
        public string ThoiGian { get; set; } = string.Empty;
        public string NguoiThucHien { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public string HanhDong { get; set; } = string.Empty;
        public string MaHoaDon { get; set; } = string.Empty;
        public string ChiTiet { get; set; } = string.Empty;
    }

    // Lưu bản sao các hóa đơn gốc trước khi gộp để hỗ trợ tính năng "Hủy gộp" an toàn
    public class BanSaoHoaDonGoc
    {
        public string MaHoaDonNhaHang { get; set; } = string.Empty;
        public string MaBan { get; set; } = string.Empty;
        public DateTime ThoiGianTao { get; set; }
        public decimal TongTienMonAn { get; set; }
        public decimal GiamGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string HinhThucThanhToan { get; set; } = "Chờ Thanh Toán";
        public string DanhSachMonJson { get; set; } = "[]";
    }

    public static class ThuNganDataStore
    {
        private static readonly string LogPath = Path.Combine(Directory.GetCurrentDirectory(), "thungan_audit_log.json");
        private static readonly string MergeBackupPath = Path.Combine(Directory.GetCurrentDirectory(), "thungan_merge_backup.json");

        public static List<LichSuThaoTacItem> Logs { get; set; } = new List<LichSuThaoTacItem>();
        public static Dictionary<string, List<BanSaoHoaDonGoc>> MergeBackups { get; set; } = new Dictionary<string, List<BanSaoHoaDonGoc>>();

        public static void Load()
        {
            try
            {
                if (File.Exists(LogPath))
                {
                    var l = JsonSerializer.Deserialize<List<LichSuThaoTacItem>>(File.ReadAllText(LogPath));
                    if (l != null) Logs = l;
                }
                if (File.Exists(MergeBackupPath))
                {
                    var m = JsonSerializer.Deserialize<Dictionary<string, List<BanSaoHoaDonGoc>>>(File.ReadAllText(MergeBackupPath));
                    if (m != null) MergeBackups = m;
                }
            }
            catch { }
        }

        public static void SaveLog(string nguoiThucHien, string vaiTro, string hanhDong, string maHoaDon, string chiTiet)
        {
            try
            {
                Load();
                Logs.Insert(0, new LichSuThaoTacItem
                {
                    ThoiGian = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    NguoiThucHien = nguoiThucHien,
                    VaiTro = vaiTro,
                    HanhDong = hanhDong,
                    MaHoaDon = maHoaDon,
                    ChiTiet = chiTiet
                });
                File.WriteAllText(LogPath, JsonSerializer.Serialize(Logs, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static void SaveMergeBackups()
        {
            try
            {
                File.WriteAllText(MergeBackupPath, JsonSerializer.Serialize(MergeBackups, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }

    [Authorize(Roles = "Thu ngân Nhà hàng,Quản lý,Giám đốc")]
    public class QuayThuNganModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuayThuNganModel> _logger;

        public QuayThuNganModel(ApplicationDbContext context, ILogger<QuayThuNganModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CheDoXem { get; set; } = "DanhSach";

        public bool LaQuanLy { get; set; } = false;
        public bool LaThuNgan { get; set; } = false;
        public string TenNguoiDungHienTai { get; set; } = string.Empty;

        public List<HoaDonViewModel> DanhSachHoaDon { get; set; } = new List<HoaDonViewModel>();
        public List<LichSuThaoTacItem> LichSuThaoTac { get; set; } = new List<LichSuThaoTacItem>();
        public List<string> DanhSachSoPhongLuuTru { get; set; } = new List<string>();
        public List<PhongDangOOptionDto> DanhSachPhongDangO { get; set; } = new List<PhongDangOOptionDto>();

        // Số liệu phục vụ Báo cáo Quản lý
        public decimal TongDoanhThuDaThanhToan { get; set; }
        public int SoHoaDonDaThanhToan { get; set; }
        public int SoHoaDonChoThanhToan { get; set; }
        public int SoHoaDonDaHuy { get; set; }

        [BindProperty]
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt";

        private bool KiemTraLaQuanLy()
        {
            return User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);
        }

        private bool KiemTraLaThuNgan()
        {
            return !KiemTraLaQuanLy() && (User.IsInRole(HangSoHeThong.VaiTroThuNgan) || User.IsInRole(HangSoHeThong.VaiTroKeToan));
        }

        private static bool LaHoaDonDaChot(HOA_DON_NHA_HANG hd)
        {
            return string.Equals(hd.TrangThaiThanhToan, HangSoHeThong.HoaDonNHDaThanhToan, StringComparison.OrdinalIgnoreCase)
                || string.Equals(hd.TrangThaiThanhToan, HangSoHeThong.HoaDonNHGhiNoPhong, StringComparison.OrdinalIgnoreCase)
                || string.Equals(hd.HinhThucThanhToan, "Đã Thanh Toán", StringComparison.OrdinalIgnoreCase)
                || string.Equals(hd.HinhThucThanhToan, "Ghi nợ phòng", StringComparison.OrdinalIgnoreCase)
                || string.Equals(hd.HinhThucThanhToan, "Đã Hủy", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LaQuanLy = KiemTraLaQuanLy();
            LaThuNgan = KiemTraLaThuNgan();
            TenNguoiDungHienTai = User.Identity?.Name ?? "Nhân viên";

            try
            {
                ThuNganDataStore.Load();
                LichSuThaoTac = ThuNganDataStore.Logs.Take(50).ToList();

                // 1. Lấy danh sách các phòng đang lưu trú (DANG_O) phục vụ Ghi nợ vào phòng
                DanhSachPhongDangO = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                            join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                            join p in _context.PHONG.AsNoTracking() on ct.PHONGMaPhong equals p.MaPhong
                                            join kh in _context.KHACH_HANG.AsNoTracking() on dp.KHACH_HANGMaKH equals kh.MaKH
                                            where dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO
                                            orderby p.SoPhong
                                            select new PhongDangOOptionDto
                                            {
                                                MaDatPhong = dp.MaDatPhong,
                                                HienThi = $"Phòng {p.SoPhong} - {kh.HoTen} ({dp.MaDatPhong})"
                                            }).ToListAsync();

                DanhSachSoPhongLuuTru = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                               join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                               join p in _context.PHONG.AsNoTracking() on ct.PHONGMaPhong equals p.MaPhong
                                               where dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO
                                               orderby p.SoPhong
                                               select p.SoPhong ?? p.MaPhong)
                                              .Distinct()
                                              .ToListAsync();

                if (!DanhSachSoPhongLuuTru.Any())
                {
                    DanhSachSoPhongLuuTru = await _context.PHONG.AsNoTracking()
                        .OrderBy(p => p.SoPhong)
                        .Select(p => p.SoPhong ?? p.MaPhong)
                        .ToListAsync();
                }

                // 2. Truy vấn danh sách hóa đơn nhà hàng, bàn và nhân viên bằng LINQ
                var allHoaDonDb = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                    .OrderByDescending(h => h.ThoiGianTao)
                    .Take(100)
                    .ToListAsync();
                var dsBanDb = await _context.BAN_NHA_HANG.AsNoTracking().ToListAsync();
                var dsNhanVienDb = await _context.NHAN_VIEN.AsNoTracking().ToListAsync();

                TongDoanhThuDaThanhToan = allHoaDonDb
                    .Where(h => h.HinhThucThanhToan == "Đã Thanh Toán" || h.HinhThucThanhToan == "Ghi nợ phòng")
                    .Sum(h => h.ThanhTien ?? 0m);
                SoHoaDonDaThanhToan = allHoaDonDb.Count(h => h.HinhThucThanhToan == "Đã Thanh Toán" || h.HinhThucThanhToan == "Ghi nợ phòng");
                SoHoaDonChoThanhToan = allHoaDonDb.Count(h => !LaHoaDonDaChot(h));
                SoHoaDonDaHuy = allHoaDonDb.Count(h => h.HinhThucThanhToan == "Đã Hủy");

                // Quầy thu ngân chỉ hiển thị hóa đơn đang mở (hóa đơn đã chốt/hủy chuyển sang LichSuHoaDon)
                IEnumerable<HOA_DON_NHA_HANG> dsHoaDonDb = CheDoXem == "LichSu"
                    ? allHoaDonDb.Where(LaHoaDonDaChot)
                    : allHoaDonDb.Where(h => !LaHoaDonDaChot(h));

                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả")
                {
                    string loc = TrangThaiLoc.Trim();
                    dsHoaDonDb = dsHoaDonDb.Where(h =>
                    {
                        string tt = !string.IsNullOrWhiteSpace(h.TrangThaiThanhToan)
                            ? h.TrangThaiThanhToan.Trim()
                            : (h.HinhThucThanhToan ?? "Chưa thanh toán").Trim();

                        if (loc.Equals("Chờ Thanh Toán", StringComparison.OrdinalIgnoreCase))
                            return tt.Equals(HangSoHeThong.HoaDonNHChoThanhToan, StringComparison.OrdinalIgnoreCase)
                                || (h.HinhThucThanhToan ?? "").Equals("Chờ Thanh Toán", StringComparison.OrdinalIgnoreCase);

                        if (loc.Equals("Chưa thanh toán", StringComparison.OrdinalIgnoreCase))
                            return tt.Equals(HangSoHeThong.HoaDonNHDangPhucVu, StringComparison.OrdinalIgnoreCase)
                                || tt.Equals("Chưa thanh toán", StringComparison.OrdinalIgnoreCase);

                        return tt.Equals(loc, StringComparison.OrdinalIgnoreCase)
                            || (h.HinhThucThanhToan ?? "").Equals(loc, StringComparison.OrdinalIgnoreCase);
                    });
                }

                int stt = 1;
                foreach (var hd in dsHoaDonDb)
                {
                    string maBanCuaHd = (hd.BAN_NHA_HANGMaBan ?? string.Empty).Trim();
                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();
                    var cacBanTuongUng = dsBanDb.Where(b => danhSachMaBan.Contains(b.MaBan)).ToList();

                    string soBanHienThi = cacBanTuongUng.Any()
                        ? string.Join(", ", cacBanTuongUng.Select(b =>
                        {
                            string sb = (b.TenBan ?? b.MaBan).Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                            if (sb.Length == 1 && char.IsDigit(sb[0])) sb = "0" + sb;
                            return sb;
                        }))
                        : (string.IsNullOrEmpty(maBanCuaHd) ? "Chưa gắn bàn" : maBanCuaHd);

                    string tenNvLap = "Phục vụ NH";
                    if (!string.IsNullOrWhiteSpace(hd.NHAN_VIENMaNV))
                    {
                        var nv = dsNhanVienDb.FirstOrDefault(n => n.MaNV == hd.NHAN_VIENMaNV);
                        if (nv != null && !string.IsNullOrWhiteSpace(nv.HoTen))
                            tenNvLap = nv.HoTen;
                    }

                    var dsMonAnThuNgan = DocDanhSachMonTuJson(hd.DanhSachMonJson);
                    string finalJson = Uri.EscapeDataString(JsonSerializer.Serialize(dsMonAnThuNgan));

                    decimal tamTinh = dsMonAnThuNgan.Sum(x => x.ThanhTien);
                    if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0m;

                    decimal phiPhucVu = Math.Round(tamTinh * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                    decimal thueVAT = Math.Round(tamTinh * HangSoHeThong.TyLeThueVAT, 0);
                    decimal giamGia = hd.GiamGia ?? 0m;
                    decimal tongCong = Math.Max(0, tamTinh + phiPhucVu + thueVAT - giamGia);

                    string trangThaiHienThi = !string.IsNullOrWhiteSpace(hd.HinhThucThanhToan)
                        ? hd.HinhThucThanhToan
                        : (hd.TrangThaiThanhToan ?? "Chưa thanh toán");

                    bool laHoaDonGop = danhSachMaBan.Count > 1 || ThuNganDataStore.MergeBackups.ContainsKey(hd.MaHoaDonNhaHang);

                    DanhSachHoaDon.Add(new HoaDonViewModel
                    {
                        STT = stt < 10 ? "0" + stt : stt.ToString(),
                        MaHoaDonNhaHang = hd.MaHoaDonNhaHang,
                        MaBan = maBanCuaHd,
                        TenBan = $"Bàn {soBanHienThi}",
                        HinhThucThanhToan = trangThaiHienThi,
                        DanhSachMonJson = finalJson,
                        TamTinh = tamTinh,
                        PhiPhucVu = phiPhucVu,
                        ThueVAT = thueVAT,
                        GiamGia = giamGia,
                        TongCong = tongCong,
                        GioTao = (hd.ThoiGianTao ?? DateTime.Now).ToString("HH:mm dd/MM"),
                        NhanVienLap = tenNvLap,
                        LaHoaDonGop = laHoaDonGop
                    });
                    stt++;
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải dữ liệu Quầy thu ngân.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải danh sách hóa đơn nhà hàng.");
                return Page();
            }
        }

        // 1. Áp dụng Voucher / Mã giảm giá (Chỉ Thu ngân)
        public async Task<IActionResult> OnPostApDungVoucherAsync(string maHoaDonVoucher, string maVoucher)
        {
            if (!KiemTraLaThuNgan())
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền áp dụng mã giảm giá!";
                return RedirectToPage();
            }

            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonVoucher);
                if (hd == null || LaHoaDonDaChot(hd))
                {
                    TempData["ErrorMessage"] = "Hóa đơn không hợp lệ hoặc đã chốt thanh toán!";
                    return RedirectToPage();
                }

                string code = (maVoucher ?? string.Empty).Trim().ToUpper();
                var dsMon = DocDanhSachMonTuJson(hd.DanhSachMonJson);
                decimal tamTinh = dsMon.Sum(x => x.ThanhTien);
                if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0m;

                decimal soTienGiam;
                if (code == "HOLIDAY2024" || code == "VIP10") soTienGiam = Math.Round(tamTinh * 0.10m, 0);
                else if (code == "VIP20") soTienGiam = Math.Round(tamTinh * 0.20m, 0);
                else if (code == "GIAM50K") soTienGiam = 50000m;
                else
                {
                    TempData["ErrorMessage"] = $"Mã giảm giá '{code}' không tồn tại! (Gợi ý: HOLIDAY2024, VIP10, VIP20, GIAM50K)";
                    return RedirectToPage();
                }

                decimal phiPhucVu = Math.Round(tamTinh * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                decimal thueVAT = Math.Round(tamTinh * HangSoHeThong.TyLeThueVAT, 0);

                hd.GiamGia = soTienGiam;
                hd.PhiPhucVu = phiPhucVu;
                hd.TienThueVAT = thueVAT;
                hd.ThanhTien = Math.Max(0, tamTinh + phiPhucVu + thueVAT - soTienGiam);
                await _context.SaveChangesAsync();

                ThuNganDataStore.SaveLog(User.Identity?.Name ?? "Thu ngân", HangSoHeThong.VaiTroThuNgan,
                    "Áp dụng Voucher", hd.MaHoaDonNhaHang, $"Mã {code} - Giảm {soTienGiam:N0} VND");
                TempData["SuccessMessage"] = $"Đã áp dụng mã {code} cho hóa đơn {hd.MaHoaDonNhaHang} (Giảm {soTienGiam:N0} VND)!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi áp dụng voucher cho hóa đơn {MaHD}", maHoaDonVoucher);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi áp dụng mã giảm giá.";
            }

            return RedirectToPage();
        }

        // 2. Gộp nhiều hóa đơn chưa chốt thành 1 Bill thống nhất (LINQ + Transaction + Lưu bản sao tách bill)
        public async Task<IActionResult> OnPostGopBillAsync(List<string> maHoaDonGopList)
        {
            if (!KiemTraLaThuNgan())
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Gộp bill!";
                return RedirectToPage();
            }

            if (maHoaDonGopList == null || maHoaDonGopList.Count < 2)
            {
                TempData["ErrorMessage"] = "Vui lòng tích chọn ít nhất 2 hóa đơn chưa thanh toán để Gộp bill!";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var dsHoaDonCanGop = await _context.HOA_DON_NHA_HANG
                    .Where(h => maHoaDonGopList.Contains(h.MaHoaDonNhaHang))
                    .OrderBy(h => h.ThoiGianTao)
                    .ToListAsync();

                dsHoaDonCanGop = dsHoaDonCanGop.Where(h => !LaHoaDonDaChot(h)).ToList();

                if (dsHoaDonCanGop.Count < 2)
                {
                    TempData["ErrorMessage"] = "Các hóa đơn được chọn không hợp lệ hoặc đã được chốt trước đó!";
                    return RedirectToPage();
                }

                // Lưu bản sao các hóa đơn gốc trước khi gộp để hỗ trợ tính năng Hủy gộp bill
                var banSaoList = dsHoaDonCanGop.Select(h => new BanSaoHoaDonGoc
                {
                    MaHoaDonNhaHang = h.MaHoaDonNhaHang,
                    MaBan = h.BAN_NHA_HANGMaBan ?? string.Empty,
                    ThoiGianTao = h.ThoiGianTao ?? DateTime.Now,
                    TongTienMonAn = h.TongTienMonAn ?? 0m,
                    GiamGia = h.GiamGia ?? 0m,
                    ThanhTien = h.ThanhTien ?? 0m,
                    HinhThucThanhToan = h.HinhThucThanhToan ?? "Chờ Thanh Toán",
                    DanhSachMonJson = h.DanhSachMonJson ?? "[]"
                }).ToList();

                var billChinh = dsHoaDonCanGop.First();
                var cacBillPhu = dsHoaDonCanGop.Skip(1).ToList();

                var danhSachMaBanGop = new List<string>();
                if (!string.IsNullOrWhiteSpace(billChinh.BAN_NHA_HANGMaBan))
                {
                    danhSachMaBanGop.AddRange(
                        billChinh.BAN_NHA_HANGMaBan.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
                }

                var mergedJsonElements = new List<JsonElement>();
                void ThemJsonMon(string? json)
                {
                    if (string.IsNullOrWhiteSpace(json) || json == "[]") return;
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        foreach (var el in doc.RootElement.EnumerateArray())
                            mergedJsonElements.Add(el.Clone());
                    }
                    catch { }
                }

                ThemJsonMon(billChinh.DanhSachMonJson);

                var maBillPhuList = cacBillPhu.Select(b => b.MaHoaDonNhaHang).ToList();
                var dsChiTietPhu = await _context.CHI_TIET_DICH_VU
                    .Where(c => c.HOA_DON_NHA_HANGMaHoaDonNhaHang != null && maBillPhuList.Contains(c.HOA_DON_NHA_HANGMaHoaDonNhaHang))
                    .ToListAsync();

                foreach (var billPhu in cacBillPhu)
                {
                    billChinh.TongTienMonAn = (billChinh.TongTienMonAn ?? 0m) + (billPhu.TongTienMonAn ?? 0m);
                    billChinh.GiamGia = (billChinh.GiamGia ?? 0m) + (billPhu.GiamGia ?? 0m);

                    if (!string.IsNullOrWhiteSpace(billPhu.BAN_NHA_HANGMaBan))
                    {
                        foreach (var mb in billPhu.BAN_NHA_HANGMaBan.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
                        {
                            if (!danhSachMaBanGop.Contains(mb))
                                danhSachMaBanGop.Add(mb);
                        }
                    }

                    ThemJsonMon(billPhu.DanhSachMonJson);
                }

                string chuoiMaBanGop = string.Join(",", danhSachMaBanGop);
                if (chuoiMaBanGop.Length > 20)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Không thể gộp quá nhiều bàn vào cùng 1 hóa đơn (vượt giới hạn lưu trữ mã bàn).";
                    return RedirectToPage();
                }

                foreach (var ct in dsChiTietPhu)
                {
                    ct.HOA_DON_NHA_HANGMaHoaDonNhaHang = billChinh.MaHoaDonNhaHang;
                }

                _context.HOA_DON_NHA_HANG.RemoveRange(cacBillPhu);

                billChinh.BAN_NHA_HANGMaBan = chuoiMaBanGop;
                billChinh.DanhSachMonJson = JsonSerializer.Serialize(mergedJsonElements);

                var dsMonSauGop = DocDanhSachMonTuJson(billChinh.DanhSachMonJson);
                decimal tamTinh = dsMonSauGop.Sum(x => x.ThanhTien);
                if (tamTinh == 0) tamTinh = billChinh.TongTienMonAn ?? 0m;
                decimal phiPhucVu = Math.Round(tamTinh * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                decimal thueVAT = Math.Round(tamTinh * HangSoHeThong.TyLeThueVAT, 0);

                billChinh.TongTienMonAn = tamTinh;
                billChinh.PhiPhucVu = phiPhucVu;
                billChinh.TienThueVAT = thueVAT;
                billChinh.ThanhTien = Math.Max(0, tamTinh + phiPhucVu + thueVAT - (billChinh.GiamGia ?? 0m));
                billChinh.TrangThaiThanhToan = HangSoHeThong.HoaDonNHChoThanhToan;
                billChinh.HinhThucThanhToan = "Chờ Thanh Toán";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThuNganDataStore.Load();
                ThuNganDataStore.MergeBackups[billChinh.MaHoaDonNhaHang] = banSaoList;
                ThuNganDataStore.SaveMergeBackups();

                ThuNganDataStore.SaveLog(User.Identity?.Name ?? "Thu ngân", HangSoHeThong.VaiTroThuNgan,
                    "Gộp bill", billChinh.MaHoaDonNhaHang, $"Gộp {dsHoaDonCanGop.Count} hóa đơn ({chuoiMaBanGop})");
                TempData["SuccessMessage"] = $"Đã gộp {dsHoaDonCanGop.Count} hóa đơn thành 1 Bill thống nhất (Mã HD: {billChinh.MaHoaDonNhaHang})!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi gộp bill tại Quầy thu ngân.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi gộp bill.";
            }

            return RedirectToPage();
        }

        // 3. Hủy gộp bill chưa thanh toán (Chỉ Thu ngân - thuần LINQ + Transaction)
        public async Task<IActionResult> OnPostHuyGopBillAsync(string maHoaDonHuyGop)
        {
            if (!KiemTraLaThuNgan())
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Hủy gộp bill!";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var billGop = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonHuyGop);
                if (billGop == null || LaHoaDonDaChot(billGop))
                {
                    TempData["ErrorMessage"] = "Chỉ có thể Hủy gộp đối với hóa đơn gộp chưa thanh toán!";
                    return RedirectToPage();
                }

                ThuNganDataStore.Load();
                if (!ThuNganDataStore.MergeBackups.TryGetValue(maHoaDonHuyGop, out var banSaoList) || banSaoList.Count < 2)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dữ liệu tách gốc của hóa đơn gộp này!";
                    return RedirectToPage();
                }

                var gocChinh = banSaoList.First();
                billGop.TongTienMonAn = gocChinh.TongTienMonAn;
                billGop.PhiPhucVu = Math.Round(gocChinh.TongTienMonAn * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                billGop.TienThueVAT = Math.Round(gocChinh.TongTienMonAn * HangSoHeThong.TyLeThueVAT, 0);
                billGop.GiamGia = gocChinh.GiamGia;
                billGop.ThanhTien = gocChinh.ThanhTien;
                billGop.HinhThucThanhToan = gocChinh.HinhThucThanhToan;
                billGop.TrangThaiThanhToan = HangSoHeThong.HoaDonNHChoThanhToan;
                billGop.BAN_NHA_HANGMaBan = gocChinh.MaBan;
                billGop.DanhSachMonJson = gocChinh.DanhSachMonJson;

                foreach (var gocPhu in banSaoList.Skip(1))
                {
                    _context.HOA_DON_NHA_HANG.Add(new HOA_DON_NHA_HANG
                    {
                        MaHoaDonNhaHang = gocPhu.MaHoaDonNhaHang,
                        BAN_NHA_HANGMaBan = gocPhu.MaBan,
                        ThoiGianTao = gocPhu.ThoiGianTao,
                        TongTienMonAn = gocPhu.TongTienMonAn,
                        PhiPhucVu = Math.Round(gocPhu.TongTienMonAn * HangSoHeThong.TyLePhiPhucVuNhaHang, 0),
                        TienThueVAT = Math.Round(gocPhu.TongTienMonAn * HangSoHeThong.TyLeThueVAT, 0),
                        GiamGia = gocPhu.GiamGia,
                        ThanhTien = gocPhu.ThanhTien,
                        HinhThucThanhToan = gocPhu.HinhThucThanhToan,
                        TrangThaiThanhToan = HangSoHeThong.HoaDonNHChoThanhToan,
                        DanhSachMonJson = gocPhu.DanhSachMonJson
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThuNganDataStore.MergeBackups.Remove(maHoaDonHuyGop);
                ThuNganDataStore.SaveMergeBackups();

                ThuNganDataStore.SaveLog(User.Identity?.Name ?? "Thu ngân", HangSoHeThong.VaiTroThuNgan,
                    "Hủy gộp bill", maHoaDonHuyGop, $"Tách lại thành {banSaoList.Count} hóa đơn ban đầu");
                TempData["SuccessMessage"] = $"Đã hủy gộp hóa đơn {maHoaDonHuyGop} về lại {banSaoList.Count} hóa đơn riêng biệt!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi hủy gộp bill {MaHD}", maHoaDonHuyGop);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi hủy gộp bill.";
            }

            return RedirectToPage();
        }

        // 4. Ghi nợ vào phòng đang lưu trú (Liên kết khóa ngoại DAT_PHONGMaDatPhong để cộng dồn khi Check-out)
        public async Task<IActionResult> OnPostGhiNoPhongAsync(string maHoaDonGhiNo, string soPhongGhiNo)
        {
            if (!KiemTraLaThuNgan())
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Ghi nợ vào phòng!";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(maHoaDonGhiNo) || string.IsNullOrWhiteSpace(soPhongGhiNo))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn số phòng hợp lệ để ghi nợ!";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonGhiNo);
                if (hd == null || LaHoaDonDaChot(hd))
                {
                    TempData["ErrorMessage"] = "Hóa đơn không tồn tại hoặc đã được chốt trước đó!";
                    return RedirectToPage();
                }

                string phongClean = soPhongGhiNo.Trim();
                // Tìm đơn lưu trú đang ở (DANG_O) theo Số phòng hoặc Mã đặt phòng
                var maDatPhongDangO = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                             join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                             join p in _context.PHONG.AsNoTracking() on ct.PHONGMaPhong equals p.MaPhong
                                             where dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO
                                                && (p.SoPhong == phongClean || p.MaPhong == phongClean || dp.MaDatPhong == phongClean)
                                             select dp.MaDatPhong).FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(maDatPhongDangO))
                {
                    TempData["ErrorMessage"] = $"Phòng '{phongClean}' hiện không có khách đang lưu trú (DANG_O), không thể ghi nợ!";
                    return RedirectToPage();
                }

                var dsMon = DocDanhSachMonTuJson(hd.DanhSachMonJson);
                decimal tamTinh = dsMon.Sum(x => x.ThanhTien);
                if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0m;
                decimal phiPhucVu = Math.Round(tamTinh * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                decimal thueVAT = Math.Round(tamTinh * HangSoHeThong.TyLeThueVAT, 0);
                decimal tongCong = Math.Max(0, tamTinh + phiPhucVu + thueVAT - (hd.GiamGia ?? 0m));

                hd.TongTienMonAn = tamTinh;
                hd.PhiPhucVu = phiPhucVu;
                hd.TienThueVAT = thueVAT;
                hd.ThanhTien = tongCong;
                hd.DAT_PHONGMaDatPhong = maDatPhongDangO;
                hd.TrangThaiThanhToan = HangSoHeThong.HoaDonNHGhiNoPhong;
                hd.HinhThucThanhToan = "Ghi nợ phòng";

                int soBanGiaiPhong = await ChuyenCacBanSangDonDepAsync(hd.BAN_NHA_HANGMaBan);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThuNganDataStore.SaveLog(User.Identity?.Name ?? "Thu ngân", HangSoHeThong.VaiTroThuNgan,
                    "Ghi nợ vào phòng", maHoaDonGhiNo, $"Chuyển công nợ {tongCong:N0} VND vào Phòng {phongClean} (Đơn {maDatPhongDangO})");
                TempData["SuccessMessage"] = $"Đã ghi nợ hóa đơn {maHoaDonGhiNo} ({tongCong:N0}đ) vào Phòng {phongClean} và chuyển {soBanGiaiPhong} bàn sang Đang dọn dẹp!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi ghi nợ hóa đơn {MaHD} vào phòng {SoPhong}", maHoaDonGhiNo, soPhongGhiNo);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi ghi nợ vào phòng.";
            }

            return RedirectToPage();
        }

        // 5. Thanh toán & Chốt hóa đơn (Chỉ Thu ngân - ghi THANH_TOAN, giải phóng bàn & tạo mã VietQR)
        public async Task<IActionResult> OnPostThanhToanAsync(string maHoaDonToPay)
        {
            if (!KiemTraLaThuNgan())
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Thanh toán & Chốt hóa đơn!";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(maHoaDonToPay)) return RedirectToPage();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonToPay);
                if (hd == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hóa đơn cần thanh toán!";
                    return RedirectToPage();
                }

                if (LaHoaDonDaChot(hd))
                {
                    TempData["ErrorMessage"] = $"Hóa đơn {maHoaDonToPay} đã được chốt trước đó!";
                    return RedirectToPage();
                }

                var dsMon = DocDanhSachMonTuJson(hd.DanhSachMonJson);
                decimal tamTinh = dsMon.Sum(x => x.ThanhTien);
                if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0m;
                decimal phiPhucVu = Math.Round(tamTinh * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                decimal thueVAT = Math.Round(tamTinh * HangSoHeThong.TyLeThueVAT, 0);
                decimal tongCong = Math.Max(0, tamTinh + phiPhucVu + thueVAT - (hd.GiamGia ?? 0m));

                string phuongThuc = string.IsNullOrWhiteSpace(PhuongThucThanhToan) ? "Tiền mặt" : PhuongThucThanhToan.Trim();

                hd.TongTienMonAn = tamTinh;
                hd.PhiPhucVu = phiPhucVu;
                hd.TienThueVAT = thueVAT;
                hd.ThanhTien = tongCong;
                hd.TrangThaiThanhToan = HangSoHeThong.HoaDonNHDaThanhToan;
                hd.HinhThucThanhToan = "Đã Thanh Toán";

                // Lưu lịch sử giao dịch vào bảng THANH_TOAN
                _context.THANH_TOAN.Add(new THANH_TOAN
                {
                    MaThanhToan = BaoMatHelper.SinhMaNgauNhien("TT"),
                    HOA_DON_NHA_HANGMaHoaDonNhaHang = hd.MaHoaDonNhaHang,
                    SoTien = tongCong,
                    PhuongThucThanhToan = phuongThuc,
                    ThoiGianGiaoDich = DateTime.Now,
                    TrangThaiGiaoDich = "Thành công"
                });

                int soBanGiaiPhong = await ChuyenCacBanSangDonDepAsync(hd.BAN_NHA_HANGMaBan);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThuNganDataStore.SaveLog(User.Identity?.Name ?? "Thu ngân", HangSoHeThong.VaiTroThuNgan,
                    "Thanh toán & Chốt hóa đơn", maHoaDonToPay, $"Đã thu {tongCong:N0} VND ({phuongThuc}) - Chuyển {soBanGiaiPhong} bàn sang Đang dọn dẹp");
                TempData["SuccessMessage"] = $"Chốt thanh toán hóa đơn {maHoaDonToPay} ({tongCong:N0}đ - {phuongThuc}) thành công! Đã chuyển {soBanGiaiPhong} bàn sang Đang dọn dẹp.";

                // Tạo thông tin mã VietQR hiển thị popup sau thanh toán
                long soTien = Convert.ToInt64(Math.Round(tongCong));
                string noiDungCk = $"Thanh toan HD {maHoaDonToPay}";
                string bankId = "TPB";
                string accountNo = "999988886666";
                string accountName = "NHA HANG THU NGAN";
                string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png" +
                               $"?amount={soTien}&addInfo={Uri.EscapeDataString(noiDungCk)}&accountName={Uri.EscapeDataString(accountName)}";

                TempData["ShowQrPayment"] = "1";
                TempData["QrThoiGian"] = DateTime.Now.Ticks.ToString();
                TempData["QrMaHoaDon"] = maHoaDonToPay;
                TempData["QrSoTien"] = $"{soTien:N0} VNĐ";
                TempData["QrNoiDung"] = noiDungCk;
                TempData["QrBankName"] = "TPBank - Ngân hàng Tiên Phong";
                TempData["QrAccountNo"] = accountNo;
                TempData["QrAccountName"] = accountName;
                TempData["QrImageUrl"] = qrUrl;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xử lý thanh toán hóa đơn nhà hàng {MaHD}", maHoaDonToPay);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi chốt thanh toán hóa đơn.";
            }

            return RedirectToPage();
        }

        // 6. Hủy hóa đơn bắt buộc nhập lý do & lưu log (Độc quyền Quản lý / Giám đốc)
        public async Task<IActionResult> OnPostHuyHoaDonAsync(string maHoaDonHuy, string lyDoHuy)
        {
            if (!KiemTraLaQuanLy())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Chỉ tài khoản Quản lý mới có quyền Hủy hóa đơn!";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                TempData["ErrorMessage"] = "Bắt buộc phải nhập lý do hủy hóa đơn để lưu vết kiểm toán!";
                return RedirectToPage();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonHuy);
                if (hd != null)
                {
                    hd.TrangThaiThanhToan = "Đã Hủy";
                    hd.HinhThucThanhToan = "Đã Hủy";

                    if (!string.IsNullOrWhiteSpace(hd.BAN_NHA_HANGMaBan))
                    {
                        var dsMaBan = hd.BAN_NHA_HANGMaBan.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                        var cacBan = await _context.BAN_NHA_HANG.Where(b => dsMaBan.Contains(b.MaBan)).ToListAsync();
                        foreach (var ban in cacBan)
                        {
                            ban.TrangThai = "Trống";
                            ban.TenKhachDat = null;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    ThuNganDataStore.SaveLog(User.Identity?.Name ?? "Quản lý", HangSoHeThong.VaiTroQuanLy,
                        "Hủy hóa đơn", maHoaDonHuy, $"Lý do: {lyDoHuy.Trim()}");
                    TempData["SuccessMessage"] = $"Quản lý đã hủy hóa đơn {maHoaDonHuy} và ghi nhận vào Lịch sử thao tác!";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi hủy hóa đơn nhà hàng {MaHD}", maHoaDonHuy);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi hủy hóa đơn.";
            }

            return RedirectToPage();
        }

        private async Task<int> ChuyenCacBanSangDonDepAsync(string? chuoiMaBan)
        {
            if (string.IsNullOrWhiteSpace(chuoiMaBan)) return 0;

            var danhSachMaBan = chuoiMaBan
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var cacBanLienQuan = await _context.BAN_NHA_HANG
                .Where(b => danhSachMaBan.Contains(b.MaBan))
                .ToListAsync();

            foreach (var ban in cacBanLienQuan)
            {
                ban.TrangThai = "Đang dọn dẹp";
                _context.BAN_NHA_HANG.Update(ban);
            }

            return cacBanLienQuan.Count;
        }

        private static List<ChiTietMonViewModel> DocDanhSachMonTuJson(string? jsonMonAn)
        {
            var ds = new List<ChiTietMonViewModel>();
            if (string.IsNullOrWhiteSpace(jsonMonAn) || jsonMonAn == "[]") return ds;

            try
            {
                using var doc = JsonDocument.Parse(jsonMonAn);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    string tenMon = element.TryGetProperty("TenMon", out var t2) ? (t2.GetString() ?? "")
                        : (element.TryGetProperty("tenDichVu", out var t1) ? (t1.GetString() ?? "") : "");
                    int soLuong = element.TryGetProperty("SoLuong", out var s2) ? s2.GetInt32()
                        : (element.TryGetProperty("soLuong", out var s1) ? s1.GetInt32() : 1);
                    decimal donGia = element.TryGetProperty("DonGia", out var d2) ? d2.GetDecimal()
                        : (element.TryGetProperty("donGia", out var d1) ? d1.GetDecimal() : 0m);
                    string ghiChu = element.TryGetProperty("GhiChu", out var g2) ? (g2.GetString() ?? "")
                        : (element.TryGetProperty("ghiChu", out var g1) ? (g1.GetString() ?? "") : "");

                    ds.Add(new ChiTietMonViewModel
                    {
                        TenMon = string.IsNullOrEmpty(tenMon) ? "Món ăn" : tenMon,
                        SoLuong = soLuong,
                        DonGia = donGia,
                        ThanhTien = soLuong * donGia,
                        GhiChu = ghiChu
                    });
                }
            }
            catch { }

            return ds;
        }
    }
}
