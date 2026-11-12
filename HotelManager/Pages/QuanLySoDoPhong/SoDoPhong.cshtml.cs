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
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    // Phân quyền truy cập theo chuẩn UC_03, UC_04 & UC_07
    [Authorize(Roles = "Quản lý,Lễ tân,Sales,Nhân viên Buồng phòng,Buồng phòng")]
    public class SoDoPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

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

            // Cờ theo dõi quy trình phối hợp Checkout giữa Lễ tân và Buồng phòng
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

        public bool IsViewingToday { get; set; } = true;
        public bool IsViewingPast { get; set; } = false;
        public DateTime ThoiGianDangXem { get; set; }

        public List<RoomGridViewModel> DanhSachLieuPhong { get; set; } = new List<RoomGridViewModel>();
        public List<AvailableRoomOptionDto> DanhSachPhongTrongDeDoi { get; set; } = new List<AvailableRoomOptionDto>();
        public List<DICH_VU> DanhSachDichVuMinibar { get; set; } = new List<DICH_VU>();

        // =========================================================================
        // HÀM HỖ TRỢ QUAN TRỌNG: Đảm bảo mỗi phòng có 1 bản ghi TRANG_THAI_PHONG riêng
        // Khắc phục triệt để lỗi bấm Checkout nhưng không lưu được trạng thái "Bẩn"
        // =========================================================================
        private async Task<TRANG_THAI_PHONG> EnsureRoomStatusRecordAsync(PHONG phong)
        {
            string maTTRieng = "TTP_" + (phong.SoPhong ?? phong.MaPhong).Trim();
            if (maTTRieng.Length > 20) maTTRieng = maTTRieng.Substring(0, 20);

            var ttRieng = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == maTTRieng);
            if (ttRieng == null)
            {
                // Nếu phòng đang trỏ vào một bản ghi trạng thái chung cũ thì lấy thông tin sang
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

                // Tải danh sách dịch vụ để Buồng phòng chọn khi báo cáo Minibar ở Ô số 2
                DanhSachDichVuMinibar = await _context.DICH_VU.AsNoTracking()
                    .OrderBy(d => d.TenDichVu)
                    .ToListAsync();

                // Đảm bảo mọi phòng đều có bản ghi TRANG_THAI_PHONG riêng biệt trước khi hiển thị
                var danhSachPhongDb = await _context.PHONG.ToListAsync();
                var danhSachTrangThaiDb = await _context.TRANG_THAI_PHONG.ToListAsync();
                bool coKhoiTaoTrangThai = false;

                foreach (var p in danhSachPhongDb)
                {
                    string maTTRieng = "TTP_" + (p.SoPhong ?? p.MaPhong).Trim();
                    if (maTTRieng.Length > 20) maTTRieng = maTTRieng.Substring(0, 20);

                    var tt = danhSachTrangThaiDb.FirstOrDefault(x => x.MaTrangThai == maTTRieng);
                    if (tt == null)
                    {
                        var ttCu = danhSachTrangThaiDb.FirstOrDefault(x => x.MaTrangThai == p.MaTrangThai);
                        tt = new TRANG_THAI_PHONG
                        {
                            MaTrangThai = maTTRieng,
                            TrangThaiPhong = ttCu?.TrangThaiPhong ?? "Trống",
                            TrangThaiVeSinh = ttCu?.TrangThaiVeSinh ?? "Sạch",
                            ThoiGianDonVeSinh = ttCu?.ThoiGianDonVeSinh ?? now
                        };
                        _context.TRANG_THAI_PHONG.Add(tt);
                        danhSachTrangThaiDb.Add(tt);
                        coKhoiTaoTrangThai = true;
                    }
                    else if (!tt.ThoiGianDonVeSinh.HasValue)
                    {
                        tt.ThoiGianDonVeSinh = now;
                        _context.TRANG_THAI_PHONG.Update(tt);
                        coKhoiTaoTrangThai = true;
                    }

                    if (p.MaTrangThai != maTTRieng)
                    {
                        p.MaTrangThai = maTTRieng;
                        _context.PHONG.Update(p);
                        coKhoiTaoTrangThai = true;
                    }
                }

                if (coKhoiTaoTrangThai)
                {
                    await _context.SaveChangesAsync();
                }

                // TRUY VẤN 1: Lấy các đơn đặt phòng đang hiệu lực tại ngày đang xem bằng LINQ
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
                        (IsViewingToday && x.TrangThaiDat == "DANG_O")
                        ||
                        (x.NgayNhan.HasValue && x.NgayTra.HasValue
                         && x.NgayNhan.Value.Date <= targetDate
                         && x.NgayTra.Value.Date >= targetDate
                         && (x.TrangThaiDat == "DANG_O" || x.TrangThaiDat == "DA_DAT" || x.TrangThaiDat == "Chờ xác nhận"))
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

                bool coThayDoiTuDong = false;

                foreach (var p in danhSachPhongDb)
                {
                    var tt = danhSachTrangThaiDb.FirstOrDefault(x => x.MaTrangThai == p.MaTrangThai);

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

                        // Kiểm tra cờ phối hợp Checkout giữa Lễ tân và Buồng phòng trong GhiChu
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

                        // ĐẢM BẢO: Nếu phòng đang chờ Buồng phòng kiểm tra Checkout ([CHO_BP_KIEM_TRA])
                        // mà trạng thái vệ sinh trong DB vẫn đang là "Sạch" (do dữ liệu cũ trước khi sửa) -> Chuyển ngay sang "Bẩn"!
                        if (dangChoBPKiemTra && tt != null)
                        {
                            string vsCheck = (tt.TrangThaiVeSinh ?? "Sạch").Trim();
                            if (vsCheck.Equals("Sạch", StringComparison.OrdinalIgnoreCase))
                            {
                                tt.TrangThaiVeSinh = "Bẩn";
                                _context.TRANG_THAI_PHONG.Update(tt);
                                coThayDoiTuDong = true;
                            }
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
                            string ttVatLy = tt?.TrangThaiPhong?.Trim() ?? "Trống";
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

                            // =========================================================================
                            // QUY TẮC: KHI PHÒNG ĐANG Ở TRẠNG THÁI "SẠCH" VÀ "TRỐNG" (KHÔNG CÓ KHÁCH Ở),
                            // SAU 1 NGÀY MỚI TỰ ĐỘNG CHUYỂN SANG "BẨN" VÀ GỬI THÔNG BÁO CHO BUỒNG PHÒNG
                            // =========================================================================
                            if (IsViewingToday && tt != null)
                            {
                                string vsHienTai = tt.TrangThaiVeSinh?.Trim() ?? "Sạch";
                                int soNgayKhongKhach = tt.ThoiGianDonVeSinh.HasValue
                                    ? (today - tt.ThoiGianDonVeSinh.Value.Date).Days
                                    : 0;

                                if (vsHienTai.Equals("Sạch", StringComparison.OrdinalIgnoreCase) && soNgayKhongKhach >= 1)
                                {
                                    tt.TrangThaiVeSinh = "Bẩn";
                                    _context.TRANG_THAI_PHONG.Update(tt);

                                    string maLogTuDong = "LG" + DateTime.Now.ToString("yyMMddHHmm") + p.SoPhong;
                                    bool daGuiThongBaoHomNay = await _context.NHAT_KY_HE_THONG
                                        .AnyAsync(nk => nk.ChiTiet != null && nk.ChiTiet.Contains($"Phòng {p.SoPhong}") && nk.ChiTiet.Contains(today.ToString("dd/MM/yyyy")));

                                    if (!daGuiThongBaoHomNay)
                                    {
                                        _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                                        {
                                            MaNhatKy = maLogTuDong.Length > 20 ? maLogTuDong.Substring(0, 20) : maLogTuDong,
                                            TenDangNhap = "Hệ thống",
                                            HanhDong = "Thông báo Buồng phòng",
                                            ChiTiet = $"[Nhân viên Buồng phòng] Phòng {p.SoPhong} đang Trống sau 1 ngày ({today:dd/MM/yyyy}) không có khách vào ở, hệ thống tự động chuyển sang trạng thái Bẩn cần dọn lại."
                                        });
                                    }
                                    coThayDoiTuDong = true;
                                }
                            }

                            DanhSachPhongTrongDeDoi.Add(new AvailableRoomOptionDto
                            {
                                MaPhong = p.MaPhong,
                                SoPhong = p.SoPhong ?? p.MaPhong,
                                LoaiPhong = p.LoaiPhong ?? "Standard",
                                SucChuaToiDa = p.SucChuaToiDa ?? 2,
                                GiaTheoNgay = p.GiaTheoNgay ?? 0,
                                TrangThaiVeSinh = tt?.TrangThaiVeSinh?.Trim() ?? "Sạch"
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
                        TrangThaiVeSinh = tt?.TrangThaiVeSinh?.Trim() ?? "Sạch",
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

                    // Phân loại hiển thị trạng thái vệ sinh chuẩn xác theo trạng thái thực trong DB (Không tự ý đè trạng thái khi khách đang ở)
                    string vsLower = vm.TrangThaiVeSinh.ToLower();
                    if (vsLower == "đang dọn dẹp" || vsLower == "đang dọn")
                    {
                        vm.TrangThaiVeSinh = "Đang dọn dẹp";
                        vm.IconVeSinh = "bi-hourglass-split";
                        vm.MauIconVeSinh = "text-info";
                        vm.MauBadgeVeSinh = "bg-info text-dark border-info";
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

                if (coThayDoiTuDong)
                {
                    await _context.SaveChangesAsync();
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

        private async Task<string> SaveIdentityImageAsync(IFormFile? file, string prefix)
        {
            if (file == null || file.Length == 0) return "";

            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "kyc");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            string fileName = $"{prefix}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
            string filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/kyc/{fileName}";
        }

        // =========================================================================
        // CÁC HANDLER DÀNH CHO NHÂN VIÊN BUỒNG PHÒNG (3 Ô TRONG MODAL BUỒNG PHÒNG)
        // =========================================================================

        // Ô 1: Chuyển trạng thái vệ sinh phòng theo chu trình: Bẩn -> Đang dọn dẹp -> Sạch
        public async Task<IActionResult> OnPostChuyenTrangThaiBuongPhongAsync()
        {
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == MaPhongBuongPhong);
                if (phong == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy phòng cần chuyển trạng thái!";
                    return RedirectToPage();
                }

                // Đảm bảo lấy đúng bản ghi TRANG_THAI_PHONG riêng của phòng này
                var trangThai = await EnsureRoomStatusRecordAsync(phong);
                string vsHienTai = (trangThai.TrangThaiVeSinh ?? "Bẩn").Trim();

                // Nếu đang ở trạng thái Bẩn -> Chuyển sang "Đang dọn dẹp"
                if (vsHienTai.Contains("Bẩn", StringComparison.OrdinalIgnoreCase))
                {
                    trangThai.TrangThaiVeSinh = "Đang dọn dẹp";
                    _context.TRANG_THAI_PHONG.Update(trangThai);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Phòng {phong.SoPhong} đã chuyển sang trạng thái 'Đang dọn dẹp'. Sau khi dọn xong, hãy ấn thêm 1 lần nữa để chuyển sang 'Sạch'!";
                }
                // Nếu đang ở trạng thái "Đang dọn dẹp" -> Chuyển sang "Sạch" và đặt lại mốc thời gian dọn vệ sinh = Hiện tại
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
                    TempData["ErrorMessage"] = $"Phòng {phong.SoPhong} hiện đang ở trạng thái Sạch, không cần chuyển trạng thái.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi chuyển trạng thái phòng: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Ô 2: Tạo phiếu báo cáo Minibar hoặc hỏng hóc trong phòng gửi cho Lễ tân khi Checkout
        public async Task<IActionResult> OnPostBaoCaoMinibarCheckoutAsync()
        {
            try
            {
                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == MaPhongBuongPhong);
                if (phong == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin phòng!";
                    return RedirectToPage();
                }

                // Tìm đơn đặt phòng đang ở (DANG_O) của phòng này
                var datPhong = await (from ct in _context.CHI_TIET_DAT_PHONG
                                      join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                      where ct.PHONGMaPhong == MaPhongBuongPhong && dp.TrangThaiDatPhong == "DANG_O"
                                      select dp).FirstOrDefaultAsync();

                DateTime now = DateTime.Now;
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                string chiTietMinibarText = "Không dùng Minibar";

                // 1. Nếu có chọn mặt hàng Minibar tiêu thụ -> Lưu vào CHI_TIET_DICH_VU
                if (!string.IsNullOrWhiteSpace(MaDichVuMinibarBP) && SoLuongMinibarBP > 0)
                {
                    var dichVu = await _context.DICH_VU.AsNoTracking().FirstOrDefaultAsync(d => d.MaDichVu == MaDichVuMinibarBP);
                    if (dichVu != null)
                    {
                        decimal thanhTienMB = (dichVu.DonGia ?? 0) * SoLuongMinibarBP;
                        var ctDichVu = new CHI_TIET_DICH_VU
                        {
                            MaChiTietDichVu = $"DV{now:yyMMddHHmmss}{suffix}",
                            DICH_VUMaDichVu = dichVu.MaDichVu,
                            PHONGMaPhong = phong.MaPhong,
                            KHACH_HANGMaKH = datPhong?.KHACH_HANGMaKH,
                            SoLuong = SoLuongMinibarBP,
                            DonGia = dichVu.DonGia,
                            ThanhTien = thanhTienMB,
                            ThoiGianSuDung = now
                        };
                        _context.CHI_TIET_DICH_VU.Add(ctDichVu);
                        chiTietMinibarText = $"Minibar: {SoLuongMinibarBP}x {dichVu.TenDichVu} ({thanhTienMB:N0}đ)";
                    }
                }

                // 2. Nếu có phí đền bù hỏng hóc trong phòng -> Lưu vào CHI_TIET_DICH_VU
                string chiTietHongHocText = "Không hỏng hóc";
                if (ChiPhiHongHocBP > 0)
                {
                    var dvMacDinh = await _context.DICH_VU.AsNoTracking().FirstOrDefaultAsync();
                    if (dvMacDinh != null)
                    {
                        var ctDenBu = new CHI_TIET_DICH_VU
                        {
                            MaChiTietDichVu = $"HH{now:yyMMddHHmmss}{suffix}",
                            DICH_VUMaDichVu = dvMacDinh.MaDichVu,
                            PHONGMaPhong = phong.MaPhong,
                            KHACH_HANGMaKH = datPhong?.KHACH_HANGMaKH,
                            SoLuong = 1,
                            DonGia = ChiPhiHongHocBP,
                            ThanhTien = ChiPhiHongHocBP,
                            ThoiGianSuDung = now
                        };
                        _context.CHI_TIET_DICH_VU.Add(ctDenBu);
                    }
                    chiTietHongHocText = $"Phí hỏng hóc: {ChiPhiHongHocBP:N0}đ";
                }

                string ghiChuThem = string.IsNullOrWhiteSpace(GhiChuKiemTraCheckoutBP) ? "Đủ đồ dùng" : GhiChuKiemTraCheckoutBP.Trim();
                string tomTatBaoCao = $"{chiTietMinibarText} | {chiTietHongHocText} | Ghi chú: {ghiChuThem}";

                // 3. Đánh dấu vào đơn đặt phòng là Buồng phòng đã kiểm tra xong để Lễ tân xác nhận Checkout
                if (datPhong != null)
                {
                    string ghiChuCu = (datPhong.GhiChu ?? "").Replace("[CHO_BP_KIEM_TRA]", "");
                    string ghiChuMoi = $"{ghiChuCu} [BP_DA_BAO_CAO: {tomTatBaoCao}]";
                    datPhong.GhiChu = ghiChuMoi.Length > 255 ? ghiChuMoi.Substring(ghiChuMoi.Length - 255) : ghiChuMoi;
                    _context.DAT_PHONG.Update(datPhong);
                }

                // 4. Gửi thông báo đến Lễ tân qua NHAT_KY_HE_THONG
                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = $"LG{now:yyMMddHHmmss}{suffix}",
                    TenDangNhap = User.Identity?.Name ?? "Buồng phòng",
                    HanhDong = "Thông báo Lễ tân",
                    ChiTiet = $"[Lễ tân] Buồng phòng đã kiểm tra Phòng {phong.SoPhong} để Checkout: {tomTatBaoCao}. Lễ tân vui lòng xác nhận!"
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã gửi phiếu báo cáo kiểm tra Minibar & Hỏng hóc của Phòng {phong.SoPhong} cho Lễ tân xác nhận Checkout!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gửi báo cáo Minibar/Hỏng hóc: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Ô 3: Tạo phiếu báo cáo kỹ thuật gửi cho Nhân viên Kỹ thuật (UC_07 - PHIEU_BAO_CAO_SU_CO)
        public async Task<IActionResult> OnPostTaoPhieuKyThuatAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MaPhongBuongPhong) || string.IsNullOrWhiteSpace(MoTaSuCoKT))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập nội dung mô tả sự cố kỹ thuật!";
                    return RedirectToPage();
                }

                var phong = await _context.PHONG.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhong == MaPhongBuongPhong);
                string soPhongText = phong?.SoPhong ?? MaPhongBuongPhong;

                DateTime now = DateTime.Now;
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

                string anhSuCoUrl = await SaveIdentityImageAsync(AnhSuCoKT, $"SUCO_{soPhongText}");

                var tk = await _context.TAI_KHOAN.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == (User.Identity != null ? User.Identity.Name : ""));

                var phieuSuCo = new PHIEU_BAO_CAO_SU_CO
                {
                    MaPhieuBaoCao = $"SC{now:yyMMddHHmmss}{suffix}",
                    NHAN_VIENMaNV = tk?.NHAN_VIENMaNV,
                    MoTaSuCo = $"[Phòng {soPhongText}] {MoTaSuCoKT.Trim()}",
                    MucDoUuTien = string.IsNullOrWhiteSpace(MucDoUuTienKT) ? "Trung bình" : MucDoUuTienKT,
                    HinhAnhDinhKem = anhSuCoUrl,
                    ThoiGianTao = now
                };
                _context.PHIEU_BAO_CAO_SU_CO.Add(phieuSuCo);

                _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                {
                    MaNhatKy = $"LG{now:yyMMddHHmmss}{suffix}",
                    TenDangNhap = User.Identity?.Name ?? "Buồng phòng",
                    HanhDong = "Yêu cầu Kỹ thuật",
                    ChiTiet = $"[Nhân viên Kỹ thuật] Báo cáo sự cố tại Phòng {soPhongText} (Ưu tiên: {phieuSuCo.MucDoUuTien}): {MoTaSuCoKT.Trim()}"
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo Phiếu báo cáo kỹ thuật ({phieuSuCo.MaPhieuBaoCao}) cho Phòng {soPhongText} và gửi thông báo đến Nhân viên Kỹ thuật!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tạo phiếu kỹ thuật: {ex.Message}";
            }

            return RedirectToPage();
        }

        // =========================================================================
        // CÁC HANDLER HIỆN TẠI CỦA LỄ TÂN (GIỮ NGUYÊN TOÀN BỘ NGHIỆP VỤ)
        // =========================================================================

        public async Task<IActionResult> OnPostDoiTrangThaiAsync(string maPhong, string trangThaiMoi, string loaiCapNhat)
        {
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
            try
            {
                DateTime now = DateTime.Now;
                DateTime today = now.Date;

                var datPhongCanCheckIn = await (from ct in _context.CHI_TIET_DAT_PHONG
                                                join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                where ct.PHONGMaPhong == maPhongCheckIn
                                                   && (dp.TrangThaiDatPhong == "DA_DAT" || dp.TrangThaiDatPhong == "Chờ xác nhận")
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
                    if (!string.IsNullOrWhiteSpace(soGiayToCheckIn))
                    {
                        khachDaiDien.SoCCCD = soGiayToCheckIn.Trim().Length > 12
                            ? soGiayToCheckIn.Trim().Substring(0, 12)
                            : soGiayToCheckIn.Trim();
                    }

                    string urlTruoc = await SaveIdentityImageAsync(anhMatTruocCheckIn, $"CI1_F_{khachDaiDien.MaKH}");
                    string urlSau = await SaveIdentityImageAsync(anhMatSauCheckIn, $"CI1_B_{khachDaiDien.MaKH}");
                    string loaiGT = string.IsNullOrWhiteSpace(loaiGiayToCheckIn) ? "CCCD (Việt Nam)" : loaiGiayToCheckIn;

                    if (!string.IsNullOrEmpty(urlTruoc) || !string.IsNullOrEmpty(urlSau))
                    {
                        khachDaiDien.GhiChuDacBiet = $"KYC|{loaiGT}|{urlTruoc}|{urlSau}";
                    }
                    _context.KHACH_HANG.Update(khachDaiDien);
                }

                datPhongCanCheckIn.TrangThaiDatPhong = "DANG_O";
                datPhongCanCheckIn.NgayNhan = now;
                _context.DAT_PHONG.Update(datPhongCanCheckIn);

                var phieuLuuTruDauTien = new PHIEU_LUU_TRU
                {
                    MaPhieuLuuTru = "PLT" + now.ToString("yyMMddHHmmss"),
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
            try
            {
                if (string.IsNullOrWhiteSpace(maPhongWalkIn) || string.IsNullOrWhiteSpace(hoTenWalkIn) || string.IsNullOrWhiteSpace(sdtWalkIn) || string.IsNullOrWhiteSpace(cccdWalkIn))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Họ tên, SĐT và Số CCCD/Hộ chiếu của khách!";
                    return RedirectToPage();
                }

                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhongWalkIn);
                if (phong == null) return RedirectToPage();

                DateTime now = DateTime.Now;
                DateTime today = now.Date;
                if (soNgayOWalkIn <= 0) soNgayOWalkIn = 1;
                if (soKhachWalkIn <= 0) soKhachWalkIn = 1;

                DateTime ngayTraDuKien = today.AddDays(soNgayOWalkIn).AddHours(12);

                string urlTruoc = await SaveIdentityImageAsync(anhMatTruocWalkIn, "WI_F");
                string urlSau = await SaveIdentityImageAsync(anhMatSauWalkIn, "WI_B");
                string loaiGT = string.IsNullOrWhiteSpace(loaiGiayToWalkIn) ? "CCCD (Việt Nam)" : loaiGiayToWalkIn;
                string kycMetadata = $"KYC|{loaiGT}|{urlTruoc}|{urlSau}";

                string soGiayToChuan = cccdWalkIn.Trim().Length > 12 ? cccdWalkIn.Trim().Substring(0, 12) : cccdWalkIn.Trim();

                var khachHang = await _context.KHACH_HANG.FirstOrDefaultAsync(k => k.SDT == sdtWalkIn.Trim());
                if (khachHang == null)
                {
                    khachHang = new KHACH_HANG
                    {
                        MaKH = "KH" + now.ToString("yyMMddHHmmss"),
                        HoTen = hoTenWalkIn.Trim(),
                        SDT = sdtWalkIn.Trim(),
                        SoCCCD = soGiayToChuan,
                        HangThanhVien = "Thường",
                        DiemThuong = 0,
                        GhiChuDacBiet = kycMetadata
                    };
                    _context.KHACH_HANG.Add(khachHang);
                }
                else
                {
                    khachHang.HoTen = hoTenWalkIn.Trim();
                    khachHang.SoCCCD = soGiayToChuan;
                    khachHang.GhiChuDacBiet = kycMetadata;
                    _context.KHACH_HANG.Update(khachHang);
                }

                string maDatPhongMoi = "DP" + now.ToString("yyyyMMddHHmmss");
                var datPhongWalkIn = new DAT_PHONG
                {
                    MaDatPhong = maDatPhongMoi,
                    KHACH_HANGMaKH = khachHang.MaKH,
                    NgayDat = now,
                    NgayNhan = now,
                    NgayTra = ngayTraDuKien,
                    SoLuongKhach = soKhachWalkIn,
                    TienCoc = 0,
                    TrangThaiDatPhong = "DANG_O",
                    KenhDatPhong = "Walk-in tại quầy",
                    GhiChu = string.IsNullOrWhiteSpace(ghiChuWalkIn) ? "Khách vãng lai nhận phòng trực tiếp" : ghiChuWalkIn.Trim()
                };
                _context.DAT_PHONG.Add(datPhongWalkIn);

                var chiTietWalkIn = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = "CT" + maDatPhongMoi,
                    DAT_PHONGMaDatPhong = maDatPhongMoi,
                    PHONGMaPhong = phong.MaPhong,
                    DonGia = phong.GiaTheoNgay,
                    SoNgayO = soNgayOWalkIn
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTietWalkIn);

                var phieuLuuTru = new PHIEU_LUU_TRU
                {
                    MaPhieuLuuTru = "PLT" + now.ToString("yyMMddHHmmss"),
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
            try
            {
                if (string.IsNullOrWhiteSpace(MaPhongOChung))
                {
                    TempData["ErrorMessage"] = "Không xác định được mã phòng cần bổ sung khách!";
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
                    if (danhSachHopLe[i].AnhMatTruoc == null || danhSachHopLe[i].AnhMatSau == null)
                    {
                        TempData["ErrorMessage"] = $"Khách ở cùng thứ {i + 1} ({danhSachHopLe[i].HoTen}) chưa có đủ 2 ảnh Mặt trước & Mặt sau của CCCD/Hộ chiếu!";
                        return RedirectToPage();
                    }
                }

                DateTime now = DateTime.Now;
                string timeStampBase = now.ToString("yyMMddHHmmss");
                var listKhachMoi = new List<KHACH_HANG>();
                var listPhieuMoi = new List<PHIEU_LUU_TRU>();

                for (int i = 0; i < danhSachHopLe.Count; i++)
                {
                    var item = danhSachHopLe[i];
                    string suffix = $"{timeStampBase}{i + 1:D2}";

                    string urlTruoc = await SaveIdentityImageAsync(item.AnhMatTruoc, $"COMP_F_{i + 1}");
                    string urlSau = await SaveIdentityImageAsync(item.AnhMatSau, $"COMP_B_{i + 1}");
                    string loaiGT = string.IsNullOrWhiteSpace(item.LoaiGiayTo) ? "CCCD (Việt Nam)" : item.LoaiGiayTo;
                    string kycMetadata = $"KYC|{loaiGT}|{urlTruoc}|{urlSau}";

                    string soGiayToChuan = item.SoGiayTo.Trim().Length > 12
                        ? item.SoGiayTo.Trim().Substring(0, 12)
                        : item.SoGiayTo.Trim();

                    var khachDiCung = new KHACH_HANG
                    {
                        MaKH = "KH" + suffix,
                        HoTen = item.HoTen.Trim(),
                        SoCCCD = soGiayToChuan,
                        SDT = string.IsNullOrWhiteSpace(item.SDT) ? "0000000000" : item.SDT.Trim(),
                        HangThanhVien = "Thường",
                        DiemThuong = 0,
                        GhiChuDacBiet = kycMetadata
                    };
                    listKhachMoi.Add(khachDiCung);

                    var phieuLuuTruMoi = new PHIEU_LUU_TRU
                    {
                        MaPhieuLuuTru = "PLT" + suffix,
                        PHONGMaPhong = MaPhongOChung,
                        KHACH_HANGMaKH = khachDiCung.MaKH,
                        ThoiGianCheckIn = now,
                        TrangThaiLuuTru = "DANG_LUU_TRU"
                    };
                    listPhieuMoi.Add(phieuLuuTruMoi);
                }

                _context.KHACH_HANG.AddRange(listKhachMoi);
                _context.PHIEU_LUU_TRU.AddRange(listPhieuMoi);
                await _context.SaveChangesAsync();

                var datPhongHienTai = await (from ct in _context.CHI_TIET_DAT_PHONG
                                             join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                             where ct.PHONGMaPhong == MaPhongOChung && dp.TrangThaiDatPhong == "DANG_O"
                                             select dp).FirstOrDefaultAsync();

                int soDaCheckIn = await _context.PHIEU_LUU_TRU
                    .CountAsync(p => p.PHONGMaPhong == MaPhongOChung && (p.TrangThaiLuuTru == "DANG_LUU_TRU" || p.TrangThaiLuuTru == "DA_ROI_DI"));

                int tongDangKy = datPhongHienTai?.SoLuongKhach ?? 1;

                if (datPhongHienTai != null && soDaCheckIn > tongDangKy)
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
                    var dvMinibar = await _context.DICH_VU.FirstOrDefaultAsync();
                    if (dvMinibar != null)
                    {
                        var chiTietMinibar = new CHI_TIET_DICH_VU
                        {
                            MaChiTietDichVu = "CTDV" + now.ToString("yyMMddHHmmss"),
                            DICH_VUMaDichVu = dvMinibar.MaDichVu,
                            KHACH_HANGMaKH = datPhong.KHACH_HANGMaKH,
                            PHONGMaPhong = phongMoi.MaPhong,
                            SoLuong = 1,
                            DonGia = ChiPhiMinibarPhongCu,
                            ThanhTien = ChiPhiMinibarPhongCu,
                            ThoiGianSuDung = now
                        };
                        _context.CHI_TIET_DICH_VU.Add(chiTietMinibar);
                    }
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

        // Quy trình Checkout phối hợp 2 bước:
        // - Bước 1: Khi Lễ tân ấn Checkout -> Lập tức chuyển trạng thái vệ sinh của phòng sang "Bẩn" (nhờ EnsureRoomStatusRecordAsync không bao giờ bị null) và gửi thông báo cho Buồng phòng kiểm tra Minibar/Hỏng hóc.
        // - Bước 2: Sau khi Buồng phòng đã gửi báo cáo ([BP_DA_BAO_CAO:...]), Lễ tân ấn xác nhận thì khách mới chính thức được Checkout.
        public async Task<IActionResult> OnPostCheckOutAsync(string maPhongCheckOut)
        {
            try
            {
                DateTime now = DateTime.Now;

                var datPhongCanCheckOut = await (from ct in _context.CHI_TIET_DAT_PHONG
                                                 join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                 where ct.PHONGMaPhong == maPhongCheckOut
                                                    && dp.TrangThaiDatPhong == "DANG_O"
                                                 select dp).FirstOrDefaultAsync();

                if (datPhongCanCheckOut != null)
                {
                    var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhongCheckOut);
                    string soPhongText = phong?.SoPhong ?? maPhongCheckOut;
                    string ghiChuHienTai = datPhongCanCheckOut.GhiChu ?? "";

                    // KIỂM TRA: Nếu Buồng phòng CHƯA gửi phiếu báo cáo Minibar / Hỏng hóc
                    if (!ghiChuHienTai.Contains("[BP_DA_BAO_CAO:"))
                    {
                        // 1. Chuyển ngay trạng thái vệ sinh của phòng sang "Bẩn" (Đảm bảo có bản ghi TRANG_THAI_PHONG riêng)
                        if (phong != null)
                        {
                            var trangThai = await EnsureRoomStatusRecordAsync(phong);
                            trangThai.TrangThaiVeSinh = "Bẩn";
                            trangThai.ThoiGianDonVeSinh = now.AddDays(-1);
                            _context.TRANG_THAI_PHONG.Update(trangThai);
                        }

                        // 2. Đánh dấu đang chờ Buồng phòng kiểm tra và gửi thông báo cho Nhân viên Buồng phòng
                        if (!ghiChuHienTai.Contains("[CHO_BP_KIEM_TRA]"))
                        {
                            string ghiChuYeuCau = ghiChuHienTai + " [CHO_BP_KIEM_TRA]";
                            datPhongCanCheckOut.GhiChu = ghiChuYeuCau.Length > 255 ? ghiChuYeuCau.Substring(ghiChuYeuCau.Length - 255) : ghiChuYeuCau;
                            _context.DAT_PHONG.Update(datPhongCanCheckOut);
                        }

                        string suffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                        _context.NHAT_KY_HE_THONG.Add(new NHAT_KY_HE_THONG
                        {
                            MaNhatKy = $"LG{now:yyMMddHHmmss}{suffix}",
                            TenDangNhap = User.Identity?.Name ?? "Lễ tân",
                            HanhDong = "Yêu cầu Hỗ trợ Checkout",
                            ChiTiet = $"[Nhân viên Buồng phòng] Lễ tân yêu cầu kiểm tra Minibar & hỏng hóc cho Phòng {soPhongText} (Đã chuyển sang trạng thái Bẩn) để làm thủ tục Checkout!"
                        });

                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Đã chuyển Phòng {soPhongText} sang trạng thái 'Bẩn' và gửi thông báo đến Nhân viên Buồng phòng! Khi Buồng phòng gửi báo cáo kiểm tra Minibar/hỏng hóc, Lễ tân xác nhận lại một lần nữa để hoàn tất Checkout cho khách.";
                        return RedirectToPage();
                    }

                    // NẾU BUỒNG PHÒNG ĐÃ GỬI BÁO CÁO -> LỄ TÂN XÁC NHẬN HOÀN TẤT CHECKOUT CHO KHÁCH
                    datPhongCanCheckOut.TrangThaiDatPhong = "Đã check-out";
                    datPhongCanCheckOut.NgayTra = now;
                    _context.DAT_PHONG.Update(datPhongCanCheckOut);

                    var danhSachPhieuLuuTru = await _context.PHIEU_LUU_TRU
                        .Where(p => p.PHONGMaPhong == maPhongCheckOut && p.TrangThaiLuuTru == "DANG_LUU_TRU")
                        .ToListAsync();

                    foreach (var phieu in danhSachPhieuLuuTru)
                    {
                        phieu.ThoiGianCheckOut = now;
                        phieu.TrangThaiLuuTru = "DA_CHECK_OUT";
                    }
                    if (danhSachPhieuLuuTru.Any())
                    {
                        _context.PHIEU_LUU_TRU.UpdateRange(danhSachPhieuLuuTru);
                    }

                    if (phong != null)
                    {
                        var trangThai = await EnsureRoomStatusRecordAsync(phong);
                        trangThai.TrangThaiPhong = "Trống";

                        // Nếu trong lúc kiểm tra Checkout mà Buồng phòng chưa dọn xong thành Sạch thì giữ trạng thái Bẩn để Buồng phòng dọn
                        if (!string.Equals(trangThai.TrangThaiVeSinh?.Trim(), "Đang dọn dẹp", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(trangThai.TrangThaiVeSinh?.Trim(), "Sạch", StringComparison.OrdinalIgnoreCase))
                        {
                            trangThai.TrangThaiVeSinh = "Bẩn";
                            trangThai.ThoiGianDonVeSinh = now.AddDays(-1);
                        }
                        _context.TRANG_THAI_PHONG.Update(trangThai);
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Lễ tân đã xác nhận báo cáo của Buồng phòng và hoàn tất Checkout cho Phòng {soPhongText}! Đã cập nhật số liệu sang Tổng quan Tiền sảnh.";
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