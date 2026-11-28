using Microsoft.AspNetCore.Authorization;
using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
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
        // BỔ SUNG: Giờ tạo, Nhân viên lập, Ghi chú chung và cờ nhận biết hóa đơn gộp
        public string GioTao { get; set; } = string.Empty;
        public string NhanVienLap { get; set; } = string.Empty;
        public string GhiChuChung { get; set; } = string.Empty;
        public bool LaHoaDonGop { get; set; } = false;
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
    [Authorize(Roles = "Thu ngân Nhà hàng, Quản lý")]
    public class QuayThuNganModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IActionDescriptorCollectionProvider _actionProvider;

        public QuayThuNganModel(ApplicationDbContext context, IActionDescriptorCollectionProvider actionProvider)
        {
            _context = context;
            _actionProvider = actionProvider;
        }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        // Chế độ hiển thị: "DanhSach" (Hóa đơn đang xử lý) hoặc "LichSu" (Lịch sử hóa đơn đã thanh toán/đã hủy)
        [BindProperty(SupportsGet = true)]
        public string CheDoXem { get; set; } = "DanhSach";

        // Phân quyền hiển thị & thao tác
        public bool LaQuanLy { get; set; } = false;
        public bool LaThuNgan { get; set; } = false;
        public string TenNguoiDungHienTai { get; set; } = string.Empty;

        public List<HoaDonViewModel> DanhSachHoaDon { get; set; } = new List<HoaDonViewModel>();
        public List<LichSuThaoTacItem> LichSuThaoTac { get; set; } = new List<LichSuThaoTacItem>();
        public List<string> DanhSachSoPhongLuuTru { get; set; } = new List<string>();

        // Số liệu phục vụ Báo cáo Quản lý
        public decimal TongDoanhThuDaThanhToan { get; set; }
        public int SoHoaDonDaThanhToan { get; set; }
        public int SoHoaDonChoThanhToan { get; set; }
        public int SoHoaDonDaHuy { get; set; }

        private string TimDuongDanTrang(string tenTrang, string duongDanMacDinh)
        {
            var page = _actionProvider.ActionDescriptors.Items
                .OfType<PageActionDescriptor>()
                .FirstOrDefault(p => p.ViewEnginePath.EndsWith("/" + tenTrang, StringComparison.OrdinalIgnoreCase)
                                  || p.ViewEnginePath.Equals("/" + tenTrang, StringComparison.OrdinalIgnoreCase));
            return page != null ? page.ViewEnginePath : duongDanMacDinh;
        }

        private async Task<(string vaiTro, string hoTen)> LayThongTinNguoiDungAsync()
        {
            string tenDangNhap = User?.Identity?.Name
                              ?? User?.FindFirst(ClaimTypes.Name)?.Value
                              ?? User?.FindFirst("TenDangNhap")?.Value
                              ?? string.Empty;

            string maTaiKhoan = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User?.FindFirst("MaTaiKhoan")?.Value
                             ?? string.Empty;

            if (string.IsNullOrEmpty(tenDangNhap))
            {
                try { tenDangNhap = HttpContext.Session.GetString("TenDangNhap") ?? string.Empty; } catch { }
            }

            string vaiTro = string.Empty;
            string hoTen = tenDangNhap;

            if (!string.IsNullOrEmpty(tenDangNhap) || !string.IsNullOrEmpty(maTaiKhoan))
            {
                var tkDb = await _context.TAI_KHOAN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap || t.MaTaiKhoan == maTaiKhoan);

                if (tkDb != null)
                {
                    vaiTro = (tkDb.VaiTro ?? string.Empty).Trim();
                    hoTen = tkDb.TenDangNhap ?? hoTen;
                }
            }

            if (string.IsNullOrEmpty(vaiTro))
            {
                vaiTro = User?.FindFirst(ClaimTypes.Role)?.Value
                      ?? User?.FindFirst("VaiTro")?.Value
                      ?? string.Empty;
            }

            return (vaiTro.Trim(), string.IsNullOrEmpty(hoTen) ? "Nhân viên" : hoTen.Trim());
        }

        private bool KiemTraLaQuanLy(string vaiTro)
        {
            if (string.IsNullOrEmpty(vaiTro)) return false;
            return vaiTro.Contains("Quản lý", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("QuanLy", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Giám đốc", StringComparison.OrdinalIgnoreCase);
        }

        private bool KiemTraLaThuNgan(string vaiTro)
        {
            if (string.IsNullOrEmpty(vaiTro) || KiemTraLaQuanLy(vaiTro)) return false;
            return vaiTro.Contains("Thu ngân", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("ThuNgan", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Kế toán", StringComparison.OrdinalIgnoreCase);
        }

        private string GetMaBanOfHoaDon(HOA_DON_NHA_HANG hd)
        {
            string maBanCuaHd = "";
            var propMaBan = hd.GetType().GetProperty("MaBan") ?? hd.GetType().GetProperty("BAN_NHA_HANGMaBan");
            if (propMaBan != null) maBanCuaHd = propMaBan.GetValue(hd)?.ToString() ?? "";
            if (string.IsNullOrEmpty(maBanCuaHd))
            {
                try { maBanCuaHd = _context.Entry(hd).Property("MaBan").CurrentValue?.ToString() ?? ""; } catch { }
            }
            if (string.IsNullOrEmpty(maBanCuaHd))
            {
                try { maBanCuaHd = _context.Entry(hd).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? ""; } catch { }
            }
            return maBanCuaHd.Trim();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (string.IsNullOrEmpty(vaiTro)) return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));

            if (vaiTro.Contains("Phục vụ", StringComparison.OrdinalIgnoreCase))
                return Redirect(TimDuongDanTrang("SoDoBan", "/NhaHang/PhucVu/SoDoBan"));

            if (vaiTro.Contains("Đầu bếp", StringComparison.OrdinalIgnoreCase) || vaiTro.Contains("Bếp", StringComparison.OrdinalIgnoreCase))
                return Redirect(TimDuongDanTrang("QuanLyOrder", "/QuanLyNhaHang/QuanLyOrder"));

            LaQuanLy = KiemTraLaQuanLy(vaiTro);
            LaThuNgan = KiemTraLaThuNgan(vaiTro);
            TenNguoiDungHienTai = hoTen;

            if (!LaQuanLy && !LaThuNgan) return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));

            try
            {
                ThuNganDataStore.Load();
                LichSuThaoTac = ThuNganDataStore.Logs.Take(50).ToList();

                // Lấy danh sách số phòng từ bảng PHONG để Thu ngân chọn khi Ghi nợ vào phòng
                try
                {
                    DanhSachSoPhongLuuTru = await _context.PHONG
                        .AsNoTracking()
                        .OrderBy(p => p.SoPhong)
                        .Select(p => p.SoPhong ?? p.MaPhong)
                        .ToListAsync();
                }
                catch { }

                var allHoaDonDb = await _context.HOA_DON_NHA_HANG.OrderByDescending(h => h.ThoiGianTao).ToListAsync();
                var dsBanDb = await _context.BAN_NHA_HANG.AsNoTracking().ToListAsync();
                var dsNhanVienDb = await _context.NHAN_VIEN.AsNoTracking().ToListAsync();

                // Tính số liệu cho Báo cáo Quản lý
                TongDoanhThuDaThanhToan = allHoaDonDb.Where(h => h.HinhThucThanhToan == "Đã Thanh Toán" || h.HinhThucThanhToan == "Ghi nợ phòng").Sum(h => h.ThanhTien ?? 0);
                SoHoaDonDaThanhToan = allHoaDonDb.Count(h => h.HinhThucThanhToan == "Đã Thanh Toán" || h.HinhThucThanhToan == "Ghi nợ phòng");
                SoHoaDonChoThanhToan = allHoaDonDb.Count(h => h.HinhThucThanhToan == "Chờ Thanh Toán" || h.HinhThucThanhToan == "Chưa thanh toán");
                SoHoaDonDaHuy = allHoaDonDb.Count(h => h.HinhThucThanhToan == "Đã Hủy");
                
                // Quầy thu ngân chỉ hiển thị các hóa đơn đang mở; hóa đơn đã thanh toán/ghi nợ/hủy tự động chuyển sang LichSuHoaDon
                var dsHoaDonDb = allHoaDonDb.Where(h => h.HinhThucThanhToan != "Đã Thanh Toán"
                                                     && h.HinhThucThanhToan != "Ghi nợ phòng"
                                                     && h.HinhThucThanhToan != "Đã Hủy");

                if (!string.IsNullOrEmpty(TrangThaiLoc) && TrangThaiLoc != "Tất cả")
                {
                    dsHoaDonDb = dsHoaDonDb.Where(h => (h.HinhThucThanhToan ?? "Chưa thanh toán") == TrangThaiLoc);
                }

                int stt = 1;
                foreach (var hd in dsHoaDonDb)
                {
                    string maBanCuaHd = GetMaBanOfHoaDon(hd);
                    if (string.IsNullOrEmpty(maBanCuaHd)) continue;

                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBanTuongUng = dsBanDb.Where(b => danhSachMaBan.Contains(b.MaBan)).ToList();
                    string soBanHienThi = cacBanTuongUng.Any()
                        ? string.Join(", ", cacBanTuongUng.Select(b =>
                        {
                            string sb = (b.TenBan ?? "").Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                            if (sb.Length == 1 && char.IsDigit(sb[0])) sb = "0" + sb;
                            return sb;
                        }))
                        : maBanCuaHd;

                    // Lấy tên Nhân viên lập từ khóa ngoại NHAN_VIENMaNV / MaNV (nếu có)
                    string tenNvLap = "Phục vụ NH";
                    try
                    {
                        var propNv = hd.GetType().GetProperty("MaNV") ?? hd.GetType().GetProperty("NHAN_VIENMaNV");
                        string maNv = propNv?.GetValue(hd)?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(maNv))
                        {
                            var nv = dsNhanVienDb.FirstOrDefault(n => n.MaNV == maNv);
                            if (nv != null && !string.IsNullOrEmpty(nv.HoTen)) tenNvLap = nv.HoTen;
                        }
                    }
                    catch { }

                    string sttFormat = stt < 10 ? "0" + stt : stt.ToString();
                    stt++;

                    string jsonMonAn = "[]";
                    try
                    {
                        var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) jsonMonAn = propJson.GetValue(hd)?.ToString() ?? "[]";
                        else jsonMonAn = _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";
                    }
                    catch { }

                    var dsMonAnThuNgan = new List<ChiTietMonViewModel>();
                    try
                    {
                        if (!string.IsNullOrEmpty(jsonMonAn) && jsonMonAn != "[]")
                        {
                            using (var doc = JsonDocument.Parse(jsonMonAn))
                            {
                                foreach (var element in doc.RootElement.EnumerateArray())
                                {
                                    string tenMon = "";
                                    if (element.TryGetProperty("tenDichVu", out var t1)) tenMon = t1.GetString() ?? "";
                                    else if (element.TryGetProperty("TenMon", out var t2)) tenMon = t2.GetString() ?? "";

                                    int soLuong = 1;
                                    if (element.TryGetProperty("soLuong", out var s1)) soLuong = s1.GetInt32();
                                    else if (element.TryGetProperty("SoLuong", out var s2)) soLuong = s2.GetInt32();

                                    decimal donGia = 0;
                                    if (element.TryGetProperty("donGia", out var d1)) donGia = d1.GetDecimal();
                                    else if (element.TryGetProperty("DonGia", out var d2)) donGia = d2.GetDecimal();

                                    string ghiChu = "";
                                    if (element.TryGetProperty("ghiChu", out var g1)) ghiChu = g1.GetString() ?? "";
                                    else if (element.TryGetProperty("GhiChu", out var g2)) ghiChu = g2.GetString() ?? "";

                                    dsMonAnThuNgan.Add(new ChiTietMonViewModel
                                    {
                                        TenMon = string.IsNullOrEmpty(tenMon) ? "Món ăn" : tenMon,
                                        SoLuong = soLuong,
                                        DonGia = donGia,
                                        ThanhTien = soLuong * donGia,
                                        GhiChu = ghiChu
                                    });
                                }
                            }
                        }
                    }
                    catch { }

                    string finalJson = Uri.EscapeDataString(JsonSerializer.Serialize(dsMonAnThuNgan));

                    decimal tamTinh = dsMonAnThuNgan.Sum(x => x.ThanhTien);
                    if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0;

                    decimal phiPhucVu = tamTinh * 0.05m;
                    decimal thueVAT = tamTinh * 0.10m;
                    decimal giamGia = hd.GiamGia ?? 0;
                    decimal tongCong = tamTinh + phiPhucVu + thueVAT - giamGia;
                    if (tongCong < 0) tongCong = 0;

                    bool laHoaDonGop = danhSachMaBan.Count > 1 || ThuNganDataStore.MergeBackups.ContainsKey(hd.MaHoaDonNhaHang ?? "");

                    DanhSachHoaDon.Add(new HoaDonViewModel
                    {
                        STT = sttFormat,
                        MaHoaDonNhaHang = hd.MaHoaDonNhaHang ?? "",
                        MaBan = maBanCuaHd,
                        TenBan = $"Bàn {soBanHienThi}",
                        HinhThucThanhToan = hd.HinhThucThanhToan ?? "Chưa thanh toán",
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
                }

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải dữ liệu: " + ex.Message);
                return Page();
            }
        }

        // 1. ÁP DỤNG VOUCHER / MÃ GIẢM GIÁ (Chỉ Thu ngân)
        public async Task<IActionResult> OnPostApDungVoucherAsync(string maHoaDonVoucher, string maVoucher)
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (!KiemTraLaThuNgan(vaiTro))
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền áp dụng mã giảm giá!";
                return RedirectToPage();
            }

            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonVoucher);
                if (hd == null || hd.HinhThucThanhToan == "Đã Thanh Toán" || hd.HinhThucThanhToan == "Đã Hủy")
                {
                    TempData["ErrorMessage"] = "Hóa đơn không hợp lệ hoặc đã chốt thanh toán!";
                    return RedirectToPage();
                }

                string code = (maVoucher ?? "").Trim().ToUpper();
                decimal tamTinh = hd.TongTienMonAn ?? 0;
                decimal soTienGiam = 0;

                if (code == "HOLIDAY2024" || code == "VIP10") soTienGiam = tamTinh * 0.10m;
                else if (code == "VIP20") soTienGiam = tamTinh * 0.20m;
                else if (code == "GIAM50K") soTienGiam = 50000;
                else
                {
                    TempData["ErrorMessage"] = $"Mã giảm giá '{code}' không tồn tại! (Gợi ý: HOLIDAY2024, VIP10, VIP20, GIAM50K)";
                    return RedirectToPage();
                }

                hd.GiamGia = soTienGiam;
                hd.ThanhTien = Math.Max(0, tamTinh + (tamTinh * 0.15m) - soTienGiam);
                await _context.SaveChangesAsync();

                ThuNganDataStore.SaveLog(hoTen, vaiTro, "Áp dụng Voucher", hd.MaHoaDonNhaHang, $"Mã {code} - Giảm {soTienGiam:N0} VND");
                TempData["SuccessMessage"] = $"Đã áp dụng mã {code} cho hóa đơn {hd.MaHoaDonNhaHang} (Giảm {soTienGiam:N0} VND)!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi áp dụng voucher: " + ex.Message;
            }
            return RedirectToPage();
        }

        // 2. GỘP BILL LINH HOẠT (Chỉ Thu ngân - Lưu bản sao để hỗ trợ Hủy gộp)
        public async Task<IActionResult> OnPostGopBillAsync(List<string> maHoaDonGopList)
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (!KiemTraLaThuNgan(vaiTro))
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Gộp bill!";
                return RedirectToPage();
            }

            try
            {
                if (maHoaDonGopList == null || maHoaDonGopList.Count < 2)
                {
                    TempData["ErrorMessage"] = "Vui lòng tích chọn ít nhất 2 hóa đơn chưa thanh toán để Gộp bill!";
                    return RedirectToPage();
                }

                var dsHoaDonCanGop = await _context.HOA_DON_NHA_HANG
                    .Where(h => maHoaDonGopList.Contains(h.MaHoaDonNhaHang) && h.HinhThucThanhToan != "Đã Thanh Toán" && h.HinhThucThanhToan != "Đã Hủy")
                    .OrderBy(h => h.ThoiGianTao)
                    .ToListAsync();

                if (dsHoaDonCanGop.Count < 2)
                {
                    TempData["ErrorMessage"] = "Các hóa đơn được chọn không hợp lệ hoặc đã thanh toán!";
                    return RedirectToPage();
                }

                string GetJsonOfHd(HOA_DON_NHA_HANG hd)
                {
                    try
                    {
                        var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) return propJson.GetValue(hd)?.ToString() ?? "[]";
                        return _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";
                    }
                    catch { return "[]"; }
                }

                // Lưu bản sao các hóa đơn gốc trước khi gộp để phục vụ tính năng Hủy gộp
                var banSaoList = dsHoaDonCanGop.Select(h => new BanSaoHoaDonGoc
                {
                    MaHoaDonNhaHang = h.MaHoaDonNhaHang,
                    MaBan = GetMaBanOfHoaDon(h),
                    ThoiGianTao = h.ThoiGianTao ?? DateTime.Now,
                    TongTienMonAn = h.TongTienMonAn ?? 0,
                    GiamGia = h.GiamGia ?? 0,
                    ThanhTien = h.ThanhTien ?? 0,
                    HinhThucThanhToan = h.HinhThucThanhToan ?? "Chờ Thanh Toán",
                    DanhSachMonJson = GetJsonOfHd(h)
                }).ToList();

                var billChinh = dsHoaDonCanGop.First();
                var cacBillPhu = dsHoaDonCanGop.Skip(1).ToList();

                var danhSachMaBanGop = new List<string>();
                string maBanChinh = GetMaBanOfHoaDon(billChinh);
                if (!string.IsNullOrEmpty(maBanChinh)) danhSachMaBanGop.AddRange(maBanChinh.Split(',', StringSplitOptions.RemoveEmptyEntries));

                var mergedJsonElements = new List<JsonElement>();
                try
                {
                    string jsonChinh = GetJsonOfHd(billChinh);
                    if (!string.IsNullOrWhiteSpace(jsonChinh) && jsonChinh != "[]")
                    {
                        using var doc = JsonDocument.Parse(jsonChinh);
                        foreach (var el in doc.RootElement.EnumerateArray())
                            mergedJsonElements.Add(el.Clone());
                    }
                }
                catch { }

                var dsChiTietDb = await _context.CHI_TIET_DICH_VU.ToListAsync();

                foreach (var billPhu in cacBillPhu)
                {
                    billChinh.TongTienMonAn = (billChinh.TongTienMonAn ?? 0) + (billPhu.TongTienMonAn ?? 0);
                    billChinh.GiamGia = (billChinh.GiamGia ?? 0) + (billPhu.GiamGia ?? 0);
                    billChinh.ThanhTien = (billChinh.ThanhTien ?? 0) + (billPhu.ThanhTien ?? 0);

                    string maBanPhu = GetMaBanOfHoaDon(billPhu);
                    if (!string.IsNullOrEmpty(maBanPhu))
                    {
                        foreach (var mb in maBanPhu.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!danhSachMaBanGop.Contains(mb.Trim()))
                                danhSachMaBanGop.Add(mb.Trim());
                        }
                    }

                    try
                    {
                        string jsonPhu = GetJsonOfHd(billPhu);
                        if (!string.IsNullOrWhiteSpace(jsonPhu) && jsonPhu != "[]")
                        {
                            using var docPhu = JsonDocument.Parse(jsonPhu);
                            foreach (var el in docPhu.RootElement.EnumerateArray())
                                mergedJsonElements.Add(el.Clone());
                        }
                    }
                    catch { }

                    foreach (var ct in dsChiTietDb)
                    {
                        string maHdCt = "";
                        try { maHdCt = _context.Entry(ct).Property("MaHoaDonNhaHang").CurrentValue?.ToString() ?? ""; } catch { }
                        if (string.IsNullOrEmpty(maHdCt))
                        {
                            try { maHdCt = _context.Entry(ct).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue?.ToString() ?? ""; } catch { }
                        }

                        if (maHdCt == billPhu.MaHoaDonNhaHang)
                        {
                            try { _context.Entry(ct).Property("MaHoaDonNhaHang").CurrentValue = billChinh.MaHoaDonNhaHang; } catch { }
                            try { _context.Entry(ct).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue = billChinh.MaHoaDonNhaHang; } catch { }
                        }
                    }

                    _context.HOA_DON_NHA_HANG.Remove(billPhu);
                }

                string chuoiMaBanGop = string.Join(",", danhSachMaBanGop);
                var propMb = billChinh.GetType().GetProperty("MaBan") ?? billChinh.GetType().GetProperty("BAN_NHA_HANGMaBan");
                if (propMb != null) propMb.SetValue(billChinh, chuoiMaBanGop);
                try { _context.Entry(billChinh).Property("MaBan").CurrentValue = chuoiMaBanGop; } catch { }
                try { _context.Entry(billChinh).Property("BAN_NHA_HANGMaBan").CurrentValue = chuoiMaBanGop; } catch { }

                try
                {
                    string mergedJsonStr = JsonSerializer.Serialize(mergedJsonElements);
                    var propJson = billChinh.GetType().GetProperty("DanhSachMonJson");
                    if (propJson != null) propJson.SetValue(billChinh, mergedJsonStr);
                    else _context.Entry(billChinh).Property("DanhSachMonJson").CurrentValue = mergedJsonStr;
                }
                catch { }

                billChinh.HinhThucThanhToan = "Chờ Thanh Toán";

                await _context.SaveChangesAsync();

                // Lưu bản sao dự phòng để có thể Hủy gộp nếu chưa thanh toán
                ThuNganDataStore.Load();
                ThuNganDataStore.MergeBackups[billChinh.MaHoaDonNhaHang] = banSaoList;
                ThuNganDataStore.SaveMergeBackups();

                ThuNganDataStore.SaveLog(hoTen, vaiTro, "Gộp bill", billChinh.MaHoaDonNhaHang, $"Gộp {dsHoaDonCanGop.Count} hóa đơn ({chuoiMaBanGop})");
                TempData["SuccessMessage"] = $"Đã gộp {dsHoaDonCanGop.Count} hóa đơn thành 1 bill thống nhất (Mã HD: {billChinh.MaHoaDonNhaHang})!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi gộp bill: " + ex.Message;
            }

            return RedirectToPage();
        }

        // 3. HỦY GỘP BILL CHƯA THANH TOÁN (Chỉ Thu ngân)
        public async Task<IActionResult> OnPostHuyGopBillAsync(string maHoaDonHuyGop)
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (!KiemTraLaThuNgan(vaiTro))
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Hủy gộp bill!";
                return RedirectToPage();
            }

            try
            {
                var billGop = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonHuyGop);
                if (billGop == null || billGop.HinhThucThanhToan == "Đã Thanh Toán" || billGop.HinhThucThanhToan == "Ghi nợ phòng")
                {
                    TempData["ErrorMessage"] = "Chỉ có thể Hủy gộp đối với hóa đơn gộp chưa thanh toán!";
                    return RedirectToPage();
                }

                ThuNganDataStore.Load();
                if (!ThuNganDataStore.MergeBackups.ContainsKey(maHoaDonHuyGop))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dữ liệu tách gốc của hóa đơn gộp này!";
                    return RedirectToPage();
                }

                var banSaoList = ThuNganDataStore.MergeBackups[maHoaDonHuyGop];
                var gocChinh = banSaoList.First();

                // Khôi phục lại bill chính
                billGop.TongTienMonAn = gocChinh.TongTienMonAn;
                billGop.GiamGia = gocChinh.GiamGia;
                billGop.ThanhTien = gocChinh.ThanhTien;
                billGop.HinhThucThanhToan = gocChinh.HinhThucThanhToan;

                var propMb = billGop.GetType().GetProperty("MaBan") ?? billGop.GetType().GetProperty("BAN_NHA_HANGMaBan");
                if (propMb != null) propMb.SetValue(billGop, gocChinh.MaBan);
                try { _context.Entry(billGop).Property("MaBan").CurrentValue = gocChinh.MaBan; } catch { }
                try { _context.Entry(billGop).Property("BAN_NHA_HANGMaBan").CurrentValue = gocChinh.MaBan; } catch { }

                try
                {
                    var propJson = billGop.GetType().GetProperty("DanhSachMonJson");
                    if (propJson != null) propJson.SetValue(billGop, gocChinh.DanhSachMonJson);
                    else _context.Entry(billGop).Property("DanhSachMonJson").CurrentValue = gocChinh.DanhSachMonJson;
                }
                catch { }

                // Tái tạo lại các bill phụ đã bị gộp
                foreach (var gocPhu in banSaoList.Skip(1))
                {
                    var hdPhu = new HOA_DON_NHA_HANG
                    {
                        MaHoaDonNhaHang = gocPhu.MaHoaDonNhaHang,
                        ThoiGianTao = gocPhu.ThoiGianTao,
                        TongTienMonAn = gocPhu.TongTienMonAn,
                        GiamGia = gocPhu.GiamGia,
                        ThanhTien = gocPhu.ThanhTien,
                        HinhThucThanhToan = gocPhu.HinhThucThanhToan
                    };
                    if (propMb != null) propMb.SetValue(hdPhu, gocPhu.MaBan);
                    _context.HOA_DON_NHA_HANG.Add(hdPhu);
                    try { _context.Entry(hdPhu).Property("MaBan").CurrentValue = gocPhu.MaBan; } catch { }
                    try { _context.Entry(hdPhu).Property("BAN_NHA_HANGMaBan").CurrentValue = gocPhu.MaBan; } catch { }
                    try
                    {
                        var propJson = hdPhu.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) propJson.SetValue(hdPhu, gocPhu.DanhSachMonJson);
                        else _context.Entry(hdPhu).Property("DanhSachMonJson").CurrentValue = gocPhu.DanhSachMonJson;
                    }
                    catch { }
                }

                await _context.SaveChangesAsync();

                ThuNganDataStore.MergeBackups.Remove(maHoaDonHuyGop);
                ThuNganDataStore.SaveMergeBackups();

                ThuNganDataStore.SaveLog(hoTen, vaiTro, "Hủy gộp bill", maHoaDonHuyGop, $"Tách lại thành {banSaoList.Count} hóa đơn ban đầu");
                TempData["SuccessMessage"] = $"Đã hủy gộp hóa đơn {maHoaDonHuyGop} về lại {banSaoList.Count} hóa đơn riêng biệt!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi hủy gộp bill: " + ex.Message;
            }

            return RedirectToPage();
        }

        // 4. GHI NỢ VÀO PHÒNG CÓ XÁC NHẬN (Chỉ Thu ngân)
        public async Task<IActionResult> OnPostGhiNoPhongAsync(string maHoaDonGhiNo, string soPhongGhiNo)
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (!KiemTraLaThuNgan(vaiTro))
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Ghi nợ vào phòng!";
                return RedirectToPage();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(maHoaDonGhiNo) || string.IsNullOrWhiteSpace(soPhongGhiNo))
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn số phòng hợp lệ để ghi nợ!";
                    return RedirectToPage();
                }

                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonGhiNo);
                if (hd != null && hd.HinhThucThanhToan != "Đã Thanh Toán")
                {
                    hd.HinhThucThanhToan = "Ghi nợ phòng";

                    // Chuyển đồng loạt các bàn liên quan sang Đang dọn dẹp
                    string maBanCuaHd = GetMaBanOfHoaDon(hd);
                    var dsMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBan = await _context.BAN_NHA_HANG.Where(b => dsMaBan.Contains(b.MaBan)).ToListAsync();
                    foreach (var ban in cacBan) ban.TrangThai = "Đang dọn dẹp";

                    await _context.SaveChangesAsync();

                    ThuNganDataStore.SaveLog(hoTen, vaiTro, "Ghi nợ vào phòng", maHoaDonGhiNo, $"Chuyển công nợ vào Phòng {soPhongGhiNo}");
                    TempData["SuccessMessage"] = $"Đã ghi nợ hóa đơn {maHoaDonGhiNo} vào Phòng {soPhongGhiNo} thành công!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi ghi nợ vào phòng: " + ex.Message;
            }
            return RedirectToPage();
        }

        // 5. THANH TOÁN & CHỐT HÓA ĐƠN (Chỉ Thu ngân - Đồng loạt chuyển các bàn sang Đang dọn dẹp)
        public async Task<IActionResult> OnPostThanhToanAsync(string maHoaDonToPay)
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (!KiemTraLaThuNgan(vaiTro))
            {
                TempData["ErrorMessage"] = "Chỉ tài khoản Thu ngân mới có quyền Thanh toán & Chốt hóa đơn!";
                return RedirectToPage();
            }

            try
            {
                if (string.IsNullOrEmpty(maHoaDonToPay)) return RedirectToPage();

                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonToPay);
                if (hd != null && hd.HinhThucThanhToan != "Đã Thanh Toán")
                {
                    hd.HinhThucThanhToan = "Đã Thanh Toán";

                    string maBanCuaHd = GetMaBanOfHoaDon(hd);
                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBanLienQuan = await _context.BAN_NHA_HANG.Where(b => danhSachMaBan.Contains(b.MaBan)).ToListAsync();

                    foreach (var ban in cacBanLienQuan)
                    {
                        ban.TrangThai = "Đang dọn dẹp";
                        if (ban.GetType().GetProperty("MaHoaDonHienTai") != null)
                        {
                            ban.GetType().GetProperty("MaHoaDonHienTai")?.SetValue(ban, null);
                        }
                    }

                    await _context.SaveChangesAsync();

                    ThuNganDataStore.SaveLog(hoTen, vaiTro, "Thanh toán & Chốt hóa đơn", maHoaDonToPay, $"Đã thu {hd.ThanhTien:N0} VND - Chuyển {cacBanLienQuan.Count} bàn sang Đang dọn dẹp");
                    TempData["SuccessMessage"] = $"Chốt hóa đơn {maHoaDonToPay} thành công! Đã chuyển {cacBanLienQuan.Count} bàn liên quan sang trạng thái Đang dọn dẹp.";

                    // --- BỔ SUNG THÔNG TIN MÃ QR CHUYỂN KHOẢN (KHÔNG ĐỔI LOGIC CŨ) ---
                    long soTien = Convert.ToInt64(Math.Round(hd.ThanhTien ?? 0m));
                    string noiDungCk = $"Thanh toan HD {maHoaDonToPay}";

                    // Cấu hình tài khoản nhận tiền của nhà hàng (đổi theo thông tin thực tế)
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
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi xử lý thanh toán: " + ex.Message;
            }

            return RedirectToPage();
        }

        // 6. HỦY HÓA ĐƠN BẮT BUỘC NHẬP LÝ DO & LƯU LOG (Độc quyền Quản lý)
        public async Task<IActionResult> OnPostHuyHoaDonAsync(string maHoaDonHuy, string lyDoHuy)
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();
            if (!KiemTraLaQuanLy(vaiTro))
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Chỉ tài khoản Quản lý mới có quyền Hủy hóa đơn!";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                TempData["ErrorMessage"] = "Bắt buộc phải nhập lý do hủy hóa đơn để lưu vết kiểm toán!";
                return RedirectToPage();
            }

            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDonHuy);
                if (hd != null)
                {
                    hd.HinhThucThanhToan = "Đã Hủy";

                    string maBanCuaHd = GetMaBanOfHoaDon(hd);
                    var dsMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBan = await _context.BAN_NHA_HANG.Where(b => dsMaBan.Contains(b.MaBan)).ToListAsync();
                    foreach (var ban in cacBan) ban.TrangThai = "Trống";

                    await _context.SaveChangesAsync();

                    ThuNganDataStore.SaveLog(hoTen, vaiTro, "Hủy hóa đơn", maHoaDonHuy, $"Lý do: {lyDoHuy.Trim()}");
                    TempData["SuccessMessage"] = $"Quản lý đã hủy hóa đơn {maHoaDonHuy} và ghi nhận vào Lịch sử thao tác!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi hủy hóa đơn: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}