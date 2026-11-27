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
        public decimal TongCong { get; set; }
    }

    public class PhongDangOOptionDto
    {
        public string MaDatPhong { get; set; } = string.Empty;
        public string HienThi { get; set; } = string.Empty;
    }

    public class QuayThuNganModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuayThuNganModel> _logger;

        public QuayThuNganModel(ApplicationDbContext context, ILogger<QuayThuNganModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Bộ lọc trạng thái hóa đơn tại Quầy thu ngân
        [BindProperty(SupportsGet = true)]
        public string? TrangThaiLoc { get; set; }

        public bool LaQuanLy { get; set; } = false;

        public List<HoaDonViewModel> DanhSachHoaDon { get; set; } = new List<HoaDonViewModel>();
        public List<PhongDangOOptionDto> DanhSachPhongDangO { get; set; } = new List<PhongDangOOptionDto>();

        [BindProperty]
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt";

        [BindProperty]
        public string? MaDatPhongGhiNo { get; set; }

        private static bool LaHoaDonDaChot(HOA_DON_NHA_HANG hd)
        {
            return string.Equals(hd.TrangThaiThanhToan, HangSoHeThong.HoaDonNHDaThanhToan, StringComparison.OrdinalIgnoreCase)
                || string.Equals(hd.TrangThaiThanhToan, HangSoHeThong.HoaDonNHGhiNoPhong, StringComparison.OrdinalIgnoreCase)
                || string.Equals(hd.HinhThucThanhToan, "Đã Thanh Toán", StringComparison.OrdinalIgnoreCase)
                || string.Equals(hd.HinhThucThanhToan, "Ghi nợ phòng", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LaQuanLy = User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);

            try
            {
                // 1. Lấy danh sách các phòng đang lưu trú (DANG_O) để phục vụ tùy chọn "Ghi nợ về phòng"
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

                // 2. Truy vấn danh sách hóa đơn nhà hàng và bàn tương ứng bằng LINQ (bỏ Reflection)
                var dsHoaDonDb = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                    .OrderByDescending(h => h.ThoiGianTao)
                    .Take(50)
                    .ToListAsync();
                var dsBanDb = await _context.BAN_NHA_HANG.AsNoTracking().ToListAsync();

                // Lọc theo trạng thái hóa đơn nếu Thu ngân chọn bộ lọc
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

                        if (loc.Equals("Đã Thanh Toán", StringComparison.OrdinalIgnoreCase))
                            return tt.Equals(HangSoHeThong.HoaDonNHDaThanhToan, StringComparison.OrdinalIgnoreCase)
                                || tt.Equals(HangSoHeThong.HoaDonNHGhiNoPhong, StringComparison.OrdinalIgnoreCase);

                        return tt.Equals(loc, StringComparison.OrdinalIgnoreCase);
                    }).ToList();
                }

                int stt = 1;
                foreach (var hd in dsHoaDonDb)
                {
                    string maBanCuaHd = (hd.BAN_NHA_HANGMaBan ?? string.Empty).Trim();

                    // Hỗ trợ hiển thị cả bàn đơn lẫn hóa đơn đã gộp từ nhiều bàn (VD: "B01,B02")
                    var danhSachMaBan = maBanCuaHd.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();
                    var cacBanTuongUng = dsBanDb.Where(b => danhSachMaBan.Contains(b.MaBan)).ToList();

                    string tenBanHienThi;
                    if (cacBanTuongUng.Any())
                    {
                        var danhSachSoBan = cacBanTuongUng.Select(b =>
                        {
                            string sb = (b.TenBan ?? b.MaBan).Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim();
                            if (sb.Length == 1 && char.IsDigit(sb[0])) sb = "0" + sb;
                            return sb;
                        });
                        tenBanHienThi = string.Join(", ", danhSachSoBan);
                    }
                    else
                    {
                        tenBanHienThi = string.IsNullOrEmpty(maBanCuaHd) ? "Chưa gắn bàn" : maBanCuaHd;
                    }

                    string sttFormat = stt < 10 ? "0" + stt : stt.ToString();
                    stt++;

                    var dsMonAnThuNgan = DocDanhSachMonTuJson(hd.DanhSachMonJson);
                    string finalJson = Uri.EscapeDataString(JsonSerializer.Serialize(dsMonAnThuNgan));

                    // Tính toán đồng bộ 5% Phí phục vụ + 10% VAT
                    decimal tamTinh = dsMonAnThuNgan.Sum(x => x.ThanhTien);
                    if (tamTinh == 0) tamTinh = hd.TongTienMonAn ?? 0m;

                    decimal phiPhucVu = Math.Round(tamTinh * HangSoHeThong.TyLePhiPhucVuNhaHang, 0);
                    decimal thueVAT = Math.Round(tamTinh * HangSoHeThong.TyLeThueVAT, 0);
                    decimal tongCong = Math.Max(0, tamTinh + phiPhucVu + thueVAT - (hd.GiamGia ?? 0m));

                    string trangThaiHienThi = !string.IsNullOrWhiteSpace(hd.TrangThaiThanhToan)
                        ? hd.TrangThaiThanhToan
                        : (hd.HinhThucThanhToan ?? "Chưa thanh toán");

                    DanhSachHoaDon.Add(new HoaDonViewModel
                    {
                        STT = sttFormat,
                        MaHoaDonNhaHang = hd.MaHoaDonNhaHang,
                        MaBan = maBanCuaHd,
                        TenBan = $"Bàn: {tenBanHienThi}",
                        HinhThucThanhToan = trangThaiHienThi,
                        DanhSachMonJson = finalJson,
                        TamTinh = tamTinh,
                        PhiPhucVu = phiPhucVu,
                        ThueVAT = thueVAT,
                        TongCong = tongCong
                    });
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

        // Xử lý gộp nhiều hóa đơn chưa chốt thành 1 Bill thống nhất tại Quầy thu ngân (LINQ + Transaction)
        public async Task<IActionResult> OnPostGopBillAsync(List<string> maHoaDonGopList)
        {
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

                // Kiểm tra độ dài cột BAN_NHA_HANGMaBan (varchar(20)) tránh lỗi tràn chuỗi SQL
                string chuoiMaBanGop = string.Join(",", danhSachMaBanGop);
                if (chuoiMaBanGop.Length > 20)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Không thể gộp quá nhiều bàn vào cùng 1 hóa đơn (vượt giới hạn lưu trữ mã bàn).";
                    return RedirectToPage();
                }

                // Chuyển toàn bộ chi tiết dịch vụ của các bill phụ sang bill chính
                foreach (var ct in dsChiTietPhu)
                {
                    ct.HOA_DON_NHA_HANGMaHoaDonNhaHang = billChinh.MaHoaDonNhaHang;
                }

                // Xóa các bill phụ sau khi đã chuyển chi tiết
                _context.HOA_DON_NHA_HANG.RemoveRange(cacBillPhu);

                // Cập nhật thông tin & tính lại 5% phí phục vụ + 10% VAT thống nhất
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

        // Xử lý chốt hóa đơn: Hỗ trợ thanh toán ngay (lưu THANH_TOAN) hoặc Ghi nợ vào phòng đang lưu trú
        public async Task<IActionResult> OnPostThanhToanAsync(string maHoaDonToPay)
        {
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

                // Tính lại tổng tiền chính xác trên Server trước khi chốt
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

                string phuongThuc = string.IsNullOrWhiteSpace(PhuongThucThanhToan) ? "Tiền mặt" : PhuongThucThanhToan.Trim();

                if (phuongThuc == HangSoHeThong.HoaDonNHGhiNoPhong)
                {
                    if (string.IsNullOrWhiteSpace(MaDatPhongGhiNo))
                    {
                        TempData["ErrorMessage"] = "Vui lòng chọn phòng đang lưu trú để ghi nợ hóa đơn nhà hàng!";
                        return RedirectToPage();
                    }

                    bool hopLePhong = await _context.DAT_PHONG.AsNoTracking()
                        .AnyAsync(d => d.MaDatPhong == MaDatPhongGhiNo && d.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO);
                    if (!hopLePhong)
                    {
                        TempData["ErrorMessage"] = "Đơn lưu trú được chọn không còn ở trạng thái Đang ở!";
                        return RedirectToPage();
                    }

                    hd.DAT_PHONGMaDatPhong = MaDatPhongGhiNo;
                    hd.TrangThaiThanhToan = HangSoHeThong.HoaDonNHGhiNoPhong;
                    hd.HinhThucThanhToan = "Ghi nợ phòng";
                }
                else
                {
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
                }

                // Đồng loạt chuyển toàn bộ các bàn liên quan (bao gồm cả các bàn đã gộp) về trạng thái "Đang dọn dẹp"
                int soBanGiaiPhong = 0;
                if (!string.IsNullOrWhiteSpace(hd.BAN_NHA_HANGMaBan))
                {
                    var danhSachMaBan = hd.BAN_NHA_HANGMaBan
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
                    soBanGiaiPhong = cacBanLienQuan.Count;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = phuongThuc == HangSoHeThong.HoaDonNHGhiNoPhong
                    ? $"Đã chuyển hóa đơn {maHoaDonToPay} ({tongCong:N0}đ) ghi nợ vào đơn lưu trú {MaDatPhongGhiNo} và giải phóng {soBanGiaiPhong} bàn!"
                    : $"Chốt thanh toán hóa đơn {maHoaDonToPay} ({tongCong:N0}đ - {phuongThuc}) thành công! Đã chuyển {soBanGiaiPhong} bàn sang Đang dọn dẹp.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xử lý thanh toán hóa đơn nhà hàng {MaHD}", maHoaDonToPay);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi chốt thanh toán hóa đơn.";
            }

            return RedirectToPage();
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
