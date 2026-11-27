using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    /// <summary>
    /// Trang Tất toán Hóa đơn Check-out & Xem lại Biên lai thanh toán (P2).
    /// Thực hiện tổng hợp tiền phòng, dịch vụ Minibar/Đền bù, nhà hàng ghi nợ, chiết khấu hạng thành viên,
    /// thuế VAT, khấu trừ cọc, tạo HOA_DON & THANH_TOAN và cộng điểm thưởng KHACH_HANG.
    /// </summary>
    [Authorize]
    public class HoaDonCheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HoaDonCheckoutModel> _logger;

        public HoaDonCheckoutModel(ApplicationDbContext context, ILogger<HoaDonCheckoutModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class DongChiTietHoaDon
        {
            public string HangMuc { get; set; } = string.Empty;
            public string MoTa { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien { get; set; }
        }

        // Thuộc tính Binding nhận dữ liệu từ Form tất toán (PascalCase + [BindProperty])
        [BindProperty(SupportsGet = true)]
        public string MaDatPhong { get; set; } = string.Empty;

        [BindProperty]
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt";

        [BindProperty]
        public string? MaGiaoDichNgoai { get; set; }

        // Dữ liệu hiển thị trên giao diện hóa đơn
        public DAT_PHONG DatPhong { get; set; } = new DAT_PHONG();
        public KHACH_HANG KhachHang { get; set; } = new KHACH_HANG();
        public PHONG Phong { get; set; } = new PHONG();
        public HOA_DON? HoaDonDaChot { get; set; }
        public THANH_TOAN? ThanhToanDaChot { get; set; }
        public List<DongChiTietHoaDon> DanhSachChiTiet { get; set; } = new List<DongChiTietHoaDon>();

        public int SoDemO { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal TienNhaHang { get; set; }
        public decimal TyLeGiamGia { get; set; }
        public decimal TienGiamGia { get; set; }
        public decimal TienThueVAT { get; set; }
        public decimal TienCocDaTru { get; set; }
        public decimal TongTienThanhToan { get; set; }
        public int DiemThuongDuKien { get; set; }
        public bool CoTheChotThanhToan { get; set; }

        [TempData] public string? ThongBao { get; set; }
        [TempData] public string? ThongBaoLoi { get; set; }

        private bool LaLeTanHoacQuanLy =>
            User.IsInRole(HangSoHeThong.VaiTroLeTan)
            || User.IsInRole(HangSoHeThong.VaiTroQuanLy)
            || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);

        // Hàm phụ trợ: Nạp dữ liệu hóa đơn & tính toán chi tiết hoàn toàn bằng LINQ
        private async Task<IActionResult?> TaiDuLieuHoaDonAsync(string maDatPhong)
        {
            var dp = await _context.DAT_PHONG.AsNoTracking().FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);
            if (dp == null) return NotFound("Không tìm thấy đơn đặt phòng.");

            // Bảo mật chống IDOR: Chỉ Lễ tân/Quản lý/Giám đốc hoặc chính chủ đơn đặt phòng mới được xem hóa đơn
            if (!LaLeTanHoacQuanLy)
            {
                var tk = await _context.TAI_KHOAN.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == User.Identity!.Name);
                if (tk == null || tk.KHACH_HANGMaKH != dp.KHACH_HANGMaKH)
                    return Forbid();
            }

            DatPhong = dp;
            KhachHang = await _context.KHACH_HANG.AsNoTracking()
                .FirstOrDefaultAsync(k => k.MaKH == dp.KHACH_HANGMaKH) ?? new KHACH_HANG();

            var chiTietPhong = await _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                .FirstOrDefaultAsync(c => c.DAT_PHONGMaDatPhong == dp.MaDatPhong);

            Phong = chiTietPhong != null
                ? (await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == chiTietPhong.PHONGMaPhong) ?? new PHONG())
                : new PHONG();

            HoaDonDaChot = await _context.HOA_DON.AsNoTracking()
                .OrderByDescending(h => h.NgayLap)
                .FirstOrDefaultAsync(h => h.DAT_PHONGMaDatPhong == dp.MaDatPhong);

            if (HoaDonDaChot != null)
            {
                ThanhToanDaChot = await _context.THANH_TOAN.AsNoTracking()
                    .OrderByDescending(t => t.ThoiGianGiaoDich)
                    .FirstOrDefaultAsync(t => t.HOA_DONMaHoaDon == HoaDonDaChot.MaHoaDon);
            }

            // 1. Tính tiền phòng theo số đêm thực tế: max(1, ngày trả thực tế - ngày nhận)
            DateTime mocNhan = dp.NgayNhan ?? DateTime.Now;
            DateTime mocTra = (HoaDonDaChot?.NgayLap) ?? (dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDaCheckOut && dp.NgayTra.HasValue ? dp.NgayTra.Value : DateTime.Now);
            SoDemO = Math.Max(1, (mocTra.Date - mocNhan.Date).Days);

            decimal donGiaPhong = chiTietPhong?.DonGia ?? (Phong.GiaTheoNgay ?? 0m);
            TienPhong = donGiaPhong * SoDemO;

            DanhSachChiTiet.Add(new DongChiTietHoaDon
            {
                HangMuc = "Tiền phòng",
                MoTa = $"Phòng {Phong.SoPhong} ({Phong.LoaiPhong}) - Từ {mocNhan:dd/MM/yyyy} đến {mocTra:dd/MM/yyyy}",
                SoLuong = SoDemO,
                DonGia = donGiaPhong,
                ThanhTien = TienPhong
            });

            // 2. Tổng hợp các dịch vụ Minibar & Đền bù sự cố phát sinh trong kỳ lưu trú
            var dsDichVuPhong = await _context.CHI_TIET_DICH_VU.AsNoTracking()
                .Where(c => c.DAT_PHONGMaDatPhong == dp.MaDatPhong
                         || (string.IsNullOrEmpty(c.DAT_PHONGMaDatPhong)
                             && string.IsNullOrEmpty(c.HOA_DON_NHA_HANGMaHoaDonNhaHang)
                             && c.PHONGMaPhong == Phong.MaPhong
                             && c.ThoiGianSuDung >= mocNhan.AddMinutes(-5)
                             && c.ThoiGianSuDung <= mocTra.AddMinutes(5)))
                .ToListAsync();

            var tenDvDict = await _context.DICH_VU.AsNoTracking()
                .ToDictionaryAsync(d => d.MaDichVu, d => d.TenDichVu ?? d.MaDichVu);

            TienDichVu = 0m;
            foreach (var ct in dsDichVuPhong)
            {
                decimal tt = ct.ThanhTien ?? ((ct.DonGia ?? 0m) * (ct.SoLuong ?? 1));
                TienDichVu += tt;
                string tenDv = (!string.IsNullOrEmpty(ct.DICH_VUMaDichVu) && tenDvDict.TryGetValue(ct.DICH_VUMaDichVu, out var name))
                    ? name : "Dịch vụ phòng / Minibar";

                DanhSachChiTiet.Add(new DongChiTietHoaDon
                {
                    HangMuc = "Dịch vụ / Minibar",
                    MoTa = tenDv,
                    SoLuong = ct.SoLuong ?? 1,
                    DonGia = ct.DonGia ?? tt,
                    ThanhTien = tt
                });
            }

            // 3. Tổng hợp các hóa đơn Nhà hàng ghi nợ về phòng (đã bao gồm phí phục vụ + VAT)
            var dsNhaHangGhiNo = await _context.HOA_DON_NHA_HANG.AsNoTracking()
                .Where(h => h.DAT_PHONGMaDatPhong == dp.MaDatPhong)
                .ToListAsync();

            TienNhaHang = 0m;
            foreach (var hdNH in dsNhaHangGhiNo)
            {
                decimal ttNH = hdNH.ThanhTien ?? 0m;
                TienNhaHang += ttNH;
                DanhSachChiTiet.Add(new DongChiTietHoaDon
                {
                    HangMuc = "Nhà hàng (Ghi nợ)",
                    MoTa = $"Hóa đơn nhà hàng #{hdNH.MaHoaDonNhaHang} (đã gồm VAT & phí phục vụ)",
                    SoLuong = 1,
                    DonGia = ttNH,
                    ThanhTien = ttNH
                });
            }

            // 4. Nếu hóa đơn đã chốt trước đó thì hiển thị đúng số liệu đã lưu, ngược lại tính mới theo chính sách
            if (HoaDonDaChot != null)
            {
                TienPhong = HoaDonDaChot.TienPhong ?? TienPhong;
                TienDichVu = HoaDonDaChot.TienDichVu ?? TienDichVu;
                TienNhaHang = HoaDonDaChot.TienNhaHang ?? TienNhaHang;
                TienGiamGia = HoaDonDaChot.TienGiamGia ?? 0m;
                TienThueVAT = HoaDonDaChot.TienThueVAT ?? 0m;
                TienCocDaTru = HoaDonDaChot.TienCocDaTru ?? 0m;
                TongTienThanhToan = HoaDonDaChot.TongTienThanhToan ?? 0m;
                CoTheChotThanhToan = false;
            }
            else
            {
                TyLeGiamGia = HangSoHeThong.LayTyLeGiamGiaTheoHang(KhachHang.HangThanhVien);
                TienGiamGia = Math.Round(TienPhong * TyLeGiamGia, 0);
                TienThueVAT = Math.Round((TienPhong - TienGiamGia + TienDichVu) * HangSoHeThong.TyLeThueVAT, 0);
                TienCocDaTru = dp.TienCoc ?? 0m;
                TongTienThanhToan = Math.Max(0, (TienPhong - TienGiamGia + TienDichVu + TienThueVAT + TienNhaHang) - TienCocDaTru);
                DiemThuongDuKien = (int)Math.Floor(TongTienThanhToan / 100000m);
                CoTheChotThanhToan = LaLeTanHoacQuanLy && dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO;
            }

            return null;
        }

        public async Task<IActionResult> OnGetAsync(string maDatPhong)
        {
            if (string.IsNullOrWhiteSpace(maDatPhong))
                return RedirectToPage("/QuanLySoDoPhong/SoDoPhong");

            MaDatPhong = maDatPhong;
            var err = await TaiDuLieuHoaDonAsync(maDatPhong);
            return err ?? Page();
        }

        // Xác nhận thu tiền & chốt hóa đơn Check-out trong 1 Transaction duy nhất
        public async Task<IActionResult> OnPostXacNhanThanhToanAsync()
        {
            if (!LaLeTanHoacQuanLy) return Forbid();
            if (string.IsNullOrWhiteSpace(MaDatPhong)) return RedirectToPage("/QuanLySoDoPhong/SoDoPhong");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var err = await TaiDuLieuHoaDonAsync(MaDatPhong);
                if (err != null) return err;

                if (HoaDonDaChot != null)
                {
                    ThongBaoLoi = "Đơn đặt phòng này đã được lập hóa đơn và tất toán trước đó!";
                    return RedirectToPage(new { maDatPhong = MaDatPhong });
                }

                var datPhongDb = await _context.DAT_PHONG.FirstOrDefaultAsync(d => d.MaDatPhong == MaDatPhong);
                if (datPhongDb == null || datPhongDb.TrangThaiDatPhong != HangSoHeThong.DatPhongDangO)
                {
                    ThongBaoLoi = "Đơn đặt phòng không còn ở trạng thái Đang ở để thực hiện Check-out!";
                    return RedirectToPage(new { maDatPhong = MaDatPhong });
                }

                DateTime now = DateTime.Now;
                string maHoaDon = BaoMatHelper.SinhMaNgauNhien("HDP");

                // 1. Tạo bản ghi Hóa đơn tổng hợp HOA_DON
                var hoaDonMoi = new HOA_DON
                {
                    MaHoaDon = maHoaDon,
                    KHACH_HANGMaKH = datPhongDb.KHACH_HANGMaKH,
                    NHAN_VIENMaNV = User.FindFirst("MaNV")?.Value,
                    DAT_PHONGMaDatPhong = datPhongDb.MaDatPhong,
                    NgayLap = now,
                    TienPhong = TienPhong,
                    TienDichVu = TienDichVu,
                    TienNhaHang = TienNhaHang,
                    TienThueVAT = TienThueVAT,
                    TienCocDaTru = TienCocDaTru,
                    TienGiamGia = TienGiamGia,
                    TongTienThanhToan = TongTienThanhToan,
                    TrangThaiThanhToan = "Đã thanh toán"
                };
                _context.HOA_DON.Add(hoaDonMoi);

                // 2. Tạo bản ghi Giao dịch thanh toán THANH_TOAN
                _context.THANH_TOAN.Add(new THANH_TOAN
                {
                    MaThanhToan = BaoMatHelper.SinhMaNgauNhien("TT"),
                    HOA_DONMaHoaDon = maHoaDon,
                    SoTien = TongTienThanhToan,
                    PhuongThucThanhToan = string.IsNullOrWhiteSpace(PhuongThucThanhToan) ? "Tiền mặt" : PhuongThucThanhToan.Trim(),
                    MaGiaoDichNgoai = MaGiaoDichNgoai?.Trim(),
                    ThoiGianGiaoDich = now,
                    TrangThaiGiaoDich = "Thành công"
                });

                // 3. Chốt các hóa đơn nhà hàng ghi nợ về phòng sang Đã thanh toán
                var dsNhaHangNo = await _context.HOA_DON_NHA_HANG
                    .Where(h => h.DAT_PHONGMaDatPhong == datPhongDb.MaDatPhong)
                    .ToListAsync();
                foreach (var hdNH in dsNhaHangNo)
                {
                    hdNH.TrangThaiThanhToan = HangSoHeThong.HoaDonNHDaThanhToan;
                    hdNH.HinhThucThanhToan = "Đã Thanh Toán (Gộp phòng)";
                }

                // 4. Hoàn tất đơn đặt phòng và đóng tất cả phiếu lưu trú của phòng
                datPhongDb.TrangThaiDatPhong = HangSoHeThong.DatPhongDaCheckOut;
                datPhongDb.NgayTra = now;
                _context.DAT_PHONG.Update(datPhongDb);

                if (!string.IsNullOrWhiteSpace(Phong.MaPhong))
                {
                    var phieuLuuTrus = await _context.PHIEU_LUU_TRU
                        .Where(p => p.PHONGMaPhong == Phong.MaPhong && p.TrangThaiLuuTru == "DANG_LUU_TRU")
                        .ToListAsync();
                    foreach (var plt in phieuLuuTrus)
                    {
                        plt.ThoiGianCheckOut = now;
                        plt.TrangThaiLuuTru = "DA_CHECK_OUT";
                    }

                    // Giải phóng trạng thái vật lý phòng về Trống - Bẩn chờ Buồng phòng dọn
                    var phongDb = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == Phong.MaPhong);
                    if (phongDb != null && !string.IsNullOrWhiteSpace(phongDb.MaTrangThai))
                    {
                        var ttDb = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == phongDb.MaTrangThai);
                        if (ttDb != null)
                        {
                            ttDb.TrangThaiPhong = "Trống";
                            if (!string.Equals(ttDb.TrangThaiVeSinh?.Trim(), "Đang dọn dẹp", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(ttDb.TrangThaiVeSinh?.Trim(), "Sạch", StringComparison.OrdinalIgnoreCase))
                            {
                                ttDb.TrangThaiVeSinh = "Bẩn";
                                ttDb.ThoiGianDonVeSinh = now.AddDays(-1);
                            }
                        }
                    }
                }

                // 5. Cộng điểm thưởng tích lũy (1 điểm / 100.000đ) và tự động xét nâng hạng thành viên
                int diemCong = (int)Math.Floor(TongTienThanhToan / 100000m);
                var khachDb = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == datPhongDb.KHACH_HANGMaKH);
                if (khachDb != null)
                {
                    int tongDiemMoi = (khachDb.DiemThuong ?? 0) + diemCong;
                    khachDb.DiemThuong = tongDiemMoi;
                    khachDb.HangThanhVien = HangSoHeThong.XacDinhHangThanhVien(tongDiemMoi);
                    _context.KHACH_HANG.Update(khachDb);
                }

                // 6. Ghi nhật ký hệ thống
                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                    TenDangNhap = User.Identity?.Name ?? "Lễ tân",
                    HanhDong = "Tất toán Check-out",
                    ChiTiet = $"Chốt hóa đơn {maHoaDon} cho Phòng {Phong.SoPhong} (Đơn {datPhongDb.MaDatPhong}): {TongTienThanhToan:N0}đ ({PhuongThucThanhToan}), +{diemCong} điểm thưởng.",
                    ThoiGian = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ThongBao = $"Tất toán hóa đơn {maHoaDon} thành công ({TongTienThanhToan:N0}đ)! Khách được cộng +{diemCong} điểm thưởng.";
                return RedirectToPage(new { maDatPhong = MaDatPhong });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi tất toán hóa đơn Check-out cho đơn {MaDP}", MaDatPhong);
                ThongBaoLoi = "Đã xảy ra lỗi hệ thống khi chốt hóa đơn Check-out.";
                return RedirectToPage(new { maDatPhong = MaDatPhong });
            }
        }
    }
}
