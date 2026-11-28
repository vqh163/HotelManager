using Microsoft.AspNetCore.Authorization;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.NhaBep
{
    // Lưu trữ trạng thái chế biến của từng Order trên màn hình bếp (Mới -> Đang nấu -> Hoàn tất)
    public static class TrangThaiOrderBepStore
    {
        private static readonly string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "kds_order_state.json");
        public static Dictionary<string, string> TrangThaiDict { get; set; } = new Dictionary<string, string>();

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath));
                    if (data != null) TrangThaiDict = data;
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(TrangThaiDict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
        // [MỚI] Lưu giờ hoàn tất của từng order để dừng đồng hồ
        private static readonly string FilePathHoanTat = Path.Combine(Directory.GetCurrentDirectory(), "kds_order_done_time.json");
        public static Dictionary<string, DateTime> GioHoanTat { get; set; } = new Dictionary<string, DateTime>();

        public static void LoadGioHoanTat()
        {
            try
            {
                if (File.Exists(FilePathHoanTat))
                {
                    var d = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(File.ReadAllText(FilePathHoanTat));
                    if (d != null) GioHoanTat = d;
                }
            }
            catch { }
        }

        public static void SaveGioHoanTat()
        {
            try { File.WriteAllText(FilePathHoanTat, JsonSerializer.Serialize(GioHoanTat)); }
            catch { }
        }
    }

    // ViewModel hiển thị từng thẻ Order trên màn hình KDS
    public class OrderBepViewModel
    {
        public string MaHoaDonNhaHang { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;
        public DateTime ThoiGianTao { get; set; }
        public long ThoiGianTaoUnixSeconds { get; set; }
        public string ThoiGianCho { get; set; } = "00:00";
        public string MucDoUuTien { get; set; } = "NORMAL";
        public string TrangThaiBep { get; set; } = "Mới"; // "Mới", "Đang nấu", "Hoàn tất"
        public List<MonOrderBepItem> DanhSachMon { get; set; } = new List<MonOrderBepItem>();
    }

    // ViewModel chi tiết từng món ăn trong Order
    public class MonOrderBepItem
    {
        public string MaDichVu { get; set; } = string.Empty;
        public string TenMon { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public string GhiChu { get; set; } = string.Empty;
        public string CanhBaoDiUng { get; set; } = string.Empty;
        public bool LaMonGoiThem { get; set; } = false;
    }
    [Authorize(Roles = "Đầu bếp,Quản lý,Giám đốc")]
    public class QuanLyOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IActionDescriptorCollectionProvider _actionProvider;
        private readonly ILogger<QuanLyOrderModel> _logger;

        public QuanLyOrderModel(
            ApplicationDbContext context,
            IActionDescriptorCollectionProvider actionProvider,
            ILogger<QuanLyOrderModel> logger)
        {
            _context = context;
            _actionProvider = actionProvider;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; } = "Tất cả";

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        // Phân quyền chặt chẽ:
        // - LaQuanLy = true: Giám sát, xóa đơn lỗi, truy cập Quản lý Thực Đơn nhưng không bấm nút chế biến.
        // - LaDauBep = true: Độc quyền thao tác các nút chế biến ("Bắt đầu chế biến", "Hoàn tất").
        public bool LaQuanLy { get; set; } = false;
        public bool LaDauBep { get; set; } = false;

        public string TenNhanVienHienThi { get; set; } = "Nhân viên Bếp";
        public string VaiTroHienThi { get; set; } = "Đầu Bếp";

        public int SoLuongMoi { get; set; }
        public int SoLuongDangNau { get; set; }
        public int SoLuongHoanTat { get; set; }

        public List<OrderBepViewModel> DanhSachOrder { get; set; } = new List<OrderBepViewModel>();

        // Hàm tự động dò đường dẫn thực tế của Razor Page trong Solution
        public string TimDuongDanTrang(string tenTrang, string duongDanMacDinh)
        {
            var page = _actionProvider.ActionDescriptors.Items
                .OfType<PageActionDescriptor>()
                .FirstOrDefault(p => p.ViewEnginePath.EndsWith("/" + tenTrang, StringComparison.OrdinalIgnoreCase)
                                  || p.ViewEnginePath.Equals("/" + tenTrang, StringComparison.OrdinalIgnoreCase));
            return page != null ? page.ViewEnginePath : duongDanMacDinh;
        }

        private bool KiemTraLaQuanLy()
        {
            return User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);
        }

        private bool KiemTraLaDauBep()
        {
            return !KiemTraLaQuanLy() && User.IsInRole(HangSoHeThong.VaiTroDauBep);
        }

        // Lấy họ tên hiển thị của nhân sự đang đăng nhập bằng LINQ
        private async Task<string> LayTenNhanVienHienThiAsync()
        {
            string? maNv = User.FindFirst("MaNV")?.Value;
            if (!string.IsNullOrWhiteSpace(maNv))
            {
                var nv = await _context.NHAN_VIEN.AsNoTracking().FirstOrDefaultAsync(n => n.MaNV == maNv);
                if (nv != null && !string.IsNullOrWhiteSpace(nv.HoTen))
                    return nv.HoTen.Trim();
            }

            return User.Identity?.Name ?? "Nhân viên Bếp";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LaQuanLy = KiemTraLaQuanLy();
            LaDauBep = KiemTraLaDauBep();

            if (!LaQuanLy && !LaDauBep)
            {
                return RedirectToPage("/Auth/DangNhap");
            }

            TenNhanVienHienThi = await LayTenNhanVienHienThiAsync();
            VaiTroHienThi = LaQuanLy ? "Quản Lý" : "Đầu Bếp";

            try
            {
                TrangThaiOrderBepStore.Load();
                TrangThaiOrderBepStore.LoadGioHoanTat();   

                // Truy vấn danh mục món ăn và cảnh báo dị ứng trực tiếp từ SQL Server (bảng DICH_VU)
                var dsDichVuDb = await _context.DICH_VU.AsNoTracking().ToListAsync();
                var dictDichVu = dsDichVuDb.ToDictionary(d => d.MaDichVu.Trim(), d => d.TenDichVu ?? "Món ăn");
                var dictDiUng = dsDichVuDb.ToDictionary(d => d.MaDichVu.Trim(), d => d.CanhBaoDiUng ?? string.Empty);

                // Lấy toàn bộ hóa đơn nhà hàng đang mở (loại hóa đơn đã thanh toán / ghi nợ phòng / đã hủy)
                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                    .Where(h => (h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                              || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan
                              || string.IsNullOrEmpty(h.TrangThaiThanhToan))
                             && h.HinhThucThanhToan != "Đã Thanh Toán"
                             && h.HinhThucThanhToan != "Ghi nợ phòng"
                             && h.HinhThucThanhToan != "Đã Hủy")
                    .OrderByDescending(h => h.ThoiGianTao)
                    .ToListAsync();

                var dsBanDb = await _context.BAN_NHA_HANG.AsNoTracking().ToListAsync();
                var maHdList = dsHoaDonActive.Select(h => h.MaHoaDonNhaHang).ToList();
                var dsChiTietDb = await _context.CHI_TIET_DICH_VU.AsNoTracking()
                    .Where(c => c.HOA_DON_NHA_HANGMaHoaDonNhaHang != null && maHdList.Contains(c.HOA_DON_NHA_HANGMaHoaDonNhaHang))
                    .ToListAsync();

                var tatCaOrder = new List<OrderBepViewModel>();

                foreach (var hd in dsHoaDonActive)
                {
                    string maHd = (hd.MaHoaDonNhaHang ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(maHd)) continue;

                    string maBan = (hd.BAN_NHA_HANGMaBan ?? string.Empty).Trim();
                    var cacMaBan = maBan.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var cacBan = dsBanDb.Where(b => cacMaBan.Contains(b.MaBan.Trim())).ToList();
                    string tenBanHienThi = cacBan.Any()
                        ? string.Join(", ", cacBan.Select(b =>
                        {
                            string sb = (b.TenBan ?? b.MaBan).Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                            if (sb.Length == 1 && char.IsDigit(sb[0])) sb = "0" + sb;
                            return sb;
                        }))
                        : maBan;
                    if (string.IsNullOrEmpty(tenBanHienThi)) tenBanHienThi = "01";

                    string jsonMon = hd.DanhSachMonJson ?? "[]";
                    var items = new List<MonOrderBepItem>();
                    string? trangThaiTrongJson = null;

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(jsonMon) && jsonMon != "[]")
                        {
                            using var doc = JsonDocument.Parse(jsonMon);
                            foreach (var el in doc.RootElement.EnumerateArray())
                            {
                                string maDv = string.Empty;
                                if (el.TryGetProperty("MaDichVu", out var m1)) maDv = m1.GetString() ?? string.Empty;
                                else if (el.TryGetProperty("maDichVu", out var m2)) maDv = m2.GetString() ?? string.Empty;

                                string tenMon = string.Empty;
                                if (el.TryGetProperty("TenMon", out var t1)) tenMon = t1.GetString() ?? string.Empty;
                                else if (el.TryGetProperty("tenDichVu", out var t2)) tenMon = t2.GetString() ?? string.Empty;

                                if (string.IsNullOrEmpty(tenMon) && !string.IsNullOrEmpty(maDv) && dictDichVu.TryGetValue(maDv, out var tenDb))
                                {
                                    tenMon = tenDb;
                                }

                                if (string.IsNullOrEmpty(maDv) && !string.IsNullOrEmpty(tenMon))
                                {
                                    var matchedDv = dsDichVuDb.FirstOrDefault(d => string.Equals(d.TenDichVu, tenMon, StringComparison.OrdinalIgnoreCase));
                                    if (matchedDv != null) maDv = matchedDv.MaDichVu;
                                }

                                int sl = 1;
                                if (el.TryGetProperty("SoLuong", out var s1)) sl = s1.GetInt32();
                                else if (el.TryGetProperty("soLuong", out var s2)) sl = s2.GetInt32();

                                string gc = string.Empty;
                                if (el.TryGetProperty("GhiChu", out var g1)) gc = g1.GetString() ?? string.Empty;
                                else if (el.TryGetProperty("ghiChu", out var g2)) gc = g2.GetString() ?? string.Empty;

                                if (trangThaiTrongJson == null && el.TryGetProperty("TrangThaiBep", out var ttb))
                                {
                                    trangThaiTrongJson = ttb.GetString();
                                }

                                string diUng = (!string.IsNullOrEmpty(maDv) && dictDiUng.TryGetValue(maDv, out var du))
                                    ? du
                                    : string.Empty;

                                if (string.IsNullOrEmpty(diUng) && !string.IsNullOrEmpty(gc) &&
                                    gc.Contains("dị ứng", StringComparison.OrdinalIgnoreCase))
                                {
                                    diUng = gc.ToUpper();
                                }

                                bool laMonGoiThem =
                                    (el.TryGetProperty("LaMonGoiThem", out var gt1) && gt1.ValueKind == JsonValueKind.True) ||
                                    (el.TryGetProperty("laMonGoiThem", out var gt2) && gt2.ValueKind == JsonValueKind.True);

                                items.Add(new MonOrderBepItem
                                {
                                    MaDichVu = maDv,
                                    TenMon = string.IsNullOrEmpty(tenMon) ? "Món ăn" : tenMon,
                                    SoLuong = sl,
                                    GhiChu = gc,
                                    CanhBaoDiUng = diUng,
                                    LaMonGoiThem = laMonGoiThem
                                });
                            }
                        }
                    }
                    catch { }

                    // Dự phòng đọc từ CHI_TIET_DICH_VU nếu hóa đơn không có JSON món ăn
                    if (!items.Any())
                    {
                        foreach (var ct in dsChiTietDb.Where(c => c.HOA_DON_NHA_HANGMaHoaDonNhaHang == maHd))
                        {
                            string maDvCt = (ct.DICH_VUMaDichVu ?? string.Empty).Trim();
                            string tenMonCt = (!string.IsNullOrEmpty(maDvCt) && dictDichVu.TryGetValue(maDvCt, out var t)) ? t : "Món ăn";
                            string diUngCt = (!string.IsNullOrEmpty(maDvCt) && dictDiUng.TryGetValue(maDvCt, out var d)) ? d : string.Empty;
                            int slCt = ct.SoLuong ?? 1;

                            items.Add(new MonOrderBepItem
                            {
                                MaDichVu = maDvCt,
                                TenMon = tenMonCt,
                                SoLuong = slCt > 0 ? slCt : 1,
                                GhiChu = string.Empty,
                                CanhBaoDiUng = diUngCt
                            });
                        }
                    }

                    if (!items.Any()) continue;

                    string trangThaiBep = "Mới";
                    if (TrangThaiOrderBepStore.TrangThaiDict.TryGetValue(maHd, out var ttStore))
                    {
                        trangThaiBep = ttStore;
                    }
                    else if (trangThaiTrongJson == "Đang nấu" || trangThaiTrongJson == "Hoàn tất")
                    {
                        trangThaiBep = trangThaiTrongJson;
                    }

                    DateTime tgTao = hd.ThoiGianTao ?? DateTime.Now;
                    // [SỬA] Order đã hoàn tất thì dừng đồng hồ tại giờ hoàn tất
                    DateTime moc = DateTime.Now;
                    if (trangThaiBep == "Hoàn tất" && TrangThaiOrderBepStore.GioHoanTat.TryGetValue(maHd, out var gioXong))
                        moc = gioXong;

                    TimeSpan elapsed = moc - tgTao;
                    if (elapsed.TotalSeconds < 0) elapsed = TimeSpan.Zero;

                    string thoiGianCho = elapsed.TotalHours >= 1
                        ? $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                        : $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";

                    long unixSec = new DateTimeOffset(DateTime.SpecifyKind(tgTao, DateTimeKind.Local)).ToUnixTimeSeconds();

                    bool coCanhBaoDiUng = items.Any(i => !string.IsNullOrEmpty(i.CanhBaoDiUng));
                    string uuTien = coCanhBaoDiUng ? "HIGH" : (items.Sum(i => i.SoLuong) >= 4 ? "URGENT" : "NORMAL");

                    tatCaOrder.Add(new OrderBepViewModel
                    {
                        MaHoaDonNhaHang = maHd,
                        TenBan = tenBanHienThi,
                        ThoiGianTao = tgTao,
                        ThoiGianTaoUnixSeconds = unixSec,
                        ThoiGianCho = thoiGianCho,
                        MucDoUuTien = uuTien,
                        TrangThaiBep = trangThaiBep,
                        DanhSachMon = items
                    });
                }

                SoLuongMoi = tatCaOrder.Count(o => o.TrangThaiBep == "Mới");
                SoLuongDangNau = tatCaOrder.Count(o => o.TrangThaiBep == "Đang nấu");
                SoLuongHoanTat = tatCaOrder.Count(o => o.TrangThaiBep == "Hoàn tất");

                var filtered = tatCaOrder.AsEnumerable();
                if (string.IsNullOrEmpty(TrangThaiLoc) || TrangThaiLoc == "Tất cả")
                {
                    // [SỬA] "Tất cả": ẩn order đã hoàn tất quá 30 phút
                    filtered = filtered.Where(o => o.TrangThaiBep != "Hoàn tất"
                        || !TrangThaiOrderBepStore.GioHoanTat.TryGetValue(o.MaHoaDonNhaHang, out var g)
                        || (DateTime.Now - g).TotalMinutes <= 30);
                }
                else if (TrangThaiLoc == "Xong" || TrangThaiLoc == "Hoàn tất")
                {
                    filtered = filtered.Where(o => o.TrangThaiBep == "Hoàn tất");
                }
                else
                {
                    filtered = filtered.Where(o => o.TrangThaiBep == TrangThaiLoc);
                }

                if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                {
                    string kw = TuKhoaTimKiem.Trim();
                    filtered = filtered.Where(o =>
                        o.TenBan.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        o.MaHoaDonNhaHang.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                        o.DanhSachMon.Any(m => m.TenMon.Contains(kw, StringComparison.OrdinalIgnoreCase)));
                }

                DanhSachOrder = filtered.ToList();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải màn hình Quản lý Order (KDS).");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi tải danh sách đơn bếp.");
                return Page();
            }
        }

        // Xử lý chế biến món ăn: Chỉ Đầu bếp được phép bấm chuyển trạng thái chế biến
        public async Task<IActionResult> OnPostChuyenTrangThaiOrderAsync(string maHoaDon, string trangThaiMoi)
        {
            if (!KiemTraLaDauBep())
            {
                TempData["ErrorMessage"] = "Phân quyền hệ thống: Tài khoản Quản lý chỉ giám sát và quản trị, quyền bấm nút xử lý chế biến dành riêng cho Đầu bếp!";
                return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(maHoaDon) && !string.IsNullOrWhiteSpace(trangThaiMoi))
                {
                    await Task.Run(() =>
                    {
                        string ma = maHoaDon.Trim();
                        string tt = trangThaiMoi.Trim();

                        TrangThaiOrderBepStore.Load();
                        TrangThaiOrderBepStore.TrangThaiDict[ma] = tt;
                        TrangThaiOrderBepStore.Save();

                        // Ghi / xóa giờ hoàn tất để dừng đồng hồ KDS
                        TrangThaiOrderBepStore.LoadGioHoanTat();
                        if (tt == "Hoàn tất")
                        {
                            if (!TrangThaiOrderBepStore.GioHoanTat.ContainsKey(ma))
                                TrangThaiOrderBepStore.GioHoanTat[ma] = DateTime.Now;
                        }
                        else
                        {
                            TrangThaiOrderBepStore.GioHoanTat.Remove(ma);
                        }
                        TrangThaiOrderBepStore.SaveGioHoanTat();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật trạng thái chế biến đơn {MaHD}", maHoaDon);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật trạng thái chế biến.";
            }

            return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
        }

        // Chức năng dành riêng cho Quản lý: Xóa đơn lỗi trên KDS và giải phóng đồng loạt bàn liên quan
        public async Task<IActionResult> OnPostXoaOrderQuanLyAsync(string maHoaDon)
        {
            if (!KiemTraLaQuanLy())
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Chỉ Quản lý mới có quyền xóa dữ liệu đơn hàng!";
                return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDon);
                if (hd != null)
                {
                    // Giải phóng toàn bộ các bàn liên quan (hỗ trợ cả bàn gộp) về trạng thái Trống
                    if (!string.IsNullOrWhiteSpace(hd.BAN_NHA_HANGMaBan))
                    {
                        var dsMaBan = hd.BAN_NHA_HANGMaBan
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .ToList();

                        var cacBan = await _context.BAN_NHA_HANG
                            .Where(b => dsMaBan.Contains(b.MaBan))
                            .ToListAsync();

                        foreach (var ban in cacBan)
                        {
                            ban.TrangThai = "Trống";
                            ban.TenKhachDat = null;
                        }
                    }

                    // Xóa các chi tiết dịch vụ liên quan trước khi xóa hóa đơn để đảm bảo toàn vẹn khóa ngoại
                    var chiTiets = await _context.CHI_TIET_DICH_VU
                        .Where(c => c.HOA_DON_NHA_HANGMaHoaDonNhaHang == maHoaDon)
                        .ToListAsync();
                    if (chiTiets.Any())
                    {
                        _context.CHI_TIET_DICH_VU.RemoveRange(chiTiets);
                    }

                    _context.HOA_DON_NHA_HANG.Remove(hd);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TrangThaiOrderBepStore.Load();
                    if (TrangThaiOrderBepStore.TrangThaiDict.ContainsKey(maHoaDon))
                    {
                        TrangThaiOrderBepStore.TrangThaiDict.Remove(maHoaDon);
                        TrangThaiOrderBepStore.Save();
                    }

                    TempData["SuccessMessage"] = $"Quản lý đã xóa đơn #{maHoaDon} và đồng bộ giải phóng bàn thành công!";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xóa đơn hàng KDS {MaHD}", maHoaDon);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi xóa đơn hàng.";
            }

            return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
        }
    }
}
