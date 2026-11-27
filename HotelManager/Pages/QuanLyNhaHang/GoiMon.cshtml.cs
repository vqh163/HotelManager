using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
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

    public class CartItemDTO
    {
        public string MaDichVu { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string GhiChu { get; set; } = string.Empty;
        public bool CoDiUng { get; set; }
    }

    public class MonTrongHoaDonDto
    {
        public string MaDichVu { get; set; } = string.Empty;
        public string TenMon { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class GoiMonModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GoiMonModel> _logger;

        public GoiMonModel(ApplicationDbContext context, ILogger<GoiMonModel> logger)
        {
            _context = context;
            _logger = logger;
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
                var ban = await _context.BAN_NHA_HANG.AsNoTracking().FirstOrDefaultAsync(b => b.MaBan == id);
                if (ban == null) return RedirectToPage("./SoDoBan");
                BanNhaHang = ban;

                // Truy vấn danh sách món ăn trực tiếp từ bảng DICH_VU (loại trừ dịch vụ nội bộ minibar/đền bù)
                var query = _context.DICH_VU.AsNoTracking()
                    .Where(d => d.LoaiDichVu != "Minibar" && d.LoaiDichVu != "Đền bù");

                if (!string.IsNullOrWhiteSpace(TuKhoaTimKiem))
                {
                    string kw = TuKhoaTimKiem.Trim();
                    query = query.Where(d => (d.TenDichVu != null && d.TenDichVu.Contains(kw)) || d.MaDichVu.Contains(kw));
                }

                if (!string.IsNullOrEmpty(DanhMucLoc) && DanhMucLoc != "Tất cả")
                {
                    query = query.Where(d => d.LoaiDichVu == DanhMucLoc);
                }

                var dsDichVu = await query.OrderBy(d => d.TenDichVu).ToListAsync();

                string[] foodImages =
                {
                    "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&q=80",
                    "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=300&q=80",
                    "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=300&q=80"
                };

                DanhSachMonAn = dsDichVu.Select((d, index) => new DichVuViewModel
                {
                    MaDichVu = d.MaDichVu,
                    TenDichVu = d.TenDichVu ?? string.Empty,
                    LoaiDichVu = string.IsNullOrEmpty(d.LoaiDichVu) ? "Món chính" : d.LoaiDichVu,
                    DonGia = d.DonGia ?? 0m,
                    HinhAnh = !string.IsNullOrEmpty(d.HinhAnh) ? d.HinhAnh : foodImages[index % foodImages.Length],
                    MoTa = d.MoTa ?? string.Empty,
                    CanhBaoDiUng = d.CanhBaoDiUng ?? string.Empty,
                    HetMon = !d.ConMon
                }).ToList();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải giao diện gọi món cho bàn {MaBan}", id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải danh sách món ăn.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToPage("./SoDoBan");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == id);
                if (ban == null) return RedirectToPage("./SoDoBan");

                if (string.IsNullOrWhiteSpace(CartDataJson))
                {
                    ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống!");
                    return await OnGetAsync(id);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var cartItems = JsonSerializer.Deserialize<List<CartItemDTO>>(CartDataJson, options);

                if (cartItems == null || !cartItems.Any())
                {
                    ModelState.AddModelError(string.Empty, "Giỏ hàng không có món hợp lệ!");
                    return await OnGetAsync(id);
                }

                // 1. Bảo mật chống sửa giá/số lượng từ Client: Truy vấn đơn giá và trạng thái còn món trực tiếp từ DB
                var maDichVus = cartItems.Select(c => c.MaDichVu).Distinct().ToList();
                var dictMonDb = await _context.DICH_VU.AsNoTracking()
                    .Where(d => maDichVus.Contains(d.MaDichVu))
                    .ToDictionaryAsync(d => d.MaDichVu);

                var dsMonMoiHopLe = new List<MonTrongHoaDonDto>();
                foreach (var item in cartItems)
                {
                    if (item.SoLuong <= 0 || item.SoLuong > 99)
                    {
                        ModelState.AddModelError(string.Empty, "Số lượng mỗi món phải nằm trong khoảng từ 1 đến 99.");
                        return await OnGetAsync(id);
                    }

                    if (!dictMonDb.TryGetValue(item.MaDichVu, out var monDb))
                    {
                        ModelState.AddModelError(string.Empty, $"Món mã '{item.MaDichVu}' không tồn tại trong thực đơn!");
                        return await OnGetAsync(id);
                    }

                    if (!monDb.ConMon)
                    {
                        ModelState.AddModelError(string.Empty, $"Món '{monDb.TenDichVu}' hiện đã hết món, không thể đặt!");
                        return await OnGetAsync(id);
                    }

                    decimal donGiaChuan = monDb.DonGia ?? 0m;
                    dsMonMoiHopLe.Add(new MonTrongHoaDonDto
                    {
                        MaDichVu = monDb.MaDichVu,
                        TenMon = monDb.TenDichVu ?? "Món ăn",
                        GhiChu = (item.GhiChu ?? string.Empty).Trim(),
                        DonGia = donGiaChuan,
                        SoLuong = item.SoLuong,
                        ThanhTien = donGiaChuan * item.SoLuong
                    });
                }

                // 2. Kiểm tra bàn đã có hóa đơn đang mở hay chưa; nếu có thì gộp món vào hóa đơn hiện tại
                var hoaDon = await _context.HOA_DON_NHA_HANG
                    .OrderByDescending(h => h.ThoiGianTao)
                    .FirstOrDefaultAsync(h => h.BAN_NHA_HANGMaBan == ban.MaBan
                        && (h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDangPhucVu
                            || h.TrangThaiThanhToan == HangSoHeThong.HoaDonNHChoThanhToan));

                var dsMonTongHop = new List<MonTrongHoaDonDto>();
                bool laHoaDonMoi = (hoaDon == null);

                if (laHoaDonMoi)
                {
                    hoaDon = new HOA_DON_NHA_HANG
                    {
                        MaHoaDonNhaHang = BaoMatHelper.SinhMaNgauNhien("HD"),
                        BAN_NHA_HANGMaBan = ban.MaBan, // SỬA LỖI ĐỨT LUỒNG: Bắt buộc gán khóa ngoại mã bàn
                        NHAN_VIENMaNV = User.FindFirst("MaNV")?.Value,
                        ThoiGianTao = DateTime.Now,
                        GiamGia = 0,
                        TrangThaiThanhToan = HangSoHeThong.HoaDonNHDangPhucVu,
                        HinhThucThanhToan = "Đang phục vụ"
                    };
                }
                else if (!string.IsNullOrWhiteSpace(hoaDon!.DanhSachMonJson))
                {
                    try
                    {
                        dsMonTongHop = JsonSerializer.Deserialize<List<MonTrongHoaDonDto>>(hoaDon.DanhSachMonJson, options)
                            ?? new List<MonTrongHoaDonDto>();
                    }
                    catch { }
                }

                dsMonTongHop.AddRange(dsMonMoiHopLe);

                // 3. Thống nhất công thức tính tiền giữa Gọi món & Quầy thu ngân (5% Phí phục vụ + 10% VAT)
                decimal tongTienMonAn = dsMonTongHop.Sum(x => x.ThanhTien);
                decimal phiPhucVu = Math.Round(tongTienMonAn * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                decimal thueVAT = Math.Round(tongTienMonAn * HangSoHeThong.TyLeThueVAT, 0);
                decimal giamGia = hoaDon!.GiamGia ?? 0m;

                hoaDon.TongTienMonAn = tongTienMonAn;
                hoaDon.PhiPhucVu = phiPhucVu;
                hoaDon.TienThueVAT = thueVAT;
                hoaDon.ThanhTien = Math.Max(0, tongTienMonAn + phiPhucVu + thueVAT - giamGia);
                hoaDon.DanhSachMonJson = JsonSerializer.Serialize(dsMonTongHop);

                if (laHoaDonMoi)
                    _context.HOA_DON_NHA_HANG.Add(hoaDon);
                else
                    _context.HOA_DON_NHA_HANG.Update(hoaDon);

                // 4. Lưu từng dòng chi tiết món mới gọi vào CHI_TIET_DICH_VU (liên kết khóa ngoại rõ ràng)
                DateTime now = DateTime.Now;
                foreach (var mon in dsMonMoiHopLe)
                {
                    _context.CHI_TIET_DICH_VU.Add(new CHI_TIET_DICH_VU
                    {
                        MaChiTietDichVu = BaoMatHelper.SinhMaNgauNhien("CD"),
                        DICH_VUMaDichVu = mon.MaDichVu,
                        HOA_DON_NHA_HANGMaHoaDonNhaHang = hoaDon.MaHoaDonNhaHang,
                        SoLuong = mon.SoLuong,
                        DonGia = mon.DonGia,
                        ThanhTien = mon.ThanhTien,
                        ThoiGianSuDung = now
                    });
                }

                // 5. Cập nhật trạng thái bàn sang "Đang có khách"
                ban.TrangThai = "Đang có khách";
                _context.BAN_NHA_HANG.Update(ban);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã ghi nhận đơn gọi món vào hóa đơn {hoaDon.MaHoaDonNhaHang} cho {ban.TenBan}!";
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi lưu đơn gọi món cho bàn {MaBan}", id);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi lưu đơn gọi món.");
                return await OnGetAsync(id);
            }
        }
    }
}
