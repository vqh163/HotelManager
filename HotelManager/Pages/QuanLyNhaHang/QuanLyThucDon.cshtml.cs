using HotelManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public class QuanLyThucDonModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public QuanLyThucDonModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string? TuKhoaTimKiem { get; set; }
        [BindProperty(SupportsGet = true)] public string? DanhMucLoc { get; set; }
        [BindProperty(SupportsGet = true)] public string? TrangThaiLoc { get; set; }

        public List<ThucDonViewModel> DanhSachThucDon { get; set; } = new List<ThucDonViewModel>();

        public int TongSoMon { get; set; }
        public int DangBan { get; set; }
        public int TamNgung { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
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
            try
            {
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