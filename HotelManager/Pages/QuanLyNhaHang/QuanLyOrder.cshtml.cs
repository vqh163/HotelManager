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
using System.Text.RegularExpressions;
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
    [Authorize(Roles = "Đầu bếp, Quản lý")]
    public class QuanLyOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IActionDescriptorCollectionProvider _actionProvider;

        public QuanLyOrderModel(ApplicationDbContext context, IActionDescriptorCollectionProvider actionProvider)
        {
            _context = context;
            _actionProvider = actionProvider;
        }

        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; } = "Tất cả";

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        // Phân quyền chặt chẽ:
        // - LaQuanLy = true: Có toàn quyền quản trị, xóa đơn, chuẩn hóa DB, vào Quản lý Thực Đơn nhưng KHÔNG được bấm nút chế biến.
        // - LaDauBep = true: Độc quyền thao tác các nút chế biến ("Bắt đầu chế biến", "Đang chế biến", "Đã hoàn thành").
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

        // Truy vấn trực tiếp VaiTro & HoTen từ bảng TAI_KHOAN và NHAN_VIEN trong SQL Server
        private async Task<(string vaiTro, string hoTen)> LayThongTinNguoiDungAsync()
        {
            string tenDangNhap = User?.Identity?.Name
                              ?? User?.FindFirst(ClaimTypes.Name)?.Value
                              ?? User?.FindFirst("TenDangNhap")?.Value
                              ?? User?.FindFirst("Username")?.Value
                              ?? Request.Cookies["GrandHorizon_User"]
                              ?? string.Empty;

            string maTaiKhoan = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User?.FindFirst("MaTaiKhoan")?.Value
                             ?? string.Empty;

            if (string.IsNullOrEmpty(tenDangNhap))
            {
                try
                {
                    tenDangNhap = HttpContext.Session.GetString("TenDangNhap")
                               ?? HttpContext.Session.GetString("Username")
                               ?? HttpContext.Session.GetString("User")
                               ?? string.Empty;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(maTaiKhoan))
            {
                try { maTaiKhoan = HttpContext.Session.GetString("MaTaiKhoan") ?? string.Empty; } catch { }
            }

            string vaiTro = string.Empty;
            string hoTen = string.Empty;

            if (!string.IsNullOrEmpty(tenDangNhap) || !string.IsNullOrEmpty(maTaiKhoan))
            {
                var tkDb = await _context.TAI_KHOAN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap || t.MaTaiKhoan == maTaiKhoan);

                if (tkDb != null)
                {
                    vaiTro = (tkDb.VaiTro ?? string.Empty).Trim();
                    hoTen = tkDb.TenDangNhap ?? string.Empty;

                    try
                    {
                        var nvList = await _context.NHAN_VIEN.AsNoTracking().ToListAsync();
                        var propMaNv = tkDb.GetType().GetProperty("MaNV") ?? tkDb.GetType().GetProperty("NHAN_VIENMaNV");
                        string? maNv = propMaNv?.GetValue(tkDb)?.ToString();
                        if (!string.IsNullOrEmpty(maNv))
                        {
                            var nv = nvList.FirstOrDefault(n => n.MaNV == maNv);
                            if (nv != null && !string.IsNullOrEmpty(nv.HoTen)) hoTen = nv.HoTen;
                        }
                    }
                    catch { }
                }
            }

            if (string.IsNullOrEmpty(vaiTro))
            {
                vaiTro = User?.FindFirst(ClaimTypes.Role)?.Value
                      ?? User?.FindFirst("VaiTro")?.Value
                      ?? User?.FindFirst("Role")?.Value
                      ?? Request.Cookies["GrandHorizon_Role"]
                      ?? string.Empty;

                if (string.IsNullOrEmpty(vaiTro))
                {
                    try { vaiTro = HttpContext.Session.GetString("VaiTro") ?? HttpContext.Session.GetString("Role") ?? string.Empty; } catch { }
                }
            }

            if (string.IsNullOrEmpty(hoTen))
            {
                hoTen = !string.IsNullOrEmpty(tenDangNhap) ? tenDangNhap : "Trần Minh Đức";
            }

            return (vaiTro.Trim(), hoTen.Trim());
        }

        private bool KiemTraLaQuanLy(string vaiTro)
        {
            if (string.IsNullOrEmpty(vaiTro)) return false;
            return vaiTro.Contains("Quản lý", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("QuanLy", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Giám đốc", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Quản trị", StringComparison.OrdinalIgnoreCase);
        }

        private bool KiemTraLaDauBep(string vaiTro)
        {
            if (string.IsNullOrEmpty(vaiTro)) return false;
            if (KiemTraLaQuanLy(vaiTro)) return false; // Tách biệt rõ Quản lý và Đầu bếp

            return vaiTro.Contains("Đầu bếp", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("DauBep", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Bếp", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Bep", StringComparison.OrdinalIgnoreCase);
        }

        // HÀM LÀM SẠCH & CHUẨN HÓA DỮ LIỆU CŨ TRONG CSDL SANG ĐỊNH DẠNG MỚI (YYMMDD + STT)
        public async Task ChuanHoaDuLieuCuAsync()
        {
            try
            {
                TrangThaiOrderBepStore.Load();
                bool hasChanges = false;

                // 1. Làm sạch tên bàn (Đảm bảo đồng bộ số bàn dạng 2 chữ số: 01, 02, 04...)
                var dsBan = await _context.BAN_NHA_HANG.ToListAsync();
                foreach (var ban in dsBan)
                {
                    if (string.IsNullOrEmpty(ban.TrangThai))
                    {
                        ban.TrangThai = "Trống";
                        hasChanges = true;
                    }
                }

                // 2. Làm sạch và chuẩn hóa toàn bộ Mã Hóa Đơn sang chuẩn YYMMDD + STT (VD: 26111401)
                var dsHoaDon = await _context.HOA_DON_NHA_HANG
                    .OrderBy(h => h.ThoiGianTao)
                    .ToListAsync();

                var dsChiTiet = await _context.CHI_TIET_DICH_VU.ToListAsync();

                // Nhóm các hóa đơn theo ngày tạo (YYMMDD) để đánh lại STT chuẩn xác
                var nhomTheoNgay = dsHoaDon.GroupBy(h => (h.ThoiGianTao ?? DateTime.Now).ToString("yyMMdd")).ToList();

                foreach (var nhom in nhomTheoNgay)
                {
                    string datePrefix = nhom.Key;
                    int stt = 1;

                    // Giữ lại các mã đã đúng chuẩn YYMMDD + STT (8 chữ số thuần)
                    var tapMaDaChuan = new HashSet<string>(
                        nhom.Where(h => Regex.IsMatch(h.MaHoaDonNhaHang ?? "", @"^\d{8,}$"))
                            .Select(h => h.MaHoaDonNhaHang!)
                    );

                    foreach (var hd in nhom)
                    {
                        string oldId = (hd.MaHoaDonNhaHang ?? string.Empty).Trim();
                        // Kiểm tra nếu mã cũ còn chứa chữ "HD" hoặc ký tự chữ cái ngẫu nhiên (VD: HD261104131824EA77)
                        if (!Regex.IsMatch(oldId, @"^\d{8,9}$"))
                        {
                            string newId;
                            do
                            {
                                newId = $"{datePrefix}{stt:D2}";
                                stt++;
                            }
                            while (tapMaDaChuan.Contains(newId) || dsHoaDon.Any(x => x.MaHoaDonNhaHang == newId));

                            tapMaDaChuan.Add(newId);

                            // Tạo bản ghi HOA_DON_NHA_HANG mới với khóa chính chuẩn YYMMDD + STT
                            var hdMoi = new HOA_DON_NHA_HANG
                            {
                                MaHoaDonNhaHang = newId,
                                ThoiGianTao = hd.ThoiGianTao ?? DateTime.Now,
                                TongTienMonAn = hd.TongTienMonAn ?? 0,
                                GiamGia = hd.GiamGia ?? 0,
                                ThanhTien = hd.ThanhTien ?? 0,
                                HinhThucThanhToan = hd.HinhThucThanhToan ?? "Chưa thanh toán"
                            };

                            // Sao chép khóa ngoại MaBan và DanhSachMonJson sang hóa đơn chuẩn mới
                            string maBanCuaHd = string.Empty;
                            var propMb = hd.GetType().GetProperty("MaBan") ?? hd.GetType().GetProperty("BAN_NHA_HANGMaBan");
                            if (propMb != null) maBanCuaHd = propMb.GetValue(hd)?.ToString() ?? string.Empty;
                            if (string.IsNullOrEmpty(maBanCuaHd))
                            {
                                try { maBanCuaHd = _context.Entry(hd).Property("MaBan").CurrentValue?.ToString() ?? string.Empty; } catch { }
                            }
                            if (string.IsNullOrEmpty(maBanCuaHd))
                            {
                                try { maBanCuaHd = _context.Entry(hd).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? string.Empty; } catch { }
                            }

                            if (propMb != null) propMb.SetValue(hdMoi, maBanCuaHd);
                            _context.HOA_DON_NHA_HANG.Add(hdMoi);
                            try { _context.Entry(hdMoi).Property("MaBan").CurrentValue = maBanCuaHd; } catch { }
                            try { _context.Entry(hdMoi).Property("BAN_NHA_HANGMaBan").CurrentValue = maBanCuaHd; } catch { }

                            try
                            {
                                string jsonMon = string.Empty;
                                var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                                if (propJson != null) jsonMon = propJson.GetValue(hd)?.ToString() ?? string.Empty;
                                else jsonMon = _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? string.Empty;

                                if (!string.IsNullOrEmpty(jsonMon))
                                {
                                    if (propJson != null) propJson.SetValue(hdMoi, jsonMon);
                                    else _context.Entry(hdMoi).Property("DanhSachMonJson").CurrentValue = jsonMon;
                                }
                            }
                            catch { }

                            // Cập nhật toàn bộ CHI_TIET_DICH_VU con sang mã hóa đơn mới
                            foreach (var ct in dsChiTiet)
                            {
                                string maHdCt = string.Empty;
                                try { maHdCt = _context.Entry(ct).Property("MaHoaDonNhaHang").CurrentValue?.ToString() ?? string.Empty; } catch { }
                                if (string.IsNullOrEmpty(maHdCt))
                                {
                                    try { maHdCt = _context.Entry(ct).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue?.ToString() ?? string.Empty; } catch { }
                                }

                                if (maHdCt.Trim() == oldId)
                                {
                                    try { _context.Entry(ct).Property("MaHoaDonNhaHang").CurrentValue = newId; } catch { }
                                    try { _context.Entry(ct).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue = newId; } catch { }
                                }
                            }

                            // Đồng bộ trạng thái KDS từ mã cũ sang mã mới
                            if (TrangThaiOrderBepStore.TrangThaiDict.ContainsKey(oldId))
                            {
                                TrangThaiOrderBepStore.TrangThaiDict[newId] = TrangThaiOrderBepStore.TrangThaiDict[oldId];
                                TrangThaiOrderBepStore.TrangThaiDict.Remove(oldId);
                            }

                            // Xóa bản ghi hóa đơn mang định dạng cũ
                            _context.HOA_DON_NHA_HANG.Remove(hd);
                            hasChanges = true;
                        }
                    }
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync();
                    TrangThaiOrderBepStore.Save();
                }
            }
            catch { /* Đảm bảo an toàn không làm gián đoạn luồng tải trang nếu có ràng buộc FK cứng */ }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var (vaiTro, hoTen) = await LayThongTinNguoiDungAsync();

            // 1. Nếu chưa đăng nhập -> Tự động chuyển về trang Đăng nhập
            if (string.IsNullOrEmpty(vaiTro))
            {
                return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));
            }

            // 2. Tự động điều hướng thẳng về màn hình chuyên môn nếu nhân viên bộ phận khác truy cập vào:
            if (vaiTro.Contains("Phục vụ", StringComparison.OrdinalIgnoreCase) ||
                vaiTro.Contains("PhucVu", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(TimDuongDanTrang("SoDoBan", "/NhaHang/PhucVu/SoDoBan"));
            }

            if (vaiTro.Contains("Thu ngân", StringComparison.OrdinalIgnoreCase) ||
                vaiTro.Contains("ThuNgan", StringComparison.OrdinalIgnoreCase) ||
                vaiTro.Contains("Kế toán", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(TimDuongDanTrang("QuayThuNgan", "/NhaHang/ThuNgan/QuayThuNgan"));
            }

            LaQuanLy = KiemTraLaQuanLy(vaiTro);
            LaDauBep = KiemTraLaDauBep(vaiTro);

            // Chỉ cho phép Đầu bếp và Quản lý truy cập màn hình Quản lý Order (KDS)
            if (!LaQuanLy && !LaDauBep)
            {
                return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));
            }

            TenNhanVienHienThi = hoTen;
            VaiTroHienThi = LaQuanLy ? "Quản Lý" : "Đầu Bếp";

            try
            {
                // Tự động chạy làm sạch và chuẩn hóa mã hóa đơn cũ sang YYMMDD + STT
                await ChuanHoaDuLieuCuAsync();

                TrangThaiMonStore.LoadData();
                TrangThaiOrderBepStore.Load();
                TrangThaiOrderBepStore.LoadGioHoanTat();   

                var dsDichVuDb = await _context.DICH_VU.AsNoTracking().ToListAsync();
                var dictDichVu = dsDichVuDb.ToDictionary(d => d.MaDichVu.Trim(), d => d.TenDichVu ?? "Món ăn");

                // [SỬA] Loại hóa đơn đã chốt (thanh toán / ghi nợ / hủy) khỏi màn hình bếp
                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG
                    .Where(h => h.HinhThucThanhToan != "Đã Thanh Toán"
                             && h.HinhThucThanhToan != "Ghi nợ phòng"
                             && h.HinhThucThanhToan != "Đã Hủy")
                    .OrderByDescending(h => h.ThoiGianTao)
                    .ToListAsync();

                var dsBanDb = await _context.BAN_NHA_HANG.AsNoTracking().ToListAsync();
                var dsChiTietDb = await _context.CHI_TIET_DICH_VU.AsNoTracking().ToListAsync();

                var tatCaOrder = new List<OrderBepViewModel>();

                foreach (var hd in dsHoaDonActive)
                {
                    string maHd = (hd.MaHoaDonNhaHang ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(maHd)) continue;

                    string maBan = string.Empty;
                    var prop = hd.GetType().GetProperty("MaBan") ?? hd.GetType().GetProperty("BAN_NHA_HANGMaBan");
                    if (prop != null) maBan = prop.GetValue(hd)?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(maBan))
                    {
                        try { maBan = _context.Entry(hd).Property("MaBan").CurrentValue?.ToString() ?? string.Empty; } catch { }
                    }
                    if (string.IsNullOrEmpty(maBan))
                    {
                        try { maBan = _context.Entry(hd).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? string.Empty; } catch { }
                    }

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

                    string jsonMon = "[]";
                    try
                    {
                        var propJson = hd.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) jsonMon = propJson.GetValue(hd)?.ToString() ?? "[]";
                        else jsonMon = _context.Entry(hd).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";
                    }
                    catch { }

                    var items = new List<MonOrderBepItem>();
                    string? trangThaiTrongJson = null;

                    try
                    {
                        if (!string.IsNullOrEmpty(jsonMon) && jsonMon != "[]")
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

                                if (string.IsNullOrEmpty(tenMon) && !string.IsNullOrEmpty(maDv) && dictDichVu.ContainsKey(maDv))
                                {
                                    tenMon = dictDichVu[maDv];
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

                                string diUng = string.Empty;
                                if (!string.IsNullOrEmpty(maDv) && TrangThaiMonStore.DiUngDict.ContainsKey(maDv))
                                {
                                    diUng = TrangThaiMonStore.DiUngDict[maDv];
                                }

                                if (string.IsNullOrEmpty(diUng) && !string.IsNullOrEmpty(gc) &&
                                    gc.Contains("dị ứng", StringComparison.OrdinalIgnoreCase))
                                {
                                    diUng = gc.ToUpper();
                                }
                                bool laMonGoiThem = false;
                                if (el.TryGetProperty("LaMonGoiThem", out var gt1) && gt1.ValueKind == JsonValueKind.True)
                                {
                                    laMonGoiThem = true;
                                }
                                else if (el.TryGetProperty("laMonGoiThem", out var gt2) && gt2.ValueKind == JsonValueKind.True)
                                {
                                    laMonGoiThem = true;
                                }
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

                    if (!items.Any())
                    {
                        foreach (var ct in dsChiTietDb)
                        {
                            string maHdCt = string.Empty;
                            try { maHdCt = _context.Entry(ct).Property("MaHoaDonNhaHang").CurrentValue?.ToString() ?? string.Empty; } catch { }
                            if (string.IsNullOrEmpty(maHdCt))
                            {
                                try { maHdCt = _context.Entry(ct).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue?.ToString() ?? string.Empty; } catch { }
                            }

                            if (maHdCt.Trim() == maHd)
                            {
                                string maDvCt = string.Empty;
                                try { maDvCt = _context.Entry(ct).Property("MaDichVu").CurrentValue?.ToString() ?? string.Empty; } catch { }
                                if (string.IsNullOrEmpty(maDvCt))
                                {
                                    try { maDvCt = _context.Entry(ct).Property("DICH_VUMaDichVu").CurrentValue?.ToString() ?? string.Empty; } catch { }
                                }

                                string tenMonCt = (!string.IsNullOrEmpty(maDvCt) && dictDichVu.ContainsKey(maDvCt)) ? dictDichVu[maDvCt] : "Món ăn";
                                string diUngCt = (!string.IsNullOrEmpty(maDvCt) && TrangThaiMonStore.DiUngDict.ContainsKey(maDvCt)) ? TrangThaiMonStore.DiUngDict[maDvCt] : string.Empty;
                                int.TryParse(ct.SoLuong?.ToString(), out int slCt);
                                items.Add(new MonOrderBepItem
                                {
                                    MaDichVu = maDvCt,
                                    TenMon = tenMonCt,
                                    SoLuong = slCt > 0 ? slCt : 1,
                                    GhiChu = string.Empty,
                                    CanhBaoDiUng = diUngCt,
                                });
                            }
                        }
                    }

                    if (!items.Any()) continue;

                    string trangThaiBep = "Mới";
                    if (TrangThaiOrderBepStore.TrangThaiDict.ContainsKey(maHd))
                    {
                        trangThaiBep = TrangThaiOrderBepStore.TrangThaiDict[maHd];
                    }
                    else if (!string.IsNullOrEmpty(trangThaiTrongJson))
                    {
                        if (trangThaiTrongJson == "Đang nấu" || trangThaiTrongJson == "Hoàn tất")
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
                        // Hiển thị ĐẦY ĐỦ mã hóa đơn trùng khớp 100% với Sơ đồ bàn và Quầy Thu Ngân
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

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
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
                ModelState.AddModelError(string.Empty, "Lỗi tải màn hình Quản lý Order: " + ex.Message);
                return Page();
            }
        }

        // XỬ LÝ CHẾ BIẾN MÓN ĂN: CHỈ ĐẦU BẾP MỚI ĐƯỢC THAO TÁC (Quản lý bị chặn tuyệt đối)
        public async Task<IActionResult> OnPostChuyenTrangThaiOrderAsync(string maHoaDon, string trangThaiMoi)
        {
            var (vaiTro, _) = await LayThongTinNguoiDungAsync();
            if (string.IsNullOrEmpty(vaiTro))
            {
                return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));
            }

            // RÀNG BUỘC PHÂN QUYỀN: Quản lý tuyệt đối không được thao tác bấm các nút xử lý chế biến
            if (KiemTraLaQuanLy(vaiTro) || !KiemTraLaDauBep(vaiTro))
            {
                TempData["ErrorMessage"] = "Phân quyền hệ thống: Tài khoản Quản lý chỉ giám sát và quản trị, quyền bấm nút xử lý chế biến dành riêng cho Đầu bếp!";
                return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
            }

            try
            {
                if (!string.IsNullOrEmpty(maHoaDon) && !string.IsNullOrEmpty(trangThaiMoi))
                {
                    TrangThaiOrderBepStore.Load();
                    TrangThaiOrderBepStore.TrangThaiDict[maHoaDon.Trim()] = trangThaiMoi.Trim();
                    TrangThaiOrderBepStore.Save();

                    // [MỚI] Ghi / xóa giờ hoàn tất
                    TrangThaiOrderBepStore.LoadGioHoanTat();
                    string ma = maHoaDon.Trim();
                    if (trangThaiMoi.Trim() == "Hoàn tất")
                    {
                        if (!TrangThaiOrderBepStore.GioHoanTat.ContainsKey(ma))
                            TrangThaiOrderBepStore.GioHoanTat[ma] = DateTime.Now;
                    }
                    else
                    {
                        TrangThaiOrderBepStore.GioHoanTat.Remove(ma);
                    }
                    TrangThaiOrderBepStore.SaveGioHoanTat();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi cập nhật trạng thái chế biến: " + ex.Message;
            }

            return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
        }

        // CHỨC NĂNG DÀNH RIÊNG CHO QUẢN LÝ: Xóa đơn lỗi hoặc Làm sạch dữ liệu cũ trên KDS
        public async Task<IActionResult> OnPostXoaOrderQuanLyAsync(string maHoaDon)
        {
            var (vaiTro, _) = await LayThongTinNguoiDungAsync();
            if (!KiemTraLaQuanLy(vaiTro))
            {
                TempData["ErrorMessage"] = "Truy cập bị từ chối: Chỉ Quản lý mới có quyền xóa dữ liệu đơn hàng!";
                return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
            }

            try
            {
                var hd = await _context.HOA_DON_NHA_HANG.FirstOrDefaultAsync(h => h.MaHoaDonNhaHang == maHoaDon);
                if (hd != null)
                {
                    // Giải phóng bàn liên quan về trạng thái Trống
                    string maBan = string.Empty;
                    var prop = hd.GetType().GetProperty("MaBan") ?? hd.GetType().GetProperty("BAN_NHA_HANGMaBan");
                    if (prop != null) maBan = prop.GetValue(hd)?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(maBan))
                    {
                        try { maBan = _context.Entry(hd).Property("MaBan").CurrentValue?.ToString() ?? string.Empty; } catch { }
                    }
                    if (string.IsNullOrEmpty(maBan))
                    {
                        try { maBan = _context.Entry(hd).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? string.Empty; } catch { }
                    }

                    var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == maBan.Trim());
                    if (ban != null) ban.TrangThai = "Trống";

                    _context.HOA_DON_NHA_HANG.Remove(hd);
                    await _context.SaveChangesAsync();

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
                TempData["ErrorMessage"] = "Lỗi khi xóa đơn: " + ex.Message;
            }

            return RedirectToPage(new { TrangThaiLoc, TuKhoaTimKiem });
        }
    }
}