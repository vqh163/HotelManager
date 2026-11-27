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

        public List<HoaDonViewModel> DanhSachHoaDon { get; set; } = new List<HoaDonViewModel>();
        public List<PhongDangOOptionDto> DanhSachPhongDangO { get; set; } = new List<PhongDangOOptionDto>();

        [BindProperty]
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt";

        [BindProperty]
        public string? MaDatPhongGhiNo { get; set; }

        public async Task OnGetAsync()
        {
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

                int stt = 1;
                foreach (var hd in dsHoaDonDb)
                {
                    var banTuongUng = dsBanDb.FirstOrDefault(b => b.MaBan == hd.BAN_NHA_HANGMaBan);
                    string tenBanHienThi = banTuongUng != null
                        ? (banTuongUng.TenBan ?? banTuongUng.MaBan).Replace("Bàn ", "").Replace(" - Tầng trệt", "").Trim()
                        : (hd.BAN_NHA_HANGMaBan ?? "Chưa gắn bàn");

                    if (tenBanHienThi.Length == 1 && char.IsDigit(tenBanHienThi[0]))
                        tenBanHienThi = "0" + tenBanHienThi;

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
                        TenBan = $"Bàn: {tenBanHienThi}",
                        HinhThucThanhToan = trangThaiHienThi,
                        DanhSachMonJson = finalJson,
                        TamTinh = tamTinh,
                        PhiPhucVu = phiPhucVu,
                        ThueVAT = thueVAT,
                        TongCong = tongCong
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải dữ liệu Quầy thu ngân.");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tải danh sách hóa đơn nhà hàng.");
            }
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

                if (hd.TrangThaiThanhToan == HangSoHeThong.HoaDonNHDaThanhToan ||
                    hd.TrangThaiThanhToan == HangSoHeThong.HoaDonNHGhiNoPhong)
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

                // Chuyển bàn về trạng thái "Đang dọn dẹp"
                if (!string.IsNullOrWhiteSpace(hd.BAN_NHA_HANGMaBan))
                {
                    var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == hd.BAN_NHA_HANGMaBan);
                    if (ban != null)
                    {
                        ban.TrangThai = "Đang dọn dẹp";
                        _context.BAN_NHA_HANG.Update(ban);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = phuongThuc == HangSoHeThong.HoaDonNHGhiNoPhong
                    ? $"Đã chuyển hóa đơn {maHoaDonToPay} ({tongCong:N0}đ) ghi nợ vào đơn lưu trú {MaDatPhongGhiNo}!"
                    : $"Chốt thanh toán hóa đơn {maHoaDonToPay} ({tongCong:N0}đ - {phuongThuc}) thành công!";
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
