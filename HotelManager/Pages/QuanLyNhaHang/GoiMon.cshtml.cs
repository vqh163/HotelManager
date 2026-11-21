using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    // DTO hiển thị dữ liệu món ăn
    public class DichVuViewModel
    {
        public string MaDichVu { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public string LoaiDichVu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public string CanhBaoDiUng { get; set; } = string.Empty;
        public bool HetMon { get; set; }
    }

    // DTO nhận dữ liệu từ Giỏ hàng (Client-side)
    public class CartItemDTO
    {
        public string MaDichVu { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string GhiChu { get; set; } = string.Empty;
        public bool CoDiUng { get; set; }
    }

    public class GoiMonModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GoiMonModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public BAN_NHA_HANG BanNhaHang { get; set; } = new BAN_NHA_HANG();
        public List<DichVuViewModel> DanhSachMonAn { get; set; } = new List<DichVuViewModel>();

        [BindProperty]
        public string CartDataJson { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? DanhMucLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TuKhoaTimKiem { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                // NẠP DỮ LIỆU TỪ Ổ CỨNG LÊN RAM TRƯỚC KHI HIỂN THỊ ĐỂ KHÔNG BỊ MẤT MÔ TẢ/DỊ ỨNG
                HotelManagement.Pages.NhaHang.NhaBep.TrangThaiMonStore.LoadData();

                BanNhaHang = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == id);
                if (BanNhaHang == null) return RedirectToPage("./SoDoBan");

                // Truy vấn danh sách món ăn bằng LINQ
                var query = _context.DICH_VU.AsQueryable();

                if (!string.IsNullOrEmpty(TuKhoaTimKiem))
                {
                    query = query.Where(d => d.TenDichVu.Contains(TuKhoaTimKiem) || d.MaDichVu.Contains(TuKhoaTimKiem));
                }

                if (!string.IsNullOrEmpty(DanhMucLoc) && DanhMucLoc != "Tất cả")
                {
                    query = query.Where(d => d.LoaiDichVu == DanhMucLoc);
                }

                var dsDichVu = await query.ToListAsync();

                string[] foodImages = {
                    "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&q=80",
                    "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=300&q=80",
                    "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=300&q=80"
                };

                DanhSachMonAn = dsDichVu.Select((d, index) =>
                {
                    bool isConMon = true;
                    if (HotelManagement.Pages.NhaHang.NhaBep.TrangThaiMonStore.DuLieu.ContainsKey(d.MaDichVu))
                        isConMon = HotelManagement.Pages.NhaHang.NhaBep.TrangThaiMonStore.DuLieu[d.MaDichVu];

                    string moTa = HotelManagement.Pages.NhaHang.NhaBep.TrangThaiMonStore.MoTaDict.ContainsKey(d.MaDichVu)
                        ? HotelManagement.Pages.NhaHang.NhaBep.TrangThaiMonStore.MoTaDict[d.MaDichVu]
                        : "";

                    string diUng = HotelManagement.Pages.NhaHang.NhaBep.TrangThaiMonStore.DiUngDict.ContainsKey(d.MaDichVu)
                        ? HotelManagement.Pages.NhaHang.NhaBep.TrangThaiMonStore.DiUngDict[d.MaDichVu]
                        : "";

                    return new DichVuViewModel
                    {
                        MaDichVu = d.MaDichVu,
                        TenDichVu = d.TenDichVu,
                        LoaiDichVu = string.IsNullOrEmpty(d.LoaiDichVu) ? "Món chính" : d.LoaiDichVu,
                        DonGia = d.DonGia ?? 0m,
                        HinhAnh = !string.IsNullOrEmpty(d.HinhAnh) ? d.HinhAnh : foodImages[index % foodImages.Length],
                        MoTa = moTa,
                        CanhBaoDiUng = diUng,
                        HetMon = !isConMon
                    };
                }).ToList();

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi tải dữ liệu: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (User?.Identity?.IsAuthenticated != true) return RedirectToPage("/TaiKhoan/DangNhap");

            try
            {
                if (string.IsNullOrEmpty(CartDataJson))
                {
                    ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống!");
                    return await OnGetAsync(id);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var cartItems = JsonSerializer.Deserialize<List<CartItemDTO>>(CartDataJson, options);
                if (cartItems == null || !cartItems.Any()) return await OnGetAsync(id);

                decimal tongTienMonMoi = cartItems.Sum(x => x.DonGia * x.SoLuong);
                decimal vatMoi = tongTienMonMoi * 0.1m;
                string maBanClean = id?.Trim() ?? string.Empty;

                var maDichVus = cartItems.Select(c => c.MaDichVu).ToList();
                var dictTenMon = _context.DICH_VU.Where(d => maDichVus.Contains(d.MaDichVu))
                                                 .ToDictionary(d => d.MaDichVu, d => d.TenDichVu);

                // Kiểm tra xem bàn đang có khách này đã có hóa đơn chưa thanh toán hay chưa
                var dsHoaDonActive = await _context.HOA_DON_NHA_HANG
                    .Where(h => h.HinhThucThanhToan != "Đã Thanh Toán")
                    .OrderByDescending(h => h.ThoiGianTao)
                    .ToListAsync();

                var hoaDonHienTai = dsHoaDonActive.FirstOrDefault(h =>
                {
                    string maBanHd = "";
                    var prop = h.GetType().GetProperty("MaBan") ?? h.GetType().GetProperty("BAN_NHA_HANGMaBan");
                    if (prop != null) maBanHd = prop.GetValue(h)?.ToString() ?? "";
                    if (string.IsNullOrEmpty(maBanHd))
                    {
                        try { maBanHd = _context.Entry(h).Property("MaBan").CurrentValue?.ToString() ?? ""; } catch { }
                    }
                    if (string.IsNullOrEmpty(maBanHd))
                    {
                        try { maBanHd = _context.Entry(h).Property("BAN_NHA_HANGMaBan").CurrentValue?.ToString() ?? ""; } catch { }
                    }

                    var dsMaBan = maBanHd.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(m => m.Trim());
                    return dsMaBan.Contains(maBanClean);
                });

                HOA_DON_NHA_HANG hoaDonTarget;

                if (hoaDonHienTai != null)
                {
                    // TRƯỜNG HỢP 1: BÀN ĐANG CÓ KHÁCH -> GỌI THÊM MÓN VÀO HÓA ĐƠN HIỆN TẠI
                    hoaDonTarget = hoaDonHienTai;
                    hoaDonTarget.TongTienMonAn = (hoaDonTarget.TongTienMonAn ?? 0) + tongTienMonMoi;
                    hoaDonTarget.ThanhTien = (hoaDonTarget.ThanhTien ?? 0) + tongTienMonMoi + vatMoi;

                    // Đọc danh sách món cũ đã có trong DanhSachMonJson
                    var danhSachMonGop = new List<object>();
                    try
                    {
                        string jsonCu = "[]";
                        var propJson = hoaDonTarget.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) jsonCu = propJson.GetValue(hoaDonTarget)?.ToString() ?? "[]";
                        else jsonCu = _context.Entry(hoaDonTarget).Property("DanhSachMonJson").CurrentValue?.ToString() ?? "[]";

                        if (!string.IsNullOrWhiteSpace(jsonCu) && jsonCu != "[]")
                        {
                            using var doc = JsonDocument.Parse(jsonCu);
                            foreach (var el in doc.RootElement.EnumerateArray())
                            {
                                danhSachMonGop.Add(el.Clone());
                            }
                        }
                    }
                    catch { }

                    // Nối thêm các món mới và đánh dấu LaMonGoiThem = true để màn hình Bếp nhận biết
                    foreach (var item in cartItems)
                    {
                        danhSachMonGop.Add(new
                        {
                            MaDichVu = item.MaDichVu,
                            TenMon = dictTenMon.ContainsKey(item.MaDichVu) ? dictTenMon[item.MaDichVu] : "Món ăn",
                            GhiChu = item.GhiChu,
                            DonGia = item.DonGia,
                            SoLuong = item.SoLuong,
                            ThanhTien = item.DonGia * item.SoLuong,
                            TrangThaiBep = "Mới",
                            LaMonGoiThem = true // Đánh dấu món gọi thêm
                        });
                    }

                    string jsonSauKhiThem = JsonSerializer.Serialize(danhSachMonGop);
                    try
                    {
                        var propJson = hoaDonTarget.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) propJson.SetValue(hoaDonTarget, jsonSauKhiThem);
                        else _context.Entry(hoaDonTarget).Property("DanhSachMonJson").CurrentValue = jsonSauKhiThem;
                    }
                    catch { }

                    // Nếu đơn trước đó bếp đã bấm "Hoàn tất" thì chuyển lại về "Mới" để bếp thấy có món gọi thêm
                    try
                    {
                        HotelManagement.Pages.NhaHang.NhaBep.TrangThaiOrderBepStore.Load();
                        if (HotelManagement.Pages.NhaHang.NhaBep.TrangThaiOrderBepStore.TrangThaiDict.ContainsKey(hoaDonTarget.MaHoaDonNhaHang))
                        {
                            HotelManagement.Pages.NhaHang.NhaBep.TrangThaiOrderBepStore.TrangThaiDict[hoaDonTarget.MaHoaDonNhaHang] = "Mới";
                            HotelManagement.Pages.NhaHang.NhaBep.TrangThaiOrderBepStore.Save();
                        }
                    }
                    catch { }
                }
                else
                {
                    // TRƯỜNG HỢP 2: BÀN TRỐNG -> TẠO HÓA ĐƠN MỚI (YYMMDD + STT)
                    string datePrefix = DateTime.Now.ToString("yyMMdd");
                    var dsMaHomNay = await _context.HOA_DON_NHA_HANG
                        .Where(h => h.MaHoaDonNhaHang.StartsWith(datePrefix) || h.MaHoaDonNhaHang.StartsWith("HD" + datePrefix))
                        .Select(h => h.MaHoaDonNhaHang)
                        .ToListAsync();

                    int sttNext = dsMaHomNay.Count + 1;
                    string uniqueMaHoaDon = "";
                    do
                    {
                        uniqueMaHoaDon = $"{datePrefix}{sttNext:D2}";
                        sttNext++;
                    }
                    while (dsMaHomNay.Contains(uniqueMaHoaDon) || await _context.HOA_DON_NHA_HANG.AnyAsync(h => h.MaHoaDonNhaHang == uniqueMaHoaDon));

                    hoaDonTarget = new HOA_DON_NHA_HANG
                    {
                        MaHoaDonNhaHang = uniqueMaHoaDon,
                        ThoiGianTao = DateTime.Now,
                        TongTienMonAn = tongTienMonMoi,
                        GiamGia = 0,
                        ThanhTien = tongTienMonMoi + vatMoi,
                        HinhThucThanhToan = "Chưa thanh toán"
                    };

                    var propMaBan = hoaDonTarget.GetType().GetProperty("MaBan") ?? hoaDonTarget.GetType().GetProperty("BAN_NHA_HANGMaBan");
                    if (propMaBan != null) propMaBan.SetValue(hoaDonTarget, maBanClean);
                    try { _context.Entry(hoaDonTarget).Property("MaBan").CurrentValue = maBanClean; } catch { }
                    try { _context.Entry(hoaDonTarget).Property("BAN_NHA_HANGMaBan").CurrentValue = maBanClean; } catch { }

                    var dsThuNgan = cartItems.Select(item => new
                    {
                        MaDichVu = item.MaDichVu,
                        TenMon = dictTenMon.ContainsKey(item.MaDichVu) ? dictTenMon[item.MaDichVu] : "Món ăn",
                        GhiChu = item.GhiChu,
                        DonGia = item.DonGia,
                        SoLuong = item.SoLuong,
                        ThanhTien = item.DonGia * item.SoLuong,
                        TrangThaiBep = "Mới",
                        LaMonGoiThem = false
                    }).ToList();

                    string jsonChuan = JsonSerializer.Serialize(dsThuNgan);
                    try
                    {
                        var propJson = hoaDonTarget.GetType().GetProperty("DanhSachMonJson");
                        if (propJson != null) propJson.SetValue(hoaDonTarget, jsonChuan);
                        else _context.Entry(hoaDonTarget).Property("DanhSachMonJson").CurrentValue = jsonChuan;
                    }
                    catch { }

                    _context.HOA_DON_NHA_HANG.Add(hoaDonTarget);
                }

                // Lưu chi tiết các món vừa chọn vào bảng CHI_TIET_DICH_VU gắn với hoaDonTarget
                foreach (var item in cartItems)
                {
                    var chiTiet = new CHI_TIET_DICH_VU
                    {
                        MaChiTietDichVu = Guid.NewGuid().ToString().Substring(0, 8),
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia,
                        ThanhTien = item.DonGia * item.SoLuong,
                        ThoiGianSuDung = DateTime.Now
                    };

                    try { _context.Entry(chiTiet).Property("MaHoaDonNhaHang").CurrentValue = hoaDonTarget.MaHoaDonNhaHang; } catch { }
                    try { _context.Entry(chiTiet).Property("MaDichVu").CurrentValue = item.MaDichVu; } catch { }
                    try { _context.Entry(chiTiet).Property("HOA_DON_NHA_HANGMaHoaDonNhaHang").CurrentValue = hoaDonTarget.MaHoaDonNhaHang; } catch { }
                    try { _context.Entry(chiTiet).Property("DICH_VUMaDichVu").CurrentValue = item.MaDichVu; } catch { }

                    _context.CHI_TIET_DICH_VU.Add(chiTiet);
                }

                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == id);
                if (ban != null)
                {
                    ban.TrangThai = "Đang có khách";
                    if (ban.GetType().GetProperty("MaHoaDonHienTai") != null)
                    {
                        ban.GetType().GetProperty("MaHoaDonHienTai")?.SetValue(ban, hoaDonTarget.MaHoaDonNhaHang);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi lưu Order: " + ex.Message);
                return await OnGetAsync(id);
            }
        }
    }
}