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
    // Phân quyền truy cập theo chuẩn UC_03 & UC_04
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

        // Class phụ trợ lưu thông tin từng hành khách lưu trú trong phòng (Dùng để serialize sang JSON cho Modal)
        public class OccupantInfoDto
        {
            public string MaKH { get; set; } = string.Empty;
            public string HoTen { get; set; } = string.Empty;
            public string SoGiayTo { get; set; } = string.Empty;
            public string LoaiGiayTo { get; set; } = "CCCD (Việt Nam)";
            public string SDT { get; set; } = string.Empty;
            public string ThoiGianCheckIn { get; set; } = string.Empty;
            public string AnhMatTruoc { get; set; } = string.Empty;
            public string AnhMatSau { get; set; } = string.Empty;
            public bool LaNguoiDaiDien { get; set; } = false;
        }

        // ViewModel hiển thị thông tin từng ô phòng trên lưới
        public class RoomGridViewModel
        {
            public string MaPhong { get; set; } = default!;
            public string? SoPhong { get; set; }
            public string? LoaiPhong { get; set; }
            public int? SoGiuong { get; set; }
            public int? SucChuaToiDa { get; set; }

            public string? TrangThaiPhong { get; set; }
            public string? TrangThaiVeSinh { get; set; }

            // Thuộc tính phục vụ UI Buồng phòng
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

            // === THUỘC TÍNH QUẢN LÝ SỐ LƯỢNG KHÁCH CHECK-IN TỪNG PHẦN (UC_03 + UC_04) ===
            public int TongSoKhachDangKy { get; set; } = 1;
            public int SoKhachDaCheckInThucTe { get; set; } = 0;
            public bool DaCheckInDuKhach => SoKhachDaCheckInThucTe >= TongSoKhachDangKy;
            public string DanhSachKhachJson { get; set; } = "[]";

            // === CÁC CỜ (FLAGS) KIỂM SOÁT NGHIỆP VỤ THỜI GIAN THỰC ===
            public bool IsCheckInValid { get; set; } = false;
            public bool IsCheckOutValid { get; set; } = false;
            public bool IsBookingValid { get; set; } = false;
            public bool IsWalkInCheckInValid { get; set; } = false;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? ThoiGianLoc { get; set; }

        public bool IsViewingToday { get; set; } = true;
        public bool IsViewingPast { get; set; } = false;
        public DateTime ThoiGianDangXem { get; set; }

        public List<RoomGridViewModel> DanhSachLieuPhong { get; set; } = new List<RoomGridViewModel>();

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

                // TRUY VẤN 1: Lấy các đơn đặt phòng đang hiệu lực tại ngày đang xem
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
                    .Where(x => x.NgayNhan.HasValue && x.NgayTra.HasValue
                             && x.NgayNhan.Value.Date <= targetDate
                             && x.NgayTra.Value.Date >= targetDate
                             && (x.TrangThaiDat == "DANG_O" || x.TrangThaiDat == "DA_DAT" || x.TrangThaiDat == "Chờ xác nhận"))
                    .ToListAsync();

                // TRUY VẤN 2: Lấy danh sách khách đang lưu trú thực tế từ bảng PHIEU_LUU_TRU kết hợp KHACH_HANG
                var activeStays = await (from plt in _context.PHIEU_LUU_TRU.AsNoTracking()
                                         join kh in _context.KHACH_HANG.AsNoTracking() on plt.KHACH_HANGMaKH equals kh.MaKH
                                         where plt.TrangThaiLuuTru == "DANG_LUU_TRU"
                                         select new
                                         {
                                             plt.MaPhieuLuuTru,
                                             plt.PHONGMaPhong,
                                             plt.ThoiGianCheckIn,
                                             kh.MaKH,
                                             kh.HoTen,
                                             kh.SoCCCD,
                                             kh.SDT,
                                             kh.GhiChuDacBiet
                                         }).ToListAsync();

                // TRUY VẤN 3: Lấy danh sách Phòng và Trạng thái vật lý
                var query = await (from p in _context.PHONG.AsNoTracking()
                                   join t in _context.TRANG_THAI_PHONG.AsNoTracking() on p.MaTrangThai equals t.MaTrangThai into pt
                                   from t in pt.DefaultIfEmpty()
                                   select new { p, t }).ToListAsync();

                foreach (var item in query)
                {
                    var bookingTaiThoiDiem = activeBookings
                        .OrderByDescending(b => b.TrangThaiDat == "DANG_O")
                        .FirstOrDefault(b => b.MaPhong == item.p.MaPhong);

                    string trangThaiTinhToan = "Trống";
                    string? maDatPhong = null;
                    string? tenKhach = null;
                    string? sdtKhach = null;
                    string? ghiChu = null;

                    int tongKhachDangKy = 1;
                    int soKhachDaCheckIn = 0;
                    var danhSachKhachTrongPhong = new List<OccupantInfoDto>();

                    bool hopLeCheckIn = false;
                    bool hopLeCheckOut = false;
                    bool hopLeDatPhong = false;
                    bool hopLeWalkIn = false;

                    if (bookingTaiThoiDiem != null)
                    {
                        maDatPhong = bookingTaiThoiDiem.MaDatPhong;
                        tenKhach = bookingTaiThoiDiem.TenKhachHang;
                        sdtKhach = bookingTaiThoiDiem.SDTKhachHang;
                        ghiChu = bookingTaiThoiDiem.GhiChu;
                        tongKhachDangKy = (bookingTaiThoiDiem.SoLuongKhach.HasValue && bookingTaiThoiDiem.SoLuongKhach.Value > 0)
                                          ? bookingTaiThoiDiem.SoLuongKhach.Value : 1;

                        if (bookingTaiThoiDiem.TrangThaiDat == "DANG_O")
                        {
                            trangThaiTinhToan = "Đang ở";
                            if (IsViewingToday)
                            {
                                hopLeCheckOut = true;
                            }

                            // Lọc danh sách các khách đã thực sự làm thủ tục Check-in vào phòng này trong bảng PHIEU_LUU_TRU
                            var staysInRoom = activeStays
                                .Where(s => s.PHONGMaPhong == item.p.MaPhong)
                                .OrderBy(s => s.ThoiGianCheckIn)
                                .ToList();

                            foreach (var s in staysInRoom)
                            {
                                ParseKycInfo(s.GhiChuDacBiet, out string loaiGiayTo, out string anhTruoc, out string anhSau);
                                danhSachKhachTrongPhong.Add(new OccupantInfoDto
                                {
                                    MaKH = s.MaKH,
                                    HoTen = s.HoTen ?? "Khách lưu trú",
                                    SoGiayTo = s.SoCCCD ?? "Chưa cập nhật",
                                    LoaiGiayTo = loaiGiayTo,
                                    SDT = s.SDT ?? "Đi cùng",
                                    ThoiGianCheckIn = s.ThoiGianCheckIn.HasValue ? s.ThoiGianCheckIn.Value.ToString("dd/MM/yyyy HH:mm") : "",
                                    AnhMatTruoc = anhTruoc,
                                    AnhMatSau = anhSau,
                                    LaNguoiDaiDien = (s.MaKH == bookingTaiThoiDiem.MaKHDaiDien)
                                });
                            }

                            // Nếu phòng Đang ở từ dữ liệu cũ chưa có bản ghi trong PHIEU_LUU_TRU, tự động lấy người đại diện làm người đầu tiên (1 người)
                            if (danhSachKhachTrongPhong.Count == 0)
                            {
                                ParseKycInfo(bookingTaiThoiDiem.GhiChuDacBietDaiDien, out string loaiGiayTo, out string anhTruoc, out string anhSau);
                                danhSachKhachTrongPhong.Add(new OccupantInfoDto
                                {
                                    MaKH = bookingTaiThoiDiem.MaKHDaiDien,
                                    HoTen = bookingTaiThoiDiem.TenKhachHang ?? "Khách đại diện",
                                    SoGiayTo = bookingTaiThoiDiem.SoCCCDDaiDien ?? "Chưa cập nhật",
                                    LoaiGiayTo = loaiGiayTo,
                                    SDT = bookingTaiThoiDiem.SDTKhachHang ?? "",
                                    ThoiGianCheckIn = bookingTaiThoiDiem.NgayNhan.HasValue ? bookingTaiThoiDiem.NgayNhan.Value.ToString("dd/MM/yyyy HH:mm") : "",
                                    AnhMatTruoc = anhTruoc,
                                    AnhMatSau = anhSau,
                                    LaNguoiDaiDien = true
                                });
                            }

                            soKhachDaCheckIn = danhSachKhachTrongPhong.Count;
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
                            string ttVatLy = item.t?.TrangThaiPhong?.Trim() ?? "Trống";
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
                        }
                    }

                    var vm = new RoomGridViewModel
                    {
                        MaPhong = item.p.MaPhong,
                        SoPhong = item.p.SoPhong,
                        LoaiPhong = item.p.LoaiPhong,
                        SoGiuong = item.p.SoGiuong,
                        SucChuaToiDa = item.p.SucChuaToiDa,
                        TrangThaiPhong = trangThaiTinhToan,
                        TrangThaiVeSinh = item.t?.TrangThaiVeSinh?.Trim() ?? "Sạch",
                        MaDatPhongHienTai = maDatPhong,
                        TenKhachHang = tenKhach,
                        SDTKhachHang = sdtKhach,
                        GhiChuKhachHang = ghiChu,
                        NgayNhan = bookingTaiThoiDiem?.NgayNhan,
                        NgayTra = bookingTaiThoiDiem?.NgayTra,
                        TongSoKhachDangKy = tongKhachDangKy,
                        SoKhachDaCheckInThucTe = soKhachDaCheckIn,
                        DanhSachKhachJson = JsonSerializer.Serialize(danhSachKhachTrongPhong),
                        IsCheckInValid = hopLeCheckIn,
                        IsCheckOutValid = hopLeCheckOut,
                        IsBookingValid = hopLeDatPhong,
                        IsWalkInCheckInValid = hopLeWalkIn
                    };

                    // Tính độ bẩn của phòng theo ngày cho Buồng phòng
                    if (item.t != null && item.t.ThoiGianDonVeSinh.HasValue)
                    {
                        vm.SoNgayChuaDon = (DateTime.Now.Date - item.t.ThoiGianDonVeSinh.Value.Date).Days;
                    }
                    else
                    {
                        vm.SoNgayChuaDon = 0;
                    }

                    if (vm.TrangThaiVeSinh.ToLower() == "sạch" && vm.SoNgayChuaDon == 0)
                    {
                        vm.IconVeSinh = "bi-stars";
                        vm.MauIconVeSinh = "text-success";
                    }
                    else if (vm.SoNgayChuaDon == 1)
                    {
                        vm.TrangThaiVeSinh = "Bẩn nhẹ";
                        vm.IconVeSinh = "bi-droplet-half";
                        vm.MauIconVeSinh = "text-warning";
                    }
                    else if (vm.SoNgayChuaDon >= 2)
                    {
                        vm.TrangThaiVeSinh = "Bẩn nặng (Cần dọn gấp)";
                        vm.IconVeSinh = "bi-exclamation-triangle-fill";
                        vm.MauIconVeSinh = "text-danger";
                    }
                    else
                    {
                        vm.IconVeSinh = "bi-brush-fill";
                        vm.MauIconVeSinh = "text-secondary";
                    }

                    DanhSachLieuPhong.Add(vm);
                }

                DanhSachLieuPhong = DanhSachLieuPhong.OrderBy(x => x.SoPhong).ToList();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tải sơ đồ phòng: {ex.Message}");
                return Page();
            }
        }

        // Hàm bóc tách thông tin Loại giấy tờ và Ảnh Mặt trước / Mặt sau từ cột GhiChuDacBiet
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

        // Hàm lưu file ảnh CCCD/Hộ chiếu vào thư mục wwwroot/uploads/kyc
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

            string fileName = $"{prefix}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
            string filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/kyc/{fileName}";
        }

        // Cập nhật trạng thái vệ sinh (Dành cho Buồng phòng)
        public async Task<IActionResult> OnPostDoiTrangThaiAsync(string maPhong, string trangThaiMoi, string loaiCapNhat)
        {
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhong);
                if (phong != null)
                {
                    var trangThai = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                    if (trangThai != null)
                    {
                        if (loaiCapNhat == "BuongPhong")
                        {
                            trangThai.TrangThaiVeSinh = trangThaiMoi.Trim();
                            if (trangThaiMoi.Trim().ToLower() == "sạch")
                            {
                                trangThai.ThoiGianDonVeSinh = DateTime.Now;
                            }
                        }
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"Đã cập nhật vệ sinh phòng {phong.SoPhong} thành Sạch!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi cập nhật: {ex.Message}";
            }

            return RedirectToPage();
        }

        // HÀM 1: CHECK-IN CHO KHÁCH ĐẶT TRƯỚC (Khởi tạo phiếu lưu trú đầu tiên, cho phép Check-in từng phần)
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

                // Cập nhật giấy tờ và ảnh chụp CCCD/Hộ chiếu cho người nhận phòng đầu tiên
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

                // 1. Chuyển trạng thái đơn đặt phòng sang Đang ở
                datPhongCanCheckIn.TrangThaiDatPhong = "DANG_O";
                datPhongCanCheckIn.NgayNhan = now;
                _context.DAT_PHONG.Update(datPhongCanCheckIn);

                // 2. Tạo bản ghi PHIEU_LUU_TRU cho người đầu tiên nhận phòng
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
                if (tongKhach > 1)
                {
                    TempData["SuccessMessage"] = $"Đã Check-in người đầu tiên (1/{tongKhach} khách)! Phòng đã mở. Bạn có thể bổ sung CCCD/Hộ chiếu cho {tongKhach - 1} khách đi cùng ngay bây giờ hoặc khi họ đến sau.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Check-in hoàn tất (1/1 khách)! Phòng đã chuyển sang trạng thái Đang ở.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi Check-in: {ex.Message}";
            }

            return RedirectToPage();
        }

        // HÀM 2: CHECK-IN TRỰC TIẾP CHO KHÁCH VÃNG LAI (WALK-IN) KÈM ẢNH CCCD/HỘ CHIẾU
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

                // 1. Tạo hoặc cập nhật hồ sơ KHACH_HANG đại diện
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

                // 2. Tạo đơn DAT_PHONG (Tổng số khách đăng ký = soKhachWalkIn)
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

                // 3. Tạo CHI_TIET_DAT_PHONG
                var chiTietWalkIn = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = "CT" + maDatPhongMoi,
                    DAT_PHONGMaDatPhong = maDatPhongMoi,
                    PHONGMaPhong = phong.MaPhong,
                    DonGia = phong.GiaTheoNgay,
                    SoNgayO = soNgayOWalkIn
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTietWalkIn);

                // 4. Tạo PHIEU_LUU_TRU cho người khách đầu tiên
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

                if (soKhachWalkIn > 1)
                {
                    TempData["SuccessMessage"] = $"Đã Check-in cho khách đại diện ({hoTenWalkIn} - 1/{soKhachWalkIn} khách). Phòng đang hiển thị biểu tượng 'Chưa đủ khách', vui lòng bấm vào phòng để chụp CCCD/Hộ chiếu cho các khách ở cùng!";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Đã Check-in hoàn tất (1/1 khách) cho phòng {phong.SoPhong}!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi Check-in khách vãng lai: {ex.Message}";
            }

            return RedirectToPage();
        }

        // HÀM 3: BỔ SUNG THÔNG TIN & ẢNH CHỤP CCCD/HỘ CHIẾU CHO KHÁCH Ở CÙNG (Khi khách đến cùng lúc hoặc đến sau)
        public async Task<IActionResult> OnPostThemKhachOChungAsync(
            string maPhongOChung,
            string hoTenKhachDiCung,
            string loaiGiayToDiCung,
            string soGiayToDiCung,
            string? sdtKhachDiCung,
            IFormFile? anhMatTruocDiCung,
            IFormFile? anhMatSauDiCung)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(maPhongOChung) || string.IsNullOrWhiteSpace(hoTenKhachDiCung) || string.IsNullOrWhiteSpace(soGiayToDiCung))
                {
                    TempData["ErrorMessage"] = "Vui lòng điền Họ tên và Số CCCD/Hộ chiếu của khách ở cùng!";
                    return RedirectToPage();
                }

                if (anhMatTruocDiCung == null || anhMatSauDiCung == null)
                {
                    TempData["ErrorMessage"] = "Theo quy định lưu trú, bắt buộc phải chụp/tải lên đủ 2 mặt (Mặt trước & Mặt sau) của CCCD hoặc Hộ chiếu!";
                    return RedirectToPage();
                }

                DateTime now = DateTime.Now;

                // Lưu 2 ảnh mặt trước & mặt sau vào hệ thống
                string urlTruoc = await SaveIdentityImageAsync(anhMatTruocDiCung, "COMP_F");
                string urlSau = await SaveIdentityImageAsync(anhMatSauDiCung, "COMP_B");
                string loaiGT = string.IsNullOrWhiteSpace(loaiGiayToDiCung) ? "CCCD (Việt Nam)" : loaiGiayToDiCung;
                string kycMetadata = $"KYC|{loaiGT}|{urlTruoc}|{urlSau}";

                string soGiayToChuan = soGiayToDiCung.Trim().Length > 12
                    ? soGiayToDiCung.Trim().Substring(0, 12)
                    : soGiayToDiCung.Trim();

                // 1. Tạo hồ sơ KHACH_HANG cho người ở cùng
                var khachDiCung = new KHACH_HANG
                {
                    MaKH = "KH" + now.ToString("yyMMddHHmmss"),
                    HoTen = hoTenKhachDiCung.Trim(),
                    SoCCCD = soGiayToChuan,
                    SDT = string.IsNullOrWhiteSpace(sdtKhachDiCung) ? "0000000000" : sdtKhachDiCung.Trim(),
                    HangThanhVien = "Thường",
                    DiemThuong = 0,
                    GhiChuDacBiet = kycMetadata
                };
                _context.KHACH_HANG.Add(khachDiCung);

                // 2. Tạo bản ghi PHIEU_LUU_TRU ghi nhận khách này đã Check-in vào phòng
                var phieuLuuTruMoi = new PHIEU_LUU_TRU
                {
                    MaPhieuLuuTru = "PLT" + now.ToString("yyMMddHHmmss"),
                    PHONGMaPhong = maPhongOChung,
                    KHACH_HANGMaKH = khachDiCung.MaKH,
                    ThoiGianCheckIn = now,
                    TrangThaiLuuTru = "DANG_LUU_TRU"
                };
                _context.PHIEU_LUU_TRU.Add(phieuLuuTruMoi);

                await _context.SaveChangesAsync();

                // Kiểm tra lại xem đã đủ số lượng khách đăng ký trong đơn đặt phòng chưa
                var datPhongHienTai = await (from ct in _context.CHI_TIET_DAT_PHONG
                                             join dp in _context.DAT_PHONG on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                             where ct.PHONGMaPhong == maPhongOChung && dp.TrangThaiDatPhong == "DANG_O"
                                             select dp).FirstOrDefaultAsync();

                int soDaCheckIn = await _context.PHIEU_LUU_TRU
                    .CountAsync(p => p.PHONGMaPhong == maPhongOChung && p.TrangThaiLuuTru == "DANG_LUU_TRU");

                int tongDangKy = datPhongHienTai?.SoLuongKhach ?? 1;

                // Nếu khách ở cùng phát sinh nhiều hơn số lượng đăng ký ban đầu (nhưng vẫn <= sức chứa phòng), tự động cập nhật lại SoLuongKhach
                if (datPhongHienTai != null && soDaCheckIn > tongDangKy)
                {
                    datPhongHienTai.SoLuongKhach = soDaCheckIn;
                    tongDangKy = soDaCheckIn;
                    _context.DAT_PHONG.Update(datPhongHienTai);
                    await _context.SaveChangesAsync();
                }

                if (soDaCheckIn >= tongDangKy)
                {
                    TempData["SuccessMessage"] = $"Đã thêm khách '{hoTenKhachDiCung}'! Phòng đã Check-in ĐỦ SỐ LƯỢNG ({soDaCheckIn}/{tongDangKy} khách) và tự động chuyển sang biểu tượng Hoàn tất.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Đã thêm khách '{hoTenKhachDiCung}' ({soDaCheckIn}/{tongDangKy} khách). Còn {tongDangKy - soDaCheckIn} khách chưa làm thủ tục Check-in.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi bổ sung khách ở cùng: {ex.Message}";
            }

            return RedirectToPage();
        }

        // HÀM 4: XỬ LÝ CHECK-OUT (Đóng toàn bộ PHIEU_LUU_TRU của phòng và giải phóng phòng)
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
                    // 1. Chốt trạng thái Booking thành Đã check-out
                    datPhongCanCheckOut.TrangThaiDatPhong = "Đã check-out";
                    datPhongCanCheckOut.NgayTra = now;
                    _context.DAT_PHONG.Update(datPhongCanCheckOut);

                    // 2. Chốt thời gian Check-out cho toàn bộ các khách ở chung trong bảng PHIEU_LUU_TRU
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

                    // 3. Giải phóng trạng thái vật lý của phòng
                    var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == maPhongCheckOut);
                    if (phong != null)
                    {
                        var trangThai = await _context.TRANG_THAI_PHONG.FirstOrDefaultAsync(t => t.MaTrangThai == phong.MaTrangThai);
                        if (trangThai != null)
                        {
                            trangThai.TrangThaiPhong = "Trống";
                            trangThai.TrangThaiVeSinh = "Bẩn nhẹ";
                            trangThai.ThoiGianDonVeSinh = DateTime.Now.AddDays(-1);
                            _context.TRANG_THAI_PHONG.Update(trangThai);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Checkout thành công cho toàn bộ khách trong phòng! Đã cập nhật số liệu sang Tổng quan Tiền sảnh.";
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