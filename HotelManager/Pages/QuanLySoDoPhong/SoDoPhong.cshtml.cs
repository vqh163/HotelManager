using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Phân quyền truy cập theo Policy chuẩn hóa (tách chi tiết từng Handler bên dưới)
    [Authorize(Policy = HangSoHeThong.PolicySoDoPhong)]
    public class SoDoPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Cờ kiểm tra quyền theo từng nghiệp vụ trong trang dùng chung
        private bool IsFrontDesk => User.IsInRole(HangSoHeThong.VaiTroLeTan)
            || User.IsInRole(HangSoHeThong.VaiTroQuanLy)
            || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);

        private bool IsHousekeeping => User.IsInRole(HangSoHeThong.VaiTroBuongPhong)
            || User.IsInRole(HangSoHeThong.VaiTroBuongPhongCu)
            || IsFrontDesk;

        public SoDoPhongModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Class DTO lưu thông tin từng hành khách trong phòng (Serialize sang JSON cho Modal)
        public class OccupantInfoDto
        {
            public string MaPhieuLuuTru { get; set; } = string.Empty;
            public string MaKH { get; set; } = string.Empty;
            public string HoTen { get; set; } = string.Empty;
            public string SoGiayTo { get; set; } = string.Empty;
            public string LoaiGiayTo { get; set; } = "CCCD (Việt Nam)";
            public string SDT { get; set; } = string.Empty;
            public string ThoiGianCheckIn { get; set; } = string.Empty;
            public string ThoiGianCheckOut { get; set; } = string.Empty;
            public string TrangThaiLuuTru { get; set; } = "DANG_LUU_TRU";
            public string AnhMatTruoc { get; set; } = string.Empty;
            public string AnhMatSau { get; set; } = string.Empty;
            public bool LaKhachDaiDien { get; set; } = false;
            public bool DaKhaiBaoTamTru { get; set; } = true;
        }

        // Class hứng dữ liệu khi Lễ tân thêm nhiều khách ở cùng một lúc
        public class CompanionInputModel
        {
            public string HoTen { get; set; } = string.Empty;
            public string LoaiGiayTo { get; set; } = "CCCD (Việt Nam)";
            public string SoGiayTo { get; set; } = string.Empty;
            public string? SDT { get; set; }
            public IFormFile? AnhMatTruoc { get; set; }
            public IFormFile? AnhMatSau { get; set; }
        }

        // Class lưu thông tin phòng Trống để chọn đổi phòng
        public class AvailableRoomOptionDto
        {
            public string MaPhong { get; set; } = string.Empty;
            public string SoPhong { get; set; } = string.Empty;
            public string LoaiPhong { get; set; } = string.Empty;
            public int SucChuaToiDa { get; set; }
            public decimal GiaTheoNgay { get; set; }
            public string TrangThaiVeSinh { get; set; } = "Sạch";
        }

        // ViewModel hiển thị từng ô phòng trên Sơ đồ phòng
        public class RoomGridViewModel
        {
            public string MaPhong { get; set; } = default!;
            public string? SoPhong { get; set; }
            public string? LoaiPhong { get; set; }
            public int? SoGiuong { get; set; }
            public int? SucChuaToiDa { get; set; }

            public string? TrangThaiPhong { get; set; }
            public string? TrangThaiVeSinh { get; set; }

            // Ngày dọn phòng gần nhất để hiển thị trong Lựa chọn 1 của Buồng phòng
            public string NgayDonGanNhat { get; set; } = "Chưa cập nhật";

            // Màu sắc phân biệt cho ô trạng thái vệ sinh trên lưới phòng
            public string MauBadgeVeSinh { get; set; } = "bg-success text-white";

            public string? IconVeSinh { get; set; }
            public string? MauIconVeSinh { get; set; }
            public int SoNgayChuaDon { get; set; }

            public string Tang => !string.IsNullOrEmpty(SoPhong) ? SoPhong.Substring(0, 1) : "1";
            public string? MaDatPhongHienTai { get; set; }
            public string? TenKhachHang { get; set; }
            public string? SDTKhachHang { get; set; }
            public string? GhiChuKhachHang { get; set; }
            public DateTime? NgayNhan { get; set; }
            public DateTime? NgayTra { get; set; }

            public int TongSoKhachDangKy { get; set; } = 1;
            public int SoKhachDaCheckInThucTe { get; set; } = 0;
            public int SoKhachDangOThucTe { get; set; } = 0;
            public int SoKhachDaRoiDiTruoc { get; set; } = 0;
            public bool DaCheckInDuKhach => SoKhachDaCheckInThucTe >= TongSoKhachDangKy;
            public string DanhSachKhachJson { get; set; } = "[]";

            public bool IsCheckInValid { get; set; } = false;
            public bool IsCheckOutValid { get; set; } = false;
            public bool IsBookingValid { get; set; } = false;
            public bool IsWalkInCheckInValid { get; set; } = false;

            public decimal GiaDangApDung { get; set; } = 0;

            // Cờ theo dõi quy trình phối hợp Báo cáo tiêu thụ Minibar giữa Buồng phòng và Lễ tân
            public bool DangChoBuongPhongKiemTra { get; set; } = false;
            public bool BuongPhongDaBaoCaoCheckout { get; set; } = false;
            public string NoiDungBaoCaoBuongPhong { get; set; } = string.Empty;
        }

        // === CÁC THUỘC TÍNH BINDING NHẬN DỮ LIỆU TỪ FORM HTML CỦA LỄ TÂN (GIỮ NGUYÊN 100%) ===
        [BindProperty(SupportsGet = true)]
        public DateTime? ThoiGianLoc { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? OpenRoom { get; set; }

        [BindProperty]
        public string MaPhongOChung { get; set; } = string.Empty;

        [BindProperty]
        public List<CompanionInputModel> DanhSachKhachOChung { get; set; } = new List<CompanionInputModel>();

        [BindProperty]
        public string MaPhongRoiDi { get; set; } = string.Empty;

        [BindProperty]
        public string MaPhieuLuuTruRoiDi { get; set; } = string.Empty;

        [BindProperty]
        public string MaKHRoiDi { get; set; } = string.Empty;

        [BindProperty]
        public bool LaKhachDaiDienRoiDi { get; set; } = false;

        [BindProperty]
        public bool CatDichVuVaAnSang { get; set; } = true;

        [BindProperty]
        public bool BaoGiamTamTruCongAn { get; set; } = true;

        [BindProperty]
        public string? MaKHDaiDienMoi { get; set; }

        [BindProperty]
        public string? SDTDaiDienMoi { get; set; }

        [BindProperty]
        public string? PhuongAnCongNo { get; set; }

        [BindProperty]
        public string MaPhongCuDoi { get; set; } = string.Empty;

        [BindProperty]
        public string MaPhongMoiDoi { get; set; } = string.Empty;

        [BindProperty]
        public string NguyenNhanDoiPhong { get; set; } = "LoiKhachSan";

        [BindProperty]
        public string LyDoChiTietDoiPhong { get; set; } = string.Empty;

        [BindProperty]
        public decimal GiaApDungPhongMoi { get; set; }

        [BindProperty]
        public decimal ChiPhiMinibarPhongCu { get; set; } = 0;

        [BindProperty]
        public string? GhiChuKiemTraBuongPhong { get; set; }

        [BindProperty]
        public bool DaThuHoiTheTuVaChuyenDo { get; set; } = true;

        [BindProperty]
        public bool KhachDaiDienKyXacNhanGia { get; set; } = false;

        [BindProperty]
        public string? ChuKyDienTuKhachDaiDien { get; set; }

        // === CÁC THUỘC TÍNH BINDING DÀNH RIÊNG CHO NHÂN VIÊN BUỒNG PHÒNG (PascalCase) ===
        [BindProperty]
        public string MaPhongBuongPhong { get; set; } = string.Empty;

        // 4 mặt hàng tiêu thụ Minibar cố định: CocaCola (30,000), Nước lọc (15,000), 7Up (30,000), Bia (40,000)
        [BindProperty]
        public int SoLuongCocaColaBP { get; set; } = 0;

        [BindProperty]
        public int SoLuongNuocLocBP { get; set; } = 0;

        [BindProperty]
        public int SoLuongSevenUpBP { get; set; } = 0;

        [BindProperty]
        public int SoLuongBiaBP { get; set; } = 0;

        [BindProperty]
        public string? MaDichVuMinibarBP { get; set; }

        [BindProperty]
        public int SoLuongMinibarBP { get; set; } = 0;

        [BindProperty]
        public decimal ChiPhiHongHocBP { get; set; } = 0;

        [BindProperty]
        public string? GhiChuKiemTraCheckoutBP { get; set; }

        [BindProperty]
        public string MoTaSuCoKT { get; set; } = string.Empty;

        [BindProperty]
        public string MucDoUuTienKT { get; set; } = "Trung bình";

        [BindProperty]
        public IFormFile? AnhSuCoKT { get; set; }
        // Tùy chọn khóa phòng sang Bảo trì và đánh dấu lỗi do khách hàng gây ra (Liên kết UC_07)
        [BindProperty]
        public bool KhoaPhongDeSuaKT { get; set; } = false;

        [BindProperty]
        public bool DoKhachLamHongKT { get; set; } = false;

        public bool IsViewingToday { get; set; } = true;
        public bool IsViewingPast { get; set; } = false;
        public DateTime ThoiGianDangXem { get; set; }

        public List<RoomGridViewModel> DanhSachLieuPhong { get; set; } = new List<RoomGridViewModel>();
        public List<AvailableRoomOptionDto> DanhSachPhongTrongDeDoi { get; set; } = new List<AvailableRoomOptionDto>();
        public List<DICH_VU> DanhSachDichVuMinibar { get; set; } = new List<DICH_VU>();

        // =========================================================================
        // HÀM HỖ TRỢ QUAN TRỌNG: Đảm bảo mỗi phòng có 1 bản ghi TRANG_THAI_PHONG riêng
        // =========================================================================
        private async Task<TRANG_THAI_PHONG> EnsureRoomStatusRecordAsync(PHONG phong)
        {
            string maTTRieng = "TTP_" + (phong.SoPhong ?? phong.MaPhong).Trim();
            if (maTTRieng.Length > 20) maTTRieng = maTTRieng.Substring(0, 20);

            var ttRieng = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == maTTRieng);
            if (ttRieng == null)
            {
                TRANG_THAI_PHONG? ttCu = null;
                if (!string.IsNullOrWhiteSpace(phong.MaTrangThai))
                {
                    ttCu = await _context.TRANG_THAI_PHONG.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                }

                ttRieng = new TRANG_THAI_PHONG
                {
                    MaTrangThai = maTTRieng,
                    TrangThaiPhong = ttCu?.TrangThaiPhong ?? "Trống",
                    TrangThaiVeSinh = ttCu?.TrangThaiVeSinh ?? "Sạch",
                    ThoiGianDonVeSinh = ttCu?.ThoiGianDonVeSinh ?? DateTime.Now
                };
                _context.TRANG_THAI_PHONG.Add(ttRieng);
            }

            if (phong.MaTrangThai != maTTRieng)
            {
                phong.MaTrangThai = maTTRieng;
                _context.PHONG.Update(phong);
            }

            return ttRieng;
        }

        // Hàm hỗ trợ khởi tạo hoặc lấy mã dịch vụ chuẩn cho 4 món Minibar (CocaCola, Nước lọc, 7Up, Bia)
        private async Task<DICH_VU> EnsureMinibarServiceItemAsync(string maDV, string tenDV, decimal donGia)
        {
            var dv = await _context.DICH_VU.FirstOrDefaultAsync(d => d.MaDichVu == maDV || d.TenDichVu == tenDV);
            if (dv == null)
            {
                dv = new DICH_VU
                {
                    MaDichVu = maDV,
                    TenDichVu = tenDV,
                    LoaiDichVu = "Minibar",
                    DonGia = donGia,
                    DonViTinh = "Lon/Chai"
                };
                _context.DICH_VU.Add(dv);
                await _context.SaveChangesAsync();
            }
            return dv;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime today = now.Date;

                ThoiGianDangXem = ThoiGianLoc ?? now;
                DateTime targetDate = ThoiGianDangXem.Date;

                IsViewingToday = (targetDate == today);
                IsViewingPast = (targetDate < today);

                DanhSachDichVuMinibar = await _context.DICH_VU.AsNoTracking()
                    .OrderBy(d => d.TenDichVu)
                    .ToListAsync();

                // Sử dụng AsNoTracking (chỉ đọc) trong OnGetAsync — KHÔNG ghi đè CSDL khi chỉ xem sơ đồ
                var danhSachPhongDb = await _context.PHONG.AsNoTracking().ToListAsync();
                var danhSachTrangThaiDb = await _context.TRANG_THAI_PHONG.AsNoTracking().ToListAsync();

                // TRUY VẤN 1: Lấy các đơn đặt phòng đang hiệu lực tại ngày đang xem bằng LINQ (bao gồm cả "Đã xác nhận")
                var activeBookings = await _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                    .Join(_context.DAT_PHONG.AsNoTracking(),
                          ctdp => ctdp.DAT_PHONGMaDatPhong,
                          dp => dp.MaDatPhong,
                          (ctdp, dp) => new { ctdp, dp })
                    .Join(_context.KHACH_HANG.AsNoTracking(),
                          joined => joined.dp.KHACH_HANGMaKH,
                          kh => kh.MaKH,
                          (joined, kh) => new
                          {
                              MaDatPhong = joined.dp.MaDatPhong,
                              MaPhong = joined.ctdp.PHONGMaPhong,
                              DonGiaHienTai = joined.ctdp.DonGia,
                              MaKHDaiDien = kh.MaKH,
                              TenKhachHang = kh.HoTen,
                              SDTKhachHang = kh.SDT,
                              SoCCCDDaiDien = kh.SoCCCD,
                              GhiChuDacBietDaiDien = kh.GhiChuDacBiet,
                              GhiChu = joined.dp.GhiChu,
                              TrangThaiDat = joined.dp.TrangThaiDatPhong,
                              NgayNhan = joined.dp.NgayNhan,
                              NgayTra = joined.dp.NgayTra,
                              SoLuongKhach = joined.dp.SoLuongKhach
                          })
                    .Where(x =>
                        (IsViewingToday && x.TrangThaiDat == HangSoHeThong.DatPhongDangO)
                        ||
                        (x.NgayNhan.HasValue && x.NgayTra.HasValue
                         && x.NgayNhan.Value.Date <= targetDate
                         && x.NgayTra.Value.Date >= targetDate
                         && (x.TrangThaiDat == HangSoHeThong.DatPhongDangO
                             || HangSoHeThong.TrangThaiChoCheckIn.Contains(x.TrangThaiDat ?? "")))
                    )
                    .ToListAsync();

                // TRUY VẤN 2: Lấy danh sách phiếu lưu trú của các phòng
                var allRoomStays = await (from plt in _context.PHIEU_LUU_TRU.AsNoTracking()
                                          join kh in _context.KHACH_HANG.AsNoTracking() on plt.KHACH_HANGMaKH equals kh.MaKH
                                          where plt.TrangThaiLuuTru == "DANG_LUU_TRU" || plt.TrangThaiLuuTru == "DA_ROI_DI"
                                          select new
                                          {
                                              plt.MaPhieuLuuTru,
                                              plt.PHONGMaPhong,
                                              plt.ThoiGianCheckIn,
                                              plt.ThoiGianCheckOut,
                                              plt.TrangThaiLuuTru,
                                              kh.MaKH,
                                              kh.HoTen,
                                              kh.SoCCCD,
                                              kh.SDT,
                                              kh.GhiChuDacBiet
                                          }).ToListAsync();

                foreach (var p in danhSachPhongDb)
                {
                    string maTTRieng = "TTP_" + (p.SoPhong ?? p.MaPhong).Trim();
                    if (maTTRieng.Length > 20) maTTRieng = maTTRieng.Substring(0, 20);
                    var tt = danhSachTrangThaiDb.FirstOrDefault(x => x.MaTrangThai == p.MaTrangThai)
                          ?? danhSachTrangThaiDb.FirstOrDefault(x => x.MaTrangThai == maTTRieng);
                    string veSinhHienThi = tt?.TrangThaiVeSinh?.Trim() ?? "Sạch";

                    var bookingTaiThoiDiem = activeBookings
                        .OrderByDescending(b => b.TrangThaiDat == "DANG_O")
                        .FirstOrDefault(b => b.MaPhong == p.MaPhong);

                    string trangThaiTinhToan = "Trống";
                    string? maDatPhong = null;
                    string? tenKhach = null;
                    string? sdtKhach = null;
                    string? ghiChu = null;
                    decimal giaApDung = p.GiaTheoNgay ?? 0;

                    int tongKhachDangKy = 1;
                    int soKhachDaCheckIn = 0;
                    int soKhachDangO = 0;
                    int soKhachRoiDiTruoc = 0;
                    var danhSachKhachTrongPhong = new List<OccupantInfoDto>();

                    bool hopLeCheckIn = false;
                    bool hopLeCheckOut = false;
                    bool hopLeDatPhong = false;
                    bool hopLeWalkIn = false;

                    bool dangChoBPKiemTra = false;
                    bool bpDaBaoCao = false;
                    string noiDungBaoCaoBP = "";

                    if (bookingTaiThoiDiem != null)
                    {
                        maDatPhong = bookingTaiThoiDiem.MaDatPhong;
                        tenKhach = bookingTaiThoiDiem.TenKhachHang;
                        sdtKhach = bookingTaiThoiDiem.SDTKhachHang;
                        ghiChu = bookingTaiThoiDiem.GhiChu;
                        giaApDung = bookingTaiThoiDiem.DonGiaHienTai ?? (p.GiaTheoNgay ?? 0);
                        tongKhachDangKy = (bookingTaiThoiDiem.SoLuongKhach.HasValue && bookingTaiThoiDiem.SoLuongKhach.Value > 0)
                                          ? bookingTaiThoiDiem.SoLuongKhach.Value : 1;

                        // Kiểm tra cờ phối hợp Báo cáo tiêu thụ Minibar giữa Buồng phòng và Lễ tân trong GhiChu
                        if (!string.IsNullOrEmpty(ghiChu))
                        {
                            if (ghiChu.Contains("[CHO_BP_KIEM_TRA]")) dangChoBPKiemTra = true;
                            if (ghiChu.Contains("[BP_DA_BAO_CAO:"))
                            {
                                bpDaBaoCao = true;
                                dangChoBPKiemTra = false;
                                int startIdx = ghiChu.LastIndexOf("[BP_DA_BAO_CAO:");
                                if (startIdx >= 0)
                                {
                                    int endIdx = ghiChu.IndexOf("]", startIdx);
                                    if (endIdx > startIdx)
                                    {
                                        noiDungBaoCaoBP = ghiChu.Substring(startIdx + 15, endIdx - (startIdx + 15)).Trim();
                                    }
                                }
                            }
                        }

                        if (dangChoBPKiemTra && veSinhHienThi.Equals("Sạch", StringComparison.OrdinalIgnoreCase))
                        {
                            veSinhHienThi = "Bẩn";
                        }

                        if (bookingTaiThoiDiem.TrangThaiDat == "DANG_O")
                        {
                            trangThaiTinhToan = "Đang ở";
                            if (IsViewingToday)
                            {
                                hopLeCheckOut = true;
                            }

                            DateTime mocNhanPhong = bookingTaiThoiDiem.NgayNhan?.Date ?? today;
                            var staysInRoom = allRoomStays
                                .Where(s => s.PHONGMaPhong == p.MaPhong
                                         && (!s.ThoiGianCheckIn.HasValue || s.ThoiGianCheckIn.Value.Date >= mocNhanPhong.AddDays(-1)))
                                .OrderByDescending(s => s.TrangThaiLuuTru == "DANG_LUU_TRU")
                                .ThenByDescending(s => s.MaKH == bookingTaiThoiDiem.MaKHDaiDien)
                                .ThenBy(s => s.ThoiGianCheckIn)
                                .ToList();

                            foreach (var s in staysInRoom)
                            {
                                ParseKycInfo(s.GhiChuDacBiet, out string loaiGiayTo, out string anhTruoc, out string anhSau);

                                bool dangO = (s.TrangThaiLuuTru == "DANG_LUU_TRU");

                                danhSachKhachTrongPhong.Add(new OccupantInfoDto
                                {
                                    MaPhieuLuuTru = s.MaPhieuLuuTru,
                                    MaKH = s.MaKH,
                                    HoTen = s.HoTen ?? "Khách lưu trú",
                                    SoGiayTo = s.SoCCCD ?? "Chưa cập nhật",
                                    LoaiGiayTo = loaiGiayTo,
                                    SDT = s.SDT ?? "",
                                    ThoiGianCheckIn = s.ThoiGianCheckIn.HasValue ? s.ThoiGianCheckIn.Value.ToString("dd/MM/yyyy HH:mm") : "",
                                    ThoiGianCheckOut = s.ThoiGianCheckOut.HasValue ? s.ThoiGianCheckOut.Value.ToString("dd/MM/yyyy HH:mm") : "",
                                    TrangThaiLuuTru = s.TrangThaiLuuTru ?? "DANG_LUU_TRU",
                                    AnhMatTruoc = anhTruoc,
                                    AnhMatSau = anhSau,
                                    LaKhachDaiDien = (s.MaKH == bookingTaiThoiDiem.MaKHDaiDien && dangO),
                                    DaKhaiBaoTamTru = dangO
                                });
                            }

                            if (danhSachKhachTrongPhong.Count == 0)
                            {
                                ParseKycInfo(bookingTaiThoiDiem.GhiChuDacBietDaiDien, out string loaiGiayTo, out string anhTruoc, out string anhSau);
                                danhSachKhachTrongPhong.Add(new OccupantInfoDto
                                {
                                    MaPhieuLuuTru = "",
                                    MaKH = bookingTaiThoiDiem.MaKHDaiDien,
                                    HoTen = bookingTaiThoiDiem.TenKhachHang ?? "Khách đại diện",
                                    SoGiayTo = bookingTaiThoiDiem.SoCCCDDaiDien ?? "Chưa cập nhật",
                                    LoaiGiayTo = loaiGiayTo,
                                    SDT = bookingTaiThoiDiem.SDTKhachHang ?? "",
                                    ThoiGianCheckIn = bookingTaiThoiDiem.NgayNhan.HasValue ? bookingTaiThoiDiem.NgayNhan.Value.ToString("dd/MM/yyyy HH:mm") : "",
                                    ThoiGianCheckOut = "",
                                    TrangThaiLuuTru = "DANG_LUU_TRU",
                                    AnhMatTruoc = anhTruoc,
                                    AnhMatSau = anhSau,
                                    LaKhachDaiDien = true,
                                    DaKhaiBaoTamTru = true
                                });
                            }

                            soKhachDangO = danhSachKhachTrongPhong.Count(x => x.TrangThaiLuuTru == "DANG_LUU_TRU");
                            soKhachRoiDiTruoc = danhSachKhachTrongPhong.Count(x => x.TrangThaiLuuTru == "DA_ROI_DI");
                            soKhachDaCheckIn = soKhachDangO + soKhachRoiDiTruoc;
                        }
                        else
                        {
                            trangThaiTinhToan = "Đã đặt";
                            if (IsViewingToday && bookingTaiThoiDiem.NgayNhan.HasValue && bookingTaiThoiDiem.NgayNhan.Value.Date == today)
                            {
                                hopLeCheckIn = true;
                            }
                        }
                    }
                    else
                    {
                        if (IsViewingToday)
                        {
                            string ttVatLy = tt?.TrangThaiPhong?.Trim() ?? (p.MaTrangThai == "TT04" ? "Bảo trì" : "Trống");
                            if (ttVatLy.Equals("Bảo trì", StringComparison.OrdinalIgnoreCase) ||
                                ttVatLy.Equals("DND", StringComparison.OrdinalIgnoreCase))
                            {
                                trangThaiTinhToan = ttVatLy;
                            }
                        }

                        if (trangThaiTinhToan == "Trống")
                        {
                            if (!IsViewingPast) hopLeDatPhong = true;
                            if (IsViewingToday) hopLeWalkIn = true;

                            if (IsViewingToday && tt != null)
                            {
                                int soNgayKhongKhach = tt.ThoiGianDonVeSinh.HasValue
                                    ? (today - tt.ThoiGianDonVeSinh.Value.Date).Days
                                    : 0;

                                // Chỉ tính hiển thị trên bộ nhớ (không ghi DB trong HTTP GET)
                                if (veSinhHienThi.Equals("Sạch", StringComparison.OrdinalIgnoreCase) && soNgayKhongKhach >= 1)
                                {
                                    veSinhHienThi = "Bẩn";
                                }
                            }

                            DanhSachPhongTrongDeDoi.Add(new AvailableRoomOptionDto
                            {
                                MaPhong = p.MaPhong,
                                SoPhong = p.SoPhong ?? p.MaPhong,
                                LoaiPhong = p.LoaiPhong ?? "Standard",
                                SucChuaToiDa = p.SucChuaToiDa ?? 2,
                                GiaTheoNgay = p.GiaTheoNgay ?? 0,
                                TrangThaiVeSinh = veSinhHienThi
                            });
                        }
                    }

                    var vm = new RoomGridViewModel
                    {
                        MaPhong = p.MaPhong,
                        SoPhong = p.SoPhong,
                        LoaiPhong = p.LoaiPhong,
                        SoGiuong = p.SoGiuong,
                        SucChuaToiDa = p.SucChuaToiDa,
                        TrangThaiPhong = trangThaiTinhToan,
                        TrangThaiVeSinh = veSinhHienThi,
                        NgayDonGanNhat = (tt != null && tt.ThoiGianDonVeSinh.HasValue)
                            ? tt.ThoiGianDonVeSinh.Value.ToString("dd/MM/yyyy HH:mm")
                            : "Chưa ghi nhận",
                        MaDatPhongHienTai = maDatPhong,
                        TenKhachHang = tenKhach,
                        SDTKhachHang = sdtKhach,
                        GhiChuKhachHang = ghiChu,
                        NgayNhan = bookingTaiThoiDiem?.NgayNhan,
                        NgayTra = bookingTaiThoiDiem?.NgayTra,
                        TongSoKhachDangKy = tongKhachDangKy,
                        SoKhachDaCheckInThucTe = soKhachDaCheckIn,
                        SoKhachDangOThucTe = soKhachDangO,
                        SoKhachDaRoiDiTruoc = soKhachRoiDiTruoc,
                        DanhSachKhachJson = JsonSerializer.Serialize(danhSachKhachTrongPhong),
                        IsCheckInValid = hopLeCheckIn,
                        IsCheckOutValid = hopLeCheckOut,
                        IsBookingValid = hopLeDatPhong,
                        IsWalkInCheckInValid = hopLeWalkIn,
                        GiaDangApDung = giaApDung,
                        DangChoBuongPhongKiemTra = dangChoBPKiemTra,
                        BuongPhongDaBaoCaoCheckout = bpDaBaoCao,
                        NoiDungBaoCaoBuongPhong = noiDungBaoCaoBP
                    };

                    if (tt != null && tt.ThoiGianDonVeSinh.HasValue)
                    {
                        vm.SoNgayChuaDon = (DateTime.Now.Date - tt.ThoiGianDonVeSinh.Value.Date).Days;
                    }
                    else
                    {
                        vm.SoNgayChuaDon = 0;
                    }

                    // Phân loại hiển thị trạng thái vệ sinh (Sạch, Bẩn nhẹ, Bẩn, Đang dọn dẹp)
                    string vsLower = vm.TrangThaiVeSinh.ToLower();
                    if (vsLower == "đang dọn dẹp" || vsLower == "đang dọn")
                    {
                        vm.TrangThaiVeSinh = "Đang dọn dẹp";
                        vm.IconVeSinh = "bi-hourglass-split";
                        vm.MauIconVeSinh = "text-info";
                        vm.MauBadgeVeSinh = "bg-info text-dark border-info";
                    }
                    else if (vsLower == "bẩn nhẹ")
                    {
                        vm.TrangThaiVeSinh = "Bẩn nhẹ";
                        vm.IconVeSinh = "bi-droplet-half";
                        vm.MauIconVeSinh = "text-warning";
                        vm.MauBadgeVeSinh = "bg-warning text-dark border-warning";
                    }
                    else if (vsLower.Contains("bẩn"))
                    {
                        vm.TrangThaiVeSinh = "Bẩn";
                        vm.IconVeSinh = "bi-exclamation-triangle-fill";
                        vm.MauIconVeSinh = "text-danger";
                        vm.MauBadgeVeSinh = "bg-danger text-white border-danger";
                    }
                    else
                    {
                        vm.TrangThaiVeSinh = "Sạch";
                        vm.IconVeSinh = "bi-stars";
                        vm.MauIconVeSinh = "text-success";
                        vm.MauBadgeVeSinh = "bg-success text-white border-success";
                    }

                    DanhSachLieuPhong.Add(vm);
                }

                DanhSachLieuPhong = DanhSachLieuPhong.OrderBy(x => x.SoPhong).ToList();
                DanhSachPhongTrongDeDoi = DanhSachPhongTrongDeDoi.OrderBy(x => x.SoPhong).ToList();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tải sơ đồ phòng: {ex.Message}");
                return Page();
            }
        }

        private void ParseKycInfo(string? ghiChuDacBiet, out string loaiGiayTo, out string anhMatTruoc, out string anhMatSau)
        {
            loaiGiayTo = "CCCD (Việt Nam)";
            anhMatTruoc = "";
            anhMatSau = "";

            if (!string.IsNullOrWhiteSpace(ghiChuDacBiet) && ghiChuDacBiet.StartsWith("KYC|"))
            {
                var parts = ghiChuDacBiet.Split('|');
                if (parts.Length >= 4)
                {
                    loaiGiayTo = parts[1];
                    anhMatTruoc = parts[2];
                    anhMatSau = parts[3];
                }
            }
        }

        private bool KiemTraHopLeGiayTo(string? loaiGiayTo, string? soGiayTo, out string soGiayToChuan, out string thongBaoLoi)
        {
            soGiayToChuan = (soGiayTo ?? string.Empty).Trim().ToUpperInvariant();
            thongBaoLoi = string.Empty;

            bool laCCCD = string.IsNullOrWhiteSpace(loaiGiayTo) || loaiGiayTo.Contains("CCCD", StringComparison.OrdinalIgnoreCase);

            if (laCCCD)
            {
                if (!Regex.IsMatch(soGiayToChuan, @"^\d{12}$"))
                {
                    thongBaoLoi = $"Số CCCD '{soGiayTo}' không hợp lệ! Khách trong nước bắt buộc nhập đúng chính xác 12 chữ số.";
                    return false;
                }
            }
            else
            {
                if (!Regex.IsMatch(soGiayToChuan, @"^[A-Z0-9]{9}$"))
                {
                    thongBaoLoi = $"Số Passport '{soGiayTo}' không hợp lệ! Bắt buộc phải đủ chính xác 9 ký tự (chỉ gồm chữ cái IN HOA hoặc chữ số, không có khoảng trắng hay ký tự đặc biệt).";
                    return false;
                }
            }

            return true;
        }

        private async Task<string> SaveIdentityImageAsync(IFormFile? file, string prefix, string subFolder = "kyc")
        {
            if (file == null || file.Length == 0) return string.Empty;

            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            var (ok, ketQua) = await BaoMatHelper.LuuAnhAnToanAsync(file, uploadFolder, prefix);
            if (!ok)
            {
                throw new InvalidOperationException(ketQua);
            }

            return $"/uploads/{subFolder}/{ketQua}";
        }

        // =========================================================================
        // CÁC HANDLER DÀNH CHO NHÂN VIÊN BUỒNG PHÒNG (3 LỰA CHỌN TRONG MODAL BUỒNG PHÒNG)
        // =========================================================================

        // LỰA CHỌN 1: Chuyển trạng thái vệ sinh phòng theo chu trình hiện tại (Bẩn / Bẩn nhẹ -> Đang dọn dẹp -> Sạch)
        public async Task<IActionResult> OnPostChuyenTrangThaiBuongPhongAsync()
        {
            if (!IsHousekeeping) return Forbid();
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == MaPhongBuongPhong);
                if (phong == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phòng cần chuyển trạng thái!";
                    return RedirectToPage();
                }

                var trangThai = await EnsureRoomStatusRecordAsync(phong);
                string vsHienTai = (trangThai.TrangThaiVeSinh ?? "Bẩn").Trim();

                if (vsHienTai.Contains("Bẩn", StringComparison.OrdinalIgnoreCase))
                {
                    trangThai.TrangThaiVeSinh = "Đang dọn dẹp";
                    _context.TRANG_THAI_PHONG.Update(trangThai);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Phòng {phong.SoPhong} đã chuyển sang trạng thái 'Đang dọn dẹp'. Sau khi dọn xong, hãy ấn thêm 1 lần nữa để chuyển sang 'Sạch'!";
                }
                else if (vsHienTai.Equals("Đang dọn dẹp", StringComparison.OrdinalIgnoreCase) || vsHienTai.Equals("Đang dọn", StringComparison.OrdinalIgnoreCase))
                {
                    trangThai.TrangThaiVeSinh = "Sạch";
                    trangThai.ThoiGianDonVeSinh = DateTime.Now;
                    _context.TRANG_THAI_PHONG.Update(trangThai);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Hoàn tất dọn phòng! Phòng {phong.SoPhong} đã được chuyển sang trạng thái 'Sạch' (Nếu sau 1 ngày không có khách vào ở sẽ tự động chuyển lại thành Bẩn).";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} hiện đang ở trạng thái Sạch, không cần thực hiện hành động nào thêm.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi chuyển trạng thái phòng: {ex.Message}";
            }

            return RedirectToPage();
        }

        // LỰA CHỌN 2: Báo cáo cho Lễ tân (Gửi bản tiêu thụ: CocaCola 30k, Nước lọc 15k, 7Up 30k, Bia 40k)
        // ĐÃ CẬP NHẬT RÀNG BUỘC CHẶN SỐ LƯỢNG ÂM Ở TẦNG BACKEND
        public async Task<IActionResult> OnPostBaoCaoMinibarCheckoutAsync()
        {
            if (!IsHousekeeping) return Forbid();
            try
            {
                // RÀNG BUỘC DỮ LIỆU TẦNG SERVER: Không cho phép nhận số lượng tiêu thụ âm
                if (SoLuongCocaColaBP < 0 || SoLuongNuocLocBP < 0 || SoLuongSevenUpBP < 0 || SoLuongBiaBP < 0)
                {
                    TempData["ErrorMessage"] = "Dữ liệu không hợp lệ! Số lượng đồ uống tiêu thụ Minibar không được phép là số âm.";
                    return RedirectToPage();
                }

                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == MaPhongBuongPhong);
                if (phong == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin phòng!";
                    return RedirectToPage();
                }

                // Tìm đơn đặt phòng đang ở (DANG_O) của phòng này
                var datPhong = await (from ct in _context.CHI_TIET_DAT_PHONG
                                      join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                      where ct.PHONGMaPhong == MaPhongBuongPhong && dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO
                                      select dp).FirstOrDefaultAsync();

                if (datPhong == null)
                {
                    TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} hiện không có khách đang lưu trú để báo cáo Minibar!";
                    return RedirectToPage();
                }

                // Thay thế bản ghi Minibar cũ của đơn lưu trú này nếu Buồng phòng gửi lại báo cáo (tránh cộng dồn trùng)
                string[] maMinibarChuan = { "MB_COCA", "MB_NUOCLOC", "MB_7UP", "MB_BIA" };
                var minibarCu = await _context.CHI_TIET_DICH_VU
                    .Where(c => c.DAT_PHONGMaDatPhong == datPhong.MaDatPhong
                             && c.DICH_VUMaDichVu != null
                             && maMinibarChuan.Contains(c.DICH_VUMaDichVu))
                    .ToListAsync();
                if (minibarCu.Any())
                {
                    _context.CHI_TIET_DICH_VU.RemoveRange(minibarCu);
                }

                DateTime now = DateTime.Now;
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

                // Danh sách 4 mặt hàng tiêu chuẩn theo yêu cầu
                var danhSachMon = new List<(string MaDV, string TenDV, decimal DonGia, int SoLuong)>
                {
                    ("MB_COCA", "CocaCola", 30000m, SoLuongCocaColaBP),
                    ("MB_NUOCLOC", "Nước lọc", 15000m, SoLuongNuocLocBP),
                    ("MB_7UP", "7Up", 30000m, SoLuongSevenUpBP),
                    ("MB_BIA", "Bia", 40000m, SoLuongBiaBP)
                };

                var cacMonDaDung = new List<string>();
                decimal tongTienTieuThu = 0m;
                int indexItem = 0;

                foreach (var mon in danhSachMon)
                {
                    if (mon.SoLuong > 0)
                    {
                        indexItem++;
                        var dvEntity = await EnsureMinibarServiceItemAsync(mon.MaDV, mon.TenDV, mon.DonGia);
                        decimal thanhTienMon = mon.DonGia * mon.SoLuong;
                        tongTienTieuThu += thanhTienMon;

                        var ctDichVu = new CHI_TIET_DICH_VU
                        {
                            MaChiTietDichVu = BaoMatHelper.SinhMaNgauNhien("DV"),
                            DICH_VUMaDichVu = dvEntity.MaDichVu,
                            DAT_PHONGMaDatPhong = datPhong.MaDatPhong,
                            PHONGMaPhong = phong.MaPhong,
                            KHACH_HANGMaKH = datPhong.KHACH_HANGMaKH,
                            SoLuong = mon.SoLuong,
                            DonGia = mon.DonGia,
                            ThanhTien = thanhTienMon,
                            ThoiGianSuDung = now
                        };
                        _context.CHI_TIET_DICH_VU.Add(ctDichVu);
                        cacMonDaDung.Add($"{mon.TenDV} x{mon.SoLuong} ({thanhTienMon:N0}đ)");
                    }
                }

                string chiTietMinibarText = cacMonDaDung.Any()
                    ? $"{string.Join(", ", cacMonDaDung)} => Tổng: {tongTienTieuThu:N0}đ"
                    : "Không tiêu thụ đồ uống (0đ)";

                string ghiChuThem = !string.IsNullOrWhiteSpace(GhiChuKiemTraCheckoutBP)
                    ? $" | Ghi chú: {GhiChuKiemTraCheckoutBP.Trim()}"
                    : "";

                string tomTatBaoCao = $"{chiTietMinibarText}{ghiChuThem}";

                // Ghi nhận vào đơn đặt phòng để hiện Chấm đỏ trên sơ đồ phòng của Lễ tân & hiển thị lúc Checkout
                if (datPhong != null)
                {
                    string ghiChuCu = (datPhong.GhiChu ?? "").Replace("[CHO_BP_KIEM_TRA]", "");
                    int oldIdx = ghiChuCu.IndexOf("[BP_DA_BAO_CAO:");
                    if (oldIdx >= 0)
                    {
                        int oldEnd = ghiChuCu.IndexOf("]", oldIdx);
                        if (oldEnd > oldIdx)
                        {
                            ghiChuCu = ghiChuCu.Remove(oldIdx, oldEnd - oldIdx + 1).Trim();
                        }
                    }

                    string ghiChuMoi = $"{ghiChuCu} [BP_DA_BAO_CAO: {tomTatBaoCao}]".Trim();
                    datPhong.GhiChu = ghiChuMoi.Length > 255 ? ghiChuMoi.Substring(ghiChuMoi.Length - 255) : ghiChuMoi;
                    _context.DAT_PHONG.Update(datPhong);
                }

                // Lưu vào NHAT_KY_HE_THONG để thông báo cho Lễ tân
                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = $"LG{now:yyMMddHHmmss}{suffix}",
                    TenDangNhap = User.Identity?.Name ?? "Buồng phòng",
                    HanhDong = "Báo cáo Tiêu thụ Minibar",
                    ChiTiet = $"[Lễ tân] Bản tiêu thụ Phòng {phong.SoPhong}: {tomTatBaoCao}"
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã gửi bản tiêu thụ của Phòng {phong.SoPhong} ({chiTietMinibarText}) đến giao diện Lễ tân!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gửi bản tiêu thụ: {ex.Message}";
            }

            return RedirectToPage();
        }

        // LỰA CHỌN 3: Báo cáo sự cố kỹ thuật (Giữ nguyên 100% logic hiện tại)
        // LỰA CHỌN 3: Báo cáo sự cố kỹ thuật (Đồng bộ 100% với quy trình 4 bước của UC_07)
        public async Task<IActionResult> OnPostTaoPhieuKyThuatAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MaPhongBuongPhong) || string.IsNullOrWhiteSpace(MoTaSuCoKT))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập nội dung mô tả sự cố kỹ thuật!";
                    return RedirectToPage();
                }

                // 1. Lấy phòng có Tracking để cập nhật trạng thái Bảo trì khi cần
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == MaPhongBuongPhong);
                if (phong == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin phòng xảy ra sự cố!";
                    return RedirectToPage();
                }

                string soPhongText = phong.SoPhong ?? MaPhongBuongPhong;
                string viTriChot = $"Phòng {soPhongText}";
                string mucDoUuTien = string.IsNullOrWhiteSpace(MucDoUuTienKT) ? "Trung bình" : MucDoUuTienKT.Trim();

                DateTime now = DateTime.Now;
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

                // 2. Lưu hình ảnh hiện trường đính kèm vào thư mục uploads/suco (không trộn chung với ảnh KYC)
                string anhSuCoUrl = await SaveIdentityImageAsync(AnhSuCoKT, $"SUCO_{soPhongText}", "suco");

                // 3. Tra cứu Mã NV và Họ tên người tạo phiếu từ TAI_KHOAN & NHAN_VIEN bằng LINQ
                string tenDangNhap = User.Identity?.Name ?? "Buồng phòng";
                var tk = await _context.TAI_KHOAN.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                string tenNguoiTao = tenDangNhap;
                if (tk != null && !string.IsNullOrWhiteSpace(tk.NHAN_VIENMaNV))
                {
                    var nv = await _context.NHAN_VIEN.AsNoTracking()
                        .FirstOrDefaultAsync(n => n.MaNV == tk.NHAN_VIENMaNV);
                    if (nv != null && !string.IsNullOrWhiteSpace(nv.HoTen))
                    {
                        tenNguoiTao = nv.HoTen;
                    }
                }

                // 4. Tự động khóa phòng sang "Bảo trì" nếu ưu tiên là Cao/Khẩn cấp hoặc có tích chọn Khóa phòng
                bool canKhoaPhong = KhoaPhongDeSuaKT || mucDoUuTien == "Khẩn cấp" || mucDoUuTien == "Cao";
                string thongBaoKhoaPhong = "";

                if (canKhoaPhong)
                {
                    // Kiểm tra xem phòng hiện có đang có khách lưu trú (DANG_O) hay không
                    bool phongDangCoKhach = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                                   join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                   where ct.PHONGMaPhong == phong.MaPhong && dp.TrangThaiDatPhong == "DANG_O"
                                                   select dp.MaDatPhong).AnyAsync();

                    var trangThaiPhong = await EnsureRoomStatusRecordAsync(phong);
                    trangThaiPhong.TrangThaiPhong = "Bảo trì";
                    _context.TRANG_THAI_PHONG.Update(trangThaiPhong);

                    thongBaoKhoaPhong = phongDangCoKhach
                        ? $" Đã khóa Bảo trì (Lưu ý: {viTriChot} đang có khách ở, cần báo Lễ tân thực hiện Đổi phòng!)."
                        : $" Đã tự động chuyển {viTriChot} sang trạng thái 'Bảo trì' trên Sơ đồ phòng.";
                }

                // 5. Khởi tạo đối tượng PHIEU_BAO_CAO_SU_CO đầy đủ trường dữ liệu cho UC_07
                var phieuSuCo = new PHIEU_BAO_CAO_SU_CO
                {
                    MaPhieuBaoCao = $"SC{now:yyMMddHHmmss}{suffix}",
                    NHAN_VIENMaNV = tk?.NHAN_VIENMaNV,
                    NguoiTaoTen = tenNguoiTao,
                    MaPhong = phong.MaPhong,
                    ViTriSuCo = viTriChot,
                    MoTaSuCo = MoTaSuCoKT.Trim(),
                    MucDoUuTien = mucDoUuTien,
                    HinhAnhDinhKem = anhSuCoUrl,
                    TrangThai = "Chờ tiếp nhận",
                    DoKhachLamHong = DoKhachLamHongKT,
                    CoKhoaPhong = canKhoaPhong,
                    ChiPhiPhatSinh = 0,
                    TongChiPhi = 0,
                    ThoiGianTao = now
                };
                _context.PHIEU_BAO_CAO_SU_CO.Add(phieuSuCo);

                // 6. Ghi thông báo gửi đến Nhân viên Kỹ thuật (cắt an toàn <= 255 ký tự)
                string chiTietLog = $"[Nhân viên Kỹ thuật] Phiếu sự cố {phieuSuCo.MaPhieuBaoCao} tại {viTriChot} (Ưu tiên: {mucDoUuTien}): {MoTaSuCoKT.Trim()}";
                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = $"LG{now:yyMMddHHmmss}{suffix}",
                    TenDangNhap = tenDangNhap,
                    HanhDong = "Yêu cầu Kỹ thuật",
                    ChiTiet = chiTietLog.Length > 255 ? chiTietLog.Substring(0, 255) : chiTietLog
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo Phiếu báo cáo kỹ thuật ({phieuSuCo.MaPhieuBaoCao}) cho {viTriChot} và gửi thông báo đến Nhân viên Kỹ thuật!{thongBaoKhoaPhong}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tạo phiếu kỹ thuật: {ex.Message}";
            }

            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostDoiTrangThaiAsync(string maPhong, string trangThaiMoi, string loaiCapNhat)
        {
            if (!IsHousekeeping) return Forbid();
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong != null)
                {
                    var trangThai = await EnsureRoomStatusRecordAsync(phong);
                    if (loaiCapNhat == "BuongPhong")
                    {
                        trangThai.TrangThaiVeSinh = trangThaiMoi.Trim();
                        if (trangThaiMoi.Trim().ToLower() == "sạch")
                        {
                            trangThai.ThoiGianDonVeSinh = DateTime.Now;
                        }
                    }
                    _context.TRANG_THAI_PHONG.Update(trangThai);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật vệ sinh phòng {phong.SoPhong} thành Sạch!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi cập nhật: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCheckInAsync(
            string maPhongCheckIn,
            string? soGiayToCheckIn,
            string? loaiGiayToCheckIn,
            IFormFile? anhMatTruocCheckIn,
            IFormFile? anhMatSauCheckIn)
        {
            if (!IsFrontDesk) return Forbid();
            try
            {
                if (!KiemTraHopLeGiayTo(loaiGiayToCheckIn, soGiayToCheckIn, out string soGiayToChuanCheckIn, out string loiGiayTo))
                {
                    TempData["ErrorMessage"] = loiGiayTo;
                    return RedirectToPage();
                }

                DateTime now = DateTime.Now;
                DateTime today = now.Date;

                // SỬA LỖI ĐỨT LUỒNG: Chấp nhận cả đơn đặt phòng có trạng thái "Đã xác nhận" do nhân viên đặt
                var datPhongCanCheckIn = await (from ct in _context.CHI_TIET_DAT_PHONG
                                                join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                where ct.PHONGMaPhong == maPhongCheckIn
                                                   && HangSoHeThong.TrangThaiChoCheckIn.Contains(dp.TrangThaiDatPhong ?? "")
                                                   && dp.NgayNhan.HasValue
                                                   && dp.NgayNhan.Value.Date == today
                                                select dp).FirstOrDefaultAsync();

                if (datPhongCanCheckIn == null)
                {
                    TempData["ErrorMessage"] = $"Từ chối Check-in! Không có lịch nhận phòng hợp lệ trong ngày hôm nay ({today:dd/MM/yyyy}).";
                    return RedirectToPage();
                }

                var khachDaiDien = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == datPhongCanCheckIn.KHACH_HANGMaKH);
                if (khachDaiDien != null)
                {
                    // Kiểm tra trùng CCCD với hồ sơ khách hàng khác trước khi cập nhật
                    bool trungCCCD = await _context.KHACH_HANG.AsNoTracking()
                        .AnyAsync(k => k.SoCCCD == soGiayToChuanCheckIn && k.MaKH != khachDaiDien.MaKH);
                    if (trungCCCD)
                    {
                        TempData["ErrorMessage"] = $"Số giấy tờ '{soGiayToChuanCheckIn}' đã gắn với một hồ sơ khách hàng khác!";
                        return RedirectToPage();
                    }

                    khachDaiDien.SoCCCD = soGiayToChuanCheckIn;

                    string urlTruoc = await SaveIdentityImageAsync(anhMatTruocCheckIn, $"CI1_F_{khachDaiDien.MaKH}");
                    string urlSau = await SaveIdentityImageAsync(anhMatSauCheckIn, $"CI1_B_{khachDaiDien.MaKH}");
                    string loaiGT = string.IsNullOrWhiteSpace(loaiGiayToCheckIn) ? "CCCD (Việt Nam)" : loaiGiayToCheckIn;

                    if (!string.IsNullOrEmpty(urlTruoc) || !string.IsNullOrEmpty(urlSau))
                    {
                        khachDaiDien.GhiChuDacBiet = $"KYC|{loaiGT}|{urlTruoc}|{urlSau}";
                    }
                    _context.KHACH_HANG.Update(khachDaiDien);
                }

                datPhongCanCheckIn.TrangThaiDatPhong = HangSoHeThong.DatPhongDangO;
                datPhongCanCheckIn.NgayNhan = now;
                _context.DAT_PHONG.Update(datPhongCanCheckIn);

                var phieuLuuTruDauTien = new PHIEU_LUU_TRU
                {
                    MaPhieuLuuTru = BaoMatHelper.SinhMaNgauNhien("PLT"),
                    NHAN_VIENMaNV = User.FindFirst("MaNV")?.Value,
                    PHONGMaPhong = maPhongCheckIn,
                    KHACH_HANGMaKH = datPhongCanCheckIn.KHACH_HANGMaKH,
                    ThoiGianCheckIn = now,
                    TrangThaiLuuTru = "DANG_LUU_TRU"
                };
                _context.PHIEU_LUU_TRU.Add(phieuLuuTruDauTien);

                await _context.SaveChangesAsync();

                int tongKhach = datPhongCanCheckIn.SoLuongKhach ?? 1;
                TempData["SuccessMessage"] = tongKhach > 1
                    ? $"Đã Check-in người đại diện (1/{tongKhach} khách)! Nhấn vào phòng để thêm đồng loạt {tongKhach - 1} khách đi cùng."
                    : "Check-in hoàn tất (1/1 khách)! Phòng đã chuyển sang trạng thái Đang ở.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi Check-in: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostWalkInCheckInAsync(
            string maPhongWalkIn,
            string hoTenWalkIn,
            string sdtWalkIn,
            string loaiGiayToWalkIn,
            string cccdWalkIn,
            int soKhachWalkIn,
            int soNgayOWalkIn,
            string? ghiChuWalkIn,
            IFormFile? anhMatTruocWalkIn,
            IFormFile? anhMatSauWalkIn)
        {
            if (!IsFrontDesk) return Forbid();
            try
            {
                if (string.IsNullOrWhiteSpace(maPhongWalkIn) || string.IsNullOrWhiteSpace(hoTenWalkIn) || string.IsNullOrWhiteSpace(sdtWalkIn) || string.IsNullOrWhiteSpace(cccdWalkIn))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Họ tên, SĐT và Số CCCD/Hộ chiếu của khách!";
                    return RedirectToPage();
                }

                if (!KiemTraHopLeGiayTo(loaiGiayToWalkIn, cccdWalkIn, out string soGiayToChuan, out string loiGiayTo))
                {
                    TempData["ErrorMessage"] = loiGiayTo;
                    return RedirectToPage();
                }

                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhongWalkIn);
                if (phong == null) return RedirectToPage();

                // Kiểm tra phòng không ở trạng thái Bảo trì và không trùng lịch đặt phòng trước
                bool dangBaoTri = phong.MaTrangThai == "TT04"
                    || (!string.IsNullOrWhiteSpace(phong.MaTrangThai)
                        && await _context.TRANG_THAI_PHONG.AsNoTracking().AnyAsync(t => t.MaTrangThai == phong.MaTrangThai && t.TrangThaiPhong == "Bảo trì"));
                if (dangBaoTri)
                {
                    TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} hiện đang khóa Bảo trì, không thể nhận khách Walk-in!";
                    return RedirectToPage();
                }

                DateTime now = DateTime.Now;
                DateTime today = now.Date;
                if (soNgayOWalkIn <= 0) soNgayOWalkIn = 1;
                if (soKhachWalkIn <= 0) soKhachWalkIn = 1;
                if (soKhachWalkIn > (phong.SucChuaToiDa ?? 2))
                {
                    TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} chỉ cho phép tối đa {phong.SucChuaToiDa ?? 2} khách!";
                    return RedirectToPage();
                }

                DateTime ngayTraDuKien = today.AddDays(soNgayOWalkIn).AddHours(12);

                bool trungLich = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                        join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                        where ct.PHONGMaPhong == phong.MaPhong
                                           && !HangSoHeThong.TrangThaiDaKetThuc.Contains(dp.TrangThaiDatPhong ?? "")
                                           && dp.NgayNhan < ngayTraDuKien
                                           && dp.NgayTra > now
                                        select dp.MaDatPhong).AnyAsync();
                if (trungLich)
                {
                    TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} đã có lịch đặt trước hoặc đang có khách trong khoảng thời gian này!";
                    return RedirectToPage();
                }

                string urlTruoc = await SaveIdentityImageAsync(anhMatTruocWalkIn, "WI_F");
                string urlSau = await SaveIdentityImageAsync(anhMatSauWalkIn, "WI_B");
                string loaiGT = string.IsNullOrWhiteSpace(loaiGiayToWalkIn) ? "CCCD (Việt Nam)" : loaiGiayToWalkIn;
                string kycMetadata = $"KYC|{loaiGT}|{urlTruoc}|{urlSau}";

                // Tìm khách theo CCCD trước rồi mới tới SĐT để không vi phạm Unique Index CCCD
                var khachHang = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.SoCCCD == soGiayToChuan)
                             ?? await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.SDT == sdtWalkIn.Trim());

                if (khachHang == null)
                {
                    khachHang = new KHACH_HANG
                    {
                        MaKH = BaoMatHelper.SinhMaNgauNhien("KH"),
                        HoTen = hoTenWalkIn.Trim(),
                        SDT = sdtWalkIn.Trim(),
                        SoCCCD = soGiayToChuan,
                        HangThanhVien = "Thành viên mới",
                        DiemThuong = 0,
                        GhiChuDacBiet = kycMetadata
                    };
                    _context.KHACH_HANG.Add(khachHang);
                }
                else
                {
                    khachHang.HoTen = hoTenWalkIn.Trim();
                    khachHang.SDT = sdtWalkIn.Trim();
                    khachHang.SoCCCD = soGiayToChuan;
                    khachHang.GhiChuDacBiet = kycMetadata;
                    _context.KHACH_HANG.Update(khachHang);
                }

                string maDatPhongMoi = BaoMatHelper.SinhMaNgauNhien("DP");
                var datPhongWalkIn = new DAT_PHONG
                {
                    MaDatPhong = maDatPhongMoi,
                    KHACH_HANGMaKH = khachHang.MaKH,
                    NgayDat = now,
                    NgayNhan = now,
                    NgayTra = ngayTraDuKien,
                    SoLuongKhach = soKhachWalkIn,
                    TienCoc = 0,
                    TrangThaiDatPhong = HangSoHeThong.DatPhongDangO,
                    KenhDatPhong = "Walk-in tại quầy",
                    GhiChu = string.IsNullOrWhiteSpace(ghiChuWalkIn) ? "Khách vãng lai nhận phòng trực tiếp" : ghiChuWalkIn.Trim()
                };
                _context.DAT_PHONG.Add(datPhongWalkIn);

                var chiTietWalkIn = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = BaoMatHelper.SinhMaNgauNhien("CT"),
                    DAT_PHONGMaDatPhong = maDatPhongMoi,
                    PHONGMaPhong = phong.MaPhong,
                    DonGia = phong.GiaTheoNgay,
                    SoNgayO = soNgayOWalkIn
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTietWalkIn);

                var phieuLuuTru = new PHIEU_LUU_TRU
                {
                    MaPhieuLuuTru = BaoMatHelper.SinhMaNgauNhien("PLT"),
                    NHAN_VIENMaNV = User.FindFirst("MaNV")?.Value,
                    PHONGMaPhong = phong.MaPhong,
                    KHACH_HANGMaKH = khachHang.MaKH,
                    ThoiGianCheckIn = now,
                    TrangThaiLuuTru = "DANG_LUU_TRU"
                };
                _context.PHIEU_LUU_TRU.Add(phieuLuuTru);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = soKhachWalkIn > 1
                    ? $"Đã Check-in khách đại diện ({hoTenWalkIn} - 1/{soKhachWalkIn} khách). Nhấn vào phòng để khai báo cùng lúc cho {soKhachWalkIn - 1} khách ở cùng!"
                    : $"Đã Check-in hoàn tất (1/1 khách) cho phòng {phong.SoPhong}!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi Check-in khách vãng lai: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostThemKhachOChungAsync()
        {
            if (!IsFrontDesk) return Forbid();
            try
            {
                if (string.IsNullOrWhiteSpace(MaPhongOChung))
                {
                    TempData["ErrorMessage"] = "Không xác định được mã phòng cần bổ sung khách!";
                    return RedirectToPage();
                }

                // Kiểm tra phòng thực sự đang có đơn lưu trú hoạt động
                var datPhongHienTai = await (from ct in _context.CHI_TIET_DAT_PHONG
                                             join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                             where ct.PHONGMaPhong == MaPhongOChung && dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO
                                             select dp).FirstOrDefaultAsync();
                if (datPhongHienTai == null)
                {
                    TempData["ErrorMessage"] = "Phòng này hiện không có đơn lưu trú đang hoạt động!";
                    return RedirectToPage();
                }

                var danhSachHopLe = DanhSachKhachOChung?
                    .Where(k => !string.IsNullOrWhiteSpace(k.HoTen) && !string.IsNullOrWhiteSpace(k.SoGiayTo))
                    .ToList() ?? new List<CompanionInputModel>();

                if (!danhSachHopLe.Any())
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Họ tên và Số CCCD/Hộ chiếu cho ít nhất 1 khách ở cùng!";
                    return RedirectToPage();
                }

                for (int i = 0; i < danhSachHopLe.Count; i++)
                {
                    if (!KiemTraHopLeGiayTo(danhSachHopLe[i].LoaiGiayTo, danhSachHopLe[i].SoGiayTo, out string soGiayToChuanItem, out string loiGiayToItem))
                    {
                        TempData["ErrorMessage"] = $"Khách ở cùng thứ {i + 1} ({danhSachHopLe[i].HoTen}): {loiGiayToItem}";
                        return RedirectToPage();
                    }
                    danhSachHopLe[i].SoGiayTo = soGiayToChuanItem;

                    if (danhSachHopLe[i].AnhMatTruoc == null || danhSachHopLe[i].AnhMatSau == null)
                    {
                        TempData["ErrorMessage"] = $"Khách ở cùng thứ {i + 1} ({danhSachHopLe[i].HoTen}) chưa có đủ 2 ảnh Mặt trước & Mặt sau của CCCD/Hộ chiếu!";
                        return RedirectToPage();
                    }
                }

                DateTime now = DateTime.Now;
                var listPhieuMoi = new List<PHIEU_LUU_TRU>();

                for (int i = 0; i < danhSachHopLe.Count; i++)
                {
                    var item = danhSachHopLe[i];
                    string urlTruoc = await SaveIdentityImageAsync(item.AnhMatTruoc, $"COMP_F_{i + 1}");
                    string urlSau = await SaveIdentityImageAsync(item.AnhMatSau, $"COMP_B_{i + 1}");
                    string loaiGT = string.IsNullOrWhiteSpace(item.LoaiGiayTo) ? "CCCD (Việt Nam)" : item.LoaiGiayTo;
                    string kycMetadata = $"KYC|{loaiGT}|{urlTruoc}|{urlSau}";

                    // Tái sử dụng hồ sơ KHACH_HANG nếu CCCD đã tồn tại (tránh lỗi trùng Unique Index CCCD)
                    var khachDiCung = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.SoCCCD == item.SoGiayTo);
                    if (khachDiCung == null)
                    {
                        khachDiCung = new KHACH_HANG
                        {
                            MaKH = BaoMatHelper.SinhMaNgauNhien("KH"),
                            HoTen = item.HoTen.Trim(),
                            SoCCCD = item.SoGiayTo,
                            SDT = string.IsNullOrWhiteSpace(item.SDT) ? null : item.SDT.Trim(),
                            HangThanhVien = "Thành viên mới",
                            DiemThuong = 0,
                            GhiChuDacBiet = kycMetadata
                        };
                        _context.KHACH_HANG.Add(khachDiCung);
                    }
                    else
                    {
                        khachDiCung.HoTen = item.HoTen.Trim();
                        if (!string.IsNullOrWhiteSpace(item.SDT)) khachDiCung.SDT = item.SDT.Trim();
                        khachDiCung.GhiChuDacBiet = kycMetadata;
                        _context.KHACH_HANG.Update(khachDiCung);
                    }

                    listPhieuMoi.Add(new PHIEU_LUU_TRU
                    {
                        MaPhieuLuuTru = BaoMatHelper.SinhMaNgauNhien("PLT"),
                        NHAN_VIENMaNV = User.FindFirst("MaNV")?.Value,
                        PHONGMaPhong = MaPhongOChung,
                        KHACH_HANGMaKH = khachDiCung.MaKH,
                        ThoiGianCheckIn = now,
                        TrangThaiLuuTru = "DANG_LUU_TRU"
                    });
                }

                _context.PHIEU_LUU_TRU.AddRange(listPhieuMoi);
                await _context.SaveChangesAsync();

                int soDaCheckIn = await _context.PHIEU_LUU_TRU
                    .CountAsync(p => p.PHONGMaPhong == MaPhongOChung && (p.TrangThaiLuuTru == "DANG_LUU_TRU" || p.TrangThaiLuuTru == "DA_ROI_DI"));

                int tongDangKy = datPhongHienTai.SoLuongKhach ?? 1;

                if (soDaCheckIn > tongDangKy)
                {
                    datPhongHienTai.SoLuongKhach = soDaCheckIn;
                    tongDangKy = soDaCheckIn;
                    _context.DAT_PHONG.Update(datPhongHienTai);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = soDaCheckIn >= tongDangKy
                    ? $"Đã Check-in cùng lúc thêm {danhSachHopLe.Count} khách ở cùng! Phòng đã ĐỦ SỐ LƯỢNG ({soDaCheckIn}/{tongDangKy} khách)."
                    : $"Đã Check-in thêm {danhSachHopLe.Count} khách ở cùng ({soDaCheckIn}/{tongDangKy} khách). Còn {tongDangKy - soDaCheckIn} khách chưa làm thủ tục.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi bổ sung danh sách khách ở cùng: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostKhachRoiDiTruocAsync()
        {
            if (!IsFrontDesk) return Forbid();
            try
            {
                if (string.IsNullOrWhiteSpace(MaPhongRoiDi) || string.IsNullOrWhiteSpace(MaKHRoiDi))
                {
                    TempData["ErrorMessage"] = "Không xác định được thông tin hành khách cần báo rời đi trước!";
                    return RedirectToPage();
                }

                DateTime now = DateTime.Now;

                var datPhong = await (from ct in _context.CHI_TIET_DAT_PHONG
                                      join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                      where ct.PHONGMaPhong == MaPhongRoiDi && dp.TrangThaiDatPhong == "DANG_O"
                                      select dp).FirstOrDefaultAsync();

                if (datPhong == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn lưu trú đang hoạt động của phòng này!";
                    return RedirectToPage();
                }

                var khachRoiDi = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == MaKHRoiDi);
                string tenKhachRoiDi = khachRoiDi?.HoTen ?? MaKHRoiDi;

                var danhSachPhieuDangO = await _context.PHIEU_LUU_TRU
                    .Where(p => p.PHONGMaPhong == MaPhongRoiDi && p.TrangThaiLuuTru == "DANG_LUU_TRU")
                    .ToListAsync();

                var phieuCuaKhachRoiDi = danhSachPhieuDangO.FirstOrDefault(p => p.MaPhieuLuuTru == MaPhieuLuuTruRoiDi || p.KHACH_HANGMaKH == MaKHRoiDi);
                var cacPhieuConLai = danhSachPhieuDangO.Where(p => p.KHACH_HANGMaKH != MaKHRoiDi).ToList();

                if (!cacPhieuConLai.Any())
                {
                    TempData["ErrorMessage"] = $"Khách '{tenKhachRoiDi}' là người cuối cùng đang ở trong phòng. Vui lòng sử dụng nút 'Checkout' màu đỏ để trả phòng và tất toán hóa đơn!";
                    return RedirectToPage();
                }

                bool isPrimaryLeaving = (datPhong.KHACH_HANGMaKH == MaKHRoiDi) || LaKhachDaiDienRoiDi;

                string thongBaoChuyenDaiDien = "";
                if (isPrimaryLeaving)
                {
                    if (string.IsNullOrWhiteSpace(MaKHDaiDienMoi))
                    {
                        TempData["ErrorMessage"] = $"CẢNH BÁO TÀI CHÍNH: '{tenKhachRoiDi}' đang là KHÁCH ĐẠI DIỆN! Bắt buộc phải chọn 1 khách ở lại làm Người đại diện mới để chịu trách nhiệm công nợ trước khi cho phép rời đi.";
                        return RedirectToPage();
                    }

                    if (string.IsNullOrWhiteSpace(SDTDaiDienMoi))
                    {
                        TempData["ErrorMessage"] = "Vui lòng nhập Số điện thoại liên hệ của Người đại diện mới để xác nhận chuyển giao trách nhiệm thanh toán!";
                        return RedirectToPage();
                    }

                    var khachDaiDienMoi = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.MaKH == MaKHDaiDienMoi);
                    if (khachDaiDienMoi == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy hồ sơ của khách đại diện mới được chọn!";
                        return RedirectToPage();
                    }

                    khachDaiDienMoi.SDT = SDTDaiDienMoi.Trim();
                    _context.KHACH_HANG.Update(khachDaiDienMoi);

                    datPhong.KHACH_HANGMaKH = khachDaiDienMoi.MaKH;

                    var hoaDonTam = await _context.HOA_DON
                        .FirstOrDefaultAsync(h => h.DAT_PHONGMaDatPhong == datPhong.MaDatPhong && h.TrangThaiThanhToan != "Đã thanh toán");
                    if (hoaDonTam != null)
                    {
                        hoaDonTam.KHACH_HANGMaKH = khachDaiDienMoi.MaKH;
                        _context.HOA_DON.Update(hoaDonTam);
                    }

                    string ghiChuCongNo = string.IsNullOrWhiteSpace(PhuongAnCongNo)
                        ? "Chuyển toàn bộ công nợ cho đại diện mới"
                        : PhuongAnCongNo;

                    thongBaoChuyenDaiDien = $" | [Đổi Đại Diện {now:dd/MM HH:mm}: Từ {tenKhachRoiDi} sang {khachDaiDienMoi.HoTen} (SĐT: {SDTDaiDienMoi}) - {ghiChuCongNo}]";
                }

                if (phieuCuaKhachRoiDi != null)
                {
                    phieuCuaKhachRoiDi.ThoiGianCheckOut = now;
                    phieuCuaKhachRoiDi.TrangThaiLuuTru = "DA_ROI_DI";
                    _context.PHIEU_LUU_TRU.Update(phieuCuaKhachRoiDi);
                }

                int soKhachConLai = cacPhieuConLai.Count;
                string logRoiDi = $" | [Khách về trước {now:dd/MM HH:mm}: {tenKhachRoiDi}";
                if (BaoGiamTamTruCongAn) logRoiDi += " - Đã báo giảm tạm trú Công an";
                if (CatDichVuVaAnSang) logRoiDi += $" - Đã cắt suất ăn sáng/phụ thu đầu người từ ngày mai (Còn {soKhachConLai} khách)";
                logRoiDi += "]";

                string ghiChuMoi = (datPhong.GhiChu ?? "") + thongBaoChuyenDaiDien + logRoiDi;
                datPhong.GhiChu = ghiChuMoi.Length > 255 ? ghiChuMoi.Substring(ghiChuMoi.Length - 255) : ghiChuMoi;

                _context.DAT_PHONG.Update(datPhong);
                await _context.SaveChangesAsync();

                if (isPrimaryLeaving)
                {
                    TempData["SuccessMessage"] = $"[Kịch bản B hoàn tất] Đã chuyển quyền Khách đại diện từ '{tenKhachRoiDi}' sang đại diện mới, chốt phương án công nợ, báo giảm tạm trú Công an và ghi nhận '{tenKhachRoiDi}' rời đi lúc {now:HH:mm dd/MM/yyyy}!";
                }
                else
                {
                    TempData["SuccessMessage"] = $"[Kịch bản A hoàn tất] Đã ghi nhận khách ở cùng '{tenKhachRoiDi}' rời đi trước lúc {now:HH:mm dd/MM/yyyy}, tự động báo giảm tạm trú Công an và cắt suất ăn sáng/phụ thu đầu người từ ngày mai!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xử lý khách rời đi trước: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDoiPhongLuuTruAsync()
        {
            if (!IsFrontDesk) return Forbid();
            try
            {
                if (string.IsNullOrWhiteSpace(MaPhongCuDoi) || string.IsNullOrWhiteSpace(MaPhongMoiDoi))
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn phòng mới cần chuyển đến!";
                    return RedirectToPage();
                }

                if (MaPhongCuDoi == MaPhongMoiDoi)
                {
                    TempData["ErrorMessage"] = "Phòng mới phải khác với phòng hiện tại!";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(LyDoChiTietDoiPhong))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập rõ lý do đổi phòng!";
                    return RedirectToPage();
                }

                DateTime now = DateTime.Now;

                var phongCu = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == MaPhongCuDoi);
                var phongMoi = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == MaPhongMoiDoi);

                if (phongCu == null || phongMoi == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin phòng cũ hoặc phòng mới trong CSDL!";
                    return RedirectToPage();
                }

                var bookingPair = await (from ct in _context.CHI_TIET_DAT_PHONG
                                         join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                         where ct.PHONGMaPhong == MaPhongCuDoi && dp.TrangThaiDatPhong == "DANG_O"
                                         select new { ct, dp }).FirstOrDefaultAsync();

                if (bookingPair == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hồ sơ lưu trú đang hoạt động tại phòng cũ để thực hiện đổi phòng!";
                    return RedirectToPage();
                }

                var chiTietDatPhong = bookingPair.ct;
                var datPhong = bookingPair.dp;

                int soKhachDangO = await _context.PHIEU_LUU_TRU
                    .CountAsync(p => p.PHONGMaPhong == MaPhongCuDoi && p.TrangThaiLuuTru == "DANG_LUU_TRU");
                if (soKhachDangO == 0) soKhachDangO = datPhong.SoLuongKhach ?? 1;

                if (phongMoi.SucChuaToiDa.HasValue && soKhachDangO > phongMoi.SucChuaToiDa.Value)
                {
                    TempData["ErrorMessage"] = $"Từ chối đổi phòng! Phòng mới {phongMoi.SoPhong} chỉ có sức chứa tối đa {phongMoi.SucChuaToiDa} người, không đủ cho {soKhachDangO} khách đang lưu trú.";
                    return RedirectToPage();
                }

                // Kiểm tra phòng mới không bị khóa Bảo trì và không trùng lịch đặt trước
                bool phongMoiBaoTri = phongMoi.MaTrangThai == "TT04"
                    || (!string.IsNullOrWhiteSpace(phongMoi.MaTrangThai)
                        && await _context.TRANG_THAI_PHONG.AsNoTracking().AnyAsync(t => t.MaTrangThai == phongMoi.MaTrangThai && t.TrangThaiPhong == "Bảo trì"));
                if (phongMoiBaoTri)
                {
                    TempData["ErrorMessage"] = $"Phòng mới {phongMoi.SoPhong} hiện đang Bảo trì, không thể chuyển khách sang!";
                    return RedirectToPage();
                }

                bool phongMoiTrungLich = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                                join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                where ct.PHONGMaPhong == phongMoi.MaPhong
                                                   && dp.MaDatPhong != datPhong.MaDatPhong
                                                   && !HangSoHeThong.TrangThaiDaKetThuc.Contains(dp.TrangThaiDatPhong ?? "")
                                                   && dp.NgayNhan < (datPhong.NgayTra ?? now.AddDays(1))
                                                   && dp.NgayTra > now
                                                select dp.MaDatPhong).AnyAsync();
                if (phongMoiTrungLich)
                {
                    TempData["ErrorMessage"] = $"Phòng mới {phongMoi.SoPhong} đã có khách ở hoặc lịch đặt trùng thời gian lưu trú!";
                    return RedirectToPage();
                }

                decimal giaCu = chiTietDatPhong.DonGia ?? (phongCu.GiaTheoNgay ?? 0);
                decimal giaMoiNiemYet = phongMoi.GiaTheoNgay ?? giaCu;
                decimal giaChotApDung = giaCu;
                decimal chenhLech = 0;

                if (NguyenNhanDoiPhong == "LoiKhachSan")
                {
                    giaChotApDung = giaCu;
                    chenhLech = 0;
                }
                else
                {
                    giaChotApDung = GiaApDungPhongMoi > 0 ? GiaApDungPhongMoi : giaMoiNiemYet;
                    chenhLech = giaChotApDung - giaCu;

                    if (chenhLech != 0 && (!KhachDaiDienKyXacNhanGia || string.IsNullOrWhiteSpace(ChuKyDienTuKhachDaiDien)))
                    {
                        TempData["ErrorMessage"] = "Đổi phòng theo mong muốn khách hàng có phát sinh thay đổi giá! Bắt buộc Đại diện phòng phải tích xác nhận và ký tên đồng ý mức giá mới.";
                        return RedirectToPage();
                    }
                }

                chiTietDatPhong.PHONGMaPhong = phongMoi.MaPhong;
                chiTietDatPhong.DonGia = giaChotApDung;
                _context.CHI_TIET_DAT_PHONG.Update(chiTietDatPhong);

                DateTime mocNhanPhong = datPhong.NgayNhan?.Date ?? now.Date;
                var danhSachPhieuLuuTru = await _context.PHIEU_LUU_TRU
                    .Where(p => p.PHONGMaPhong == MaPhongCuDoi
                             && (p.TrangThaiLuuTru == "DANG_LUU_TRU" || (p.TrangThaiLuuTru == "DA_ROI_DI" && p.ThoiGianCheckIn >= mocNhanPhong.AddDays(-1))))
                    .ToListAsync();

                foreach (var phieu in danhSachPhieuLuuTru)
                {
                    phieu.PHONGMaPhong = phongMoi.MaPhong;
                }
                if (danhSachPhieuLuuTru.Any())
                {
                    _context.PHIEU_LUU_TRU.UpdateRange(danhSachPhieuLuuTru);
                }

                var dichVuPhongCu = await _context.CHI_TIET_DICH_VU
                    .Where(dv => dv.PHONGMaPhong == MaPhongCuDoi && dv.ThoiGianSuDung >= mocNhanPhong)
                    .ToListAsync();

                foreach (var dv in dichVuPhongCu)
                {
                    dv.PHONGMaPhong = phongMoi.MaPhong;
                }
                if (dichVuPhongCu.Any())
                {
                    _context.CHI_TIET_DICH_VU.UpdateRange(dichVuPhongCu);
                }

                string logMinibar = "";
                if (ChiPhiMinibarPhongCu > 0)
                {
                    var dvMinibar = await EnsureMinibarServiceItemAsync("DV_MINIBAR_KHAC", "Phụ phí Minibar / Chuyển phòng", 0m);
                    var chiTietMinibar = new CHI_TIET_DICH_VU
                    {
                        MaChiTietDichVu = BaoMatHelper.SinhMaNgauNhien("DV"),
                        DICH_VUMaDichVu = dvMinibar.MaDichVu,
                        DAT_PHONGMaDatPhong = datPhong.MaDatPhong,
                        KHACH_HANGMaKH = datPhong.KHACH_HANGMaKH,
                        PHONGMaPhong = phongMoi.MaPhong,
                        SoLuong = 1,
                        DonGia = ChiPhiMinibarPhongCu,
                        ThanhTien = ChiPhiMinibarPhongCu,
                        ThoiGianSuDung = now
                    };
                    _context.CHI_TIET_DICH_VU.Add(chiTietMinibar);
                    logMinibar = $" - Minibar/Tài sản phòng cũ {phongCu.SoPhong}: {ChiPhiMinibarPhongCu:N0}đ ({GhiChuKiemTraBuongPhong ?? "Buồng phòng đã kiểm"})";
                }

                var trangThaiPhongCu = await EnsureRoomStatusRecordAsync(phongCu);
                trangThaiPhongCu.TrangThaiPhong = "Trống";
                trangThaiPhongCu.TrangThaiVeSinh = "Bẩn";
                trangThaiPhongCu.ThoiGianDonVeSinh = now.AddDays(-1);
                _context.TRANG_THAI_PHONG.Update(trangThaiPhongCu);

                var trangThaiPhongMoi = await EnsureRoomStatusRecordAsync(phongMoi);
                trangThaiPhongMoi.TrangThaiPhong = "Đang ở";
                _context.TRANG_THAI_PHONG.Update(trangThaiPhongMoi);

                string loaiNguyenNhanText = NguyenNhanDoiPhong == "LoiKhachSan"
                    ? "Do lỗi KS (Giữ nguyên giá)"
                    : $"Theo mong muốn KH (Giá mới: {giaChotApDung:N0}đ/đêm, Chênh lệch: {(chenhLech >= 0 ? "+" : "")}{chenhLech:N0}đ - Đại diện ký: {ChuKyDienTuKhachDaiDien})";

                string logDoiPhong = $" | [Đổi phòng {now:dd/MM HH:mm}: {phongCu.SoPhong} -> {phongMoi.SoPhong} ({loaiNguyenNhanText} - Lý do: {LyDoChiTietDoiPhong.Trim()}){logMinibar}]";
                string ghiChuCapNhat = (datPhong.GhiChu ?? "") + logDoiPhong;
                datPhong.GhiChu = ghiChuCapNhat.Length > 255 ? ghiChuCapNhat.Substring(ghiChuCapNhat.Length - 255) : ghiChuCapNhat;

                _context.DAT_PHONG.Update(datPhong);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đổi phòng thành công từ Phòng {phongCu.SoPhong} sang Phòng {phongMoi.SoPhong}! Đã giữ nguyên Phiếu lưu trú, Tiền cọc ({datPhong.TienCoc ?? 0:N0}đ), chuyển tiếp {soKhachDangO} khách đang ở và chuyển Phòng {phongCu.SoPhong} sang trạng thái Trống (Bẩn).";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi thực hiện đổi phòng: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCheckOutAsync(string maPhongCheckOut)
        {
            if (!IsFrontDesk) return Forbid();
            try
            {
                DateTime now = DateTime.Now;

                var datPhongCanCheckOut = await (from ct in _context.CHI_TIET_DAT_PHONG
                                                 join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                 where ct.PHONGMaPhong == maPhongCheckOut
                                                    && dp.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO
                                                 select dp).FirstOrDefaultAsync();

                if (datPhongCanCheckOut != null)
                {
                    var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhongCheckOut);
                    string soPhongText = phong?.SoPhong ?? maPhongCheckOut;
                    string ghiChuHienTai = datPhongCanCheckOut.GhiChu ?? "";

                    // BƯỚC 1: Nếu Buồng phòng CHƯA gửi bản báo cáo tiêu thụ Minibar -> gửi yêu cầu kiểm tra
                    if (!ghiChuHienTai.Contains("[BP_DA_BAO_CAO:"))
                    {
                        if (phong != null)
                        {
                            var trangThai = await EnsureRoomStatusRecordAsync(phong);
                            trangThai.TrangThaiVeSinh = "Bẩn";
                            trangThai.ThoiGianDonVeSinh = now.AddDays(-1);
                            _context.TRANG_THAI_PHONG.Update(trangThai);
                        }

                        if (!ghiChuHienTai.Contains("[CHO_BP_KIEM_TRA]"))
                        {
                            string ghiChuYeuCau = ghiChuHienTai + " [CHO_BP_KIEM_TRA]";
                            datPhongCanCheckOut.GhiChu = ghiChuYeuCau.Length > 255 ? ghiChuYeuCau.Substring(ghiChuYeuCau.Length - 255) : ghiChuYeuCau;
                            _context.DAT_PHONG.Update(datPhongCanCheckOut);
                        }

                        _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                        {
                            MaNhatKy = BaoMatHelper.SinhMaNgauNhien("LG"),
                            TenDangNhap = User.Identity?.Name ?? "Lễ tân",
                            HanhDong = "Yêu cầu Hỗ trợ Checkout",
                            ChiTiet = $"[Nhân viên Buồng phòng] Lễ tân yêu cầu kiểm tra Minibar & gửi bản tiêu thụ cho Phòng {soPhongText} để làm thủ tục Checkout!"
                        });

                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Đã chuyển Phòng {soPhongText} sang trạng thái 'Bẩn' và gửi thông báo đến Nhân viên Buồng phòng! Khi Buồng phòng gửi Bản tiêu thụ (hiện chấm đỏ trên phòng), Lễ tân bấm Checkout để mở màn hình Tất toán hóa đơn.";
                        return RedirectToPage();
                    }

                    // BƯỚC 2: Buồng phòng đã báo cáo -> Chuyển sang màn hình Tất toán Hóa đơn Check-out (lập HOA_DON, THANH_TOAN và tích điểm)
                    return RedirectToPage("/QuanLySoDoPhong/HoaDonCheckout", new { maDatPhong = datPhongCanCheckOut.MaDatPhong });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin lưu trú hợp lệ để thực hiện Checkout.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi Check-out: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}