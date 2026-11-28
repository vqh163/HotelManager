using Microsoft.AspNetCore.Authorization;
using HotelManager.Data;
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

namespace HotelManagement.Pages.NhaHang.NhaBep
{
    // NÂNG CẤP: LƯU TRỮ VĨNH VIỄN BẰNG FILE JSON TRÊN SERVER
    public static class TrangThaiMonStore
    {
        public static Dictionary<string, bool> DuLieu = new Dictionary<string, bool>();
        public static Dictionary<string, string> MoTaDict = new Dictionary<string, string>();
        public static Dictionary<string, string> DiUngDict = new Dictionary<string, string>();

        private static readonly string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "menu_data.json");

        public class MenuDataState
        {
            public Dictionary<string, bool> DuLieu { get; set; } = new Dictionary<string, bool>();
            public Dictionary<string, string> MoTaDict { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, string> DiUngDict { get; set; } = new Dictionary<string, string>();
        }

        // Đọc dữ liệu từ ổ cứng lên RAM
        public static void LoadData()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    var json = File.ReadAllText(FilePath);
                    var data = JsonSerializer.Deserialize<MenuDataState>(json);
                    if (data != null)
                    {
                        DuLieu = data.DuLieu ?? new Dictionary<string, bool>();
                        MoTaDict = data.MoTaDict ?? new Dictionary<string, string>();
                        DiUngDict = data.DiUngDict ?? new Dictionary<string, string>();
                    }
                }
                catch { /* Bỏ qua nếu lỗi đọc file */ }
            }
        }

        // Ghi dữ liệu từ RAM xuống ổ cứng
        public static void SaveData()
        {
            var data = new MenuDataState { DuLieu = DuLieu, MoTaDict = MoTaDict, DiUngDict = DiUngDict };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }

    public class ThucDonViewModel
    {
        public string MaDichVu { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public string LoaiDichVu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public bool ConMon { get; set; }
    }
    [Authorize(Roles = "Đầu bếp, Quản lý")]
    public class QuanLyThucDonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IActionDescriptorCollectionProvider _actionProvider;

        public QuanLyThucDonModel(ApplicationDbContext context, IActionDescriptorCollectionProvider actionProvider)
        {
            _context = context;
            _actionProvider = actionProvider;
        }

        [BindProperty(SupportsGet = true)] public string? TuKhoaTimKiem { get; set; }
        [BindProperty(SupportsGet = true)] public string? DanhMucLoc { get; set; }
        [BindProperty(SupportsGet = true)] public string? TrangThaiLoc { get; set; }

        // Thuộc tính xác định người dùng hiện tại có phải cấp Quản lý hay không
        public bool LaQuanLy { get; set; } = false;

        public List<ThucDonViewModel> DanhSachThucDon { get; set; } = new List<ThucDonViewModel>();

        public int TongSoMon { get; set; }
        public int DangBan { get; set; }
        public int TamNgung { get; set; }

        // Hàm tự động dò tìm đúng đường dẫn Razor Page thực tế trong project để điều hướng chính xác
        private string TimDuongDanTrang(string tenTrang, string duongDanMacDinh)
        {
            var page = _actionProvider.ActionDescriptors.Items
                .OfType<PageActionDescriptor>()
                .FirstOrDefault(p => p.ViewEnginePath.EndsWith("/" + tenTrang, StringComparison.OrdinalIgnoreCase)
                                  || p.ViewEnginePath.Equals("/" + tenTrang, StringComparison.OrdinalIgnoreCase));
            return page != null ? page.ViewEnginePath : duongDanMacDinh;
        }

        // Truy vấn trực tiếp VaiTro thực tế từ bảng TAI_KHOAN trong SQL Server bằng LINQ
        private async Task<string> LayVaiTroThucTeAsync()
        {
            string tenDangNhap = User?.Identity?.Name
                              ?? User?.FindFirst(ClaimTypes.Name)?.Value
                              ?? User?.FindFirst("TenDangNhap")?.Value
                              ?? User?.FindFirst("Username")?.Value
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

            // 1. Ưu tiên đối chiếu trực tiếp trong bảng TAI_KHOAN của SQL Server
            if (!string.IsNullOrEmpty(tenDangNhap) || !string.IsNullOrEmpty(maTaiKhoan))
            {
                var tkDb = await _context.TAI_KHOAN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap || t.MaTaiKhoan == maTaiKhoan);

                if (tkDb != null && !string.IsNullOrEmpty(tkDb.VaiTro))
                {
                    return tkDb.VaiTro.Trim();
                }
            }

            // 2. Dự phòng đọc từ Claim hoặc Session của trang Đăng nhập
            string vaiTro = User?.FindFirst(ClaimTypes.Role)?.Value
                         ?? User?.FindFirst("VaiTro")?.Value
                         ?? User?.FindFirst("Role")?.Value
                         ?? string.Empty;

            if (string.IsNullOrEmpty(vaiTro))
            {
                try
                {
                    vaiTro = HttpContext.Session.GetString("VaiTro")
                          ?? HttpContext.Session.GetString("Role")
                          ?? string.Empty;
                }
                catch { }
            }

            return vaiTro.Trim();
        }

        private bool LaVaiTroQuanLy(string vaiTro)
        {
            if (string.IsNullOrEmpty(vaiTro)) return false;

            return vaiTro.Contains("Quản lý", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("QuanLy", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Giám đốc", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Quản trị", StringComparison.OrdinalIgnoreCase);
        }

        private bool CoQuyenBepHoacQuanLy(string vaiTro)
        {
            if (string.IsNullOrEmpty(vaiTro)) return false;

            return LaVaiTroQuanLy(vaiTro) ||
                   vaiTro.Contains("Đầu bếp", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("DauBep", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Bếp", StringComparison.OrdinalIgnoreCase) ||
                   vaiTro.Contains("Bep", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string vaiTro = await LayVaiTroThucTeAsync();

            // 1. Nếu chưa đăng nhập -> Tự động chuyển về trang Đăng nhập
            if (string.IsNullOrEmpty(vaiTro))
            {
                return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));
            }

            // - Nếu không phải Đầu bếp hoặc Quản lý -> Chuyển về trang Đăng nhập
            if (!CoQuyenBepHoacQuanLy(vaiTro))
            {
                return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));
            }

            // Xác định quyền Quản lý (Quản lý = true để hiện nút Thêm/Sửa/Xóa món; Đầu bếp = false chỉ được bật/tắt Còn/Hết món)
            LaQuanLy = LaVaiTroQuanLy(vaiTro);

            try
            {
                // Kích hoạt nạp dữ liệu từ File JSON khi load trang
                TrangThaiMonStore.LoadData();

                var query = _context.DICH_VU.AsQueryable();

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                    query = query.Where(d => d.TenDichVu.Contains(TuKhoaTimKiem) || d.MaDichVu.Contains(TuKhoaTimKiem));

                if (!string.IsNullOrEmpty(DanhMucLoc) && DanhMucLoc != "Tất cả")
                    query = query.Where(d => d.LoaiDichVu == DanhMucLoc);

                query = query.OrderBy(d => d.TenDichVu);

                var dsDichVu = await query.ToListAsync();

                string[] foodImages = {
                    "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&q=80",
                    "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=300&q=80"
                };

                DanhSachThucDon = dsDichVu.Select((d, index) =>
                {
                    bool isConMon = true;
                    if (TrangThaiMonStore.DuLieu.ContainsKey(d.MaDichVu))
                        isConMon = TrangThaiMonStore.DuLieu[d.MaDichVu];

                    string moTa = TrangThaiMonStore.MoTaDict.ContainsKey(d.MaDichVu) ? TrangThaiMonStore.MoTaDict[d.MaDichVu] : "";

                    return new ThucDonViewModel
                    {
                        MaDichVu = d.MaDichVu,
                        TenDichVu = d.TenDichVu,
                        LoaiDichVu = string.IsNullOrEmpty(d.LoaiDichVu) ? "Món chính" : d.LoaiDichVu,
                        DonGia = d.DonGia ?? 0m,
                        HinhAnh = !string.IsNullOrEmpty(d.HinhAnh) ? d.HinhAnh : foodImages[index % foodImages.Length],
                        MoTa = moTa,
                        ConMon = isConMon
                    };
                }).ToList();

                if (!string.IsNullOrEmpty(TrangThaiLoc))
                {
                    bool isFilterConMon = (TrangThaiLoc == "ConMon");
                    DanhSachThucDon = DanhSachThucDon.Where(m => m.ConMon == isFilterConMon).ToList();
                }

                var allDichVu = await _context.DICH_VU.ToListAsync();
                TongSoMon = allDichVu.Count;
                DangBan = allDichVu.Count(d => !TrangThaiMonStore.DuLieu.ContainsKey(d.MaDichVu) || TrangThaiMonStore.DuLieu[d.MaDichVu]);
                TamNgung = TongSoMon - DangBan;

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải dữ liệu: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            string vaiTro = await LayVaiTroThucTeAsync();
            if (string.IsNullOrEmpty(vaiTro))
            {
                return Redirect(TimDuongDanTrang("DangNhap", "/TaiKhoan/DangNhap"));
            }

            // Chặn chặt chẽ ở tầng Backend: Chỉ Quản lý mới có quyền xóa món ăn khỏi thực đơn
            if (!LaVaiTroQuanLy(vaiTro))
            {
                ModelState.AddModelError(string.Empty, "Truy cập bị từ chối: Chỉ tài khoản cấp Quản lý mới được phép xóa món ăn!");
                return await OnGetAsync();
            }

            try
            {
                var mon = await _context.DICH_VU.FirstOrDefaultAsync(m => m.MaDichVu == id);
                if (mon != null)
                {
                    _context.DICH_VU.Remove(mon);
                    await _context.SaveChangesAsync();

                    if (TrangThaiMonStore.DuLieu.ContainsKey(id)) TrangThaiMonStore.DuLieu.Remove(id);
                    if (TrangThaiMonStore.MoTaDict.ContainsKey(id)) TrangThaiMonStore.MoTaDict.Remove(id);
                    if (TrangThaiMonStore.DiUngDict.ContainsKey(id)) TrangThaiMonStore.DiUngDict.Remove(id);

                    // LƯU CẬP NHẬT XUỐNG Ổ CỨNG SAU KHI XÓA
                    TrangThaiMonStore.SaveData();
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi xóa món: Có thể món này đang tồn tại trong hóa đơn cũ. " + ex.Message);
                return await OnGetAsync();
            }
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(string id)
        {
            // Kiểm tra quyền: Chỉ Đầu bếp hoặc Quản lý mới được phép bật/tắt trạng thái Còn món / Hết món
            string vaiTro = await LayVaiTroThucTeAsync();
            if (!CoQuyenBepHoacQuanLy(vaiTro))
            {
                return new JsonResult(new { success = false });
            }

            try
            {
                TrangThaiMonStore.LoadData();

                bool currentStatus = true;
                if (TrangThaiMonStore.DuLieu.ContainsKey(id))
                    currentStatus = TrangThaiMonStore.DuLieu[id];

                TrangThaiMonStore.DuLieu[id] = !currentStatus;

                // LƯU CẬP NHẬT XUỐNG Ổ CỨNG SAU KHI ĐỔI TRẠNG THÁI CÒN/HẾT MÓN
                TrangThaiMonStore.SaveData();

                var tongSo = await _context.DICH_VU.CountAsync();
                var allDichVu = await _context.DICH_VU.ToListAsync();
                var dangBan = allDichVu.Count(d => !TrangThaiMonStore.DuLieu.ContainsKey(d.MaDichVu) || TrangThaiMonStore.DuLieu[d.MaDichVu]);
                var tamNgung = tongSo - dangBan;

                return new JsonResult(new { success = true, tongSoMon = tongSo, dangBan = dangBan, tamNgung = tamNgung });
            }
            catch
            {
                return new JsonResult(new { success = false });
            }
        }
    }
}