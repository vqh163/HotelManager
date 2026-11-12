using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HotelManager.Data;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    public class DatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DatPhongModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Thông tin chi tiết của phòng đang được chọn (Hiển thị ở cột bên trái)
        public PHONG PhongDuocChon { get; set; } = new PHONG();

        // Đối tượng nhận dữ liệu thời gian lưu trú, số lượng khách, tiền cọc, ghi chú
        [BindProperty]
        public DAT_PHONG ThongTinDatPhong { get; set; } = new DAT_PHONG();

        // =====================================================================
        // CÁC THUỘC TÍNH BINDING THÔNG TIN LIÊN HỆ KHÁCH HÀNG (BỔ SUNG CCCD)
        // =====================================================================
        [BindProperty]
        [Required(ErrorMessage = "Vui lòng cập nhật Họ và tên.")]
        [StringLength(50, ErrorMessage = "Họ và tên không vượt quá 50 ký tự.")]
        public string HoTenKhach { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng cập nhật Số CCCD/CMND.")]
        [RegularExpression(@"^(\d{9}|\d{12})$", ErrorMessage = "Số CCCD/CMND phải bao gồm 9 hoặc 12 chữ số.")]
        public string SoCCCDKhach { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng cập nhật Số điện thoại.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.")]
        public string SDTKhach { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng cập nhật Địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng.")]
        public string EmailKhach { get; set; } = string.Empty;

        // Cờ cảnh báo khi hồ sơ khách hàng đang đăng nhập bị thiếu thông tin (Họ tên, CCCD, SĐT, Email)
        public bool ThieuThongTinHoSo { get; set; } = false;

        // Danh sách tên các trường đang bị thiếu trong hồ sơ để nhắc khách hàng cập nhật
        public string DanhSachTruongThieu { get; set; } = string.Empty;

        // Hàm phụ trợ: Tải lại thông tin phòng và kiểm tra tình trạng hồ sơ khách hàng
        private async Task<bool> LoadPageDataAsync(string id, bool isGetRequest)
        {
            var phong = await _context.PHONG
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhong == id);

            if (phong == null) return false;
            PhongDuocChon = phong;

            // Kiểm tra nếu người dùng đã đăng nhập
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                // Nếu tài khoản có liên kết với hồ sơ KHACH_HANG (UC_05)
                if (taiKhoan != null && !string.IsNullOrWhiteSpace(taiKhoan.KHACH_HANGMaKH))
                {
                    var khachHang = await _context.KHACH_HANG
                        .AsNoTracking()
                        .FirstOrDefaultAsync(k => k.MaKH == taiKhoan.KHACH_HANGMaKH);

                    if (khachHang != null)
                    {
                        // Nếu là lần đầu mở trang (GET), tự động điền thông tin từ hồ sơ lên Form
                        if (isGetRequest)
                        {
                            HoTenKhach = khachHang.HoTen ?? string.Empty;
                            SoCCCDKhach = khachHang.SoCCCD ?? string.Empty;
                            SDTKhach = khachHang.SDT ?? string.Empty;
                            EmailKhach = khachHang.Email ?? string.Empty;
                        }

                        // Kiểm tra xem trong hồ sơ gốc của khách hàng có bị thiếu thông tin nào không
                        var cacTruongThieu = new List<string>();
                        if (string.IsNullOrWhiteSpace(khachHang.HoTen)) cacTruongThieu.Add("Họ và tên");
                        if (string.IsNullOrWhiteSpace(khachHang.SoCCCD)) cacTruongThieu.Add("Số CCCD");
                        if (string.IsNullOrWhiteSpace(khachHang.SDT)) cacTruongThieu.Add("Số điện thoại");
                        if (string.IsNullOrWhiteSpace(khachHang.Email)) cacTruongThieu.Add("Email");

                        if (cacTruongThieu.Any())
                        {
                            ThieuThongTinHoSo = true;
                            DanhSachTruongThieu = string.Join(", ", cacTruongThieu);
                        }
                    }
                    else
                    {
                        ThieuThongTinHoSo = true;
                        DanhSachTruongThieu = "Họ và tên, Số CCCD, Số điện thoại, Email";
                    }
                }
                else if (taiKhoan != null && (taiKhoan.VaiTro == "Khách hàng" || string.IsNullOrWhiteSpace(taiKhoan.NHAN_VIENMaNV)))
                {
                    // Tài khoản khách hàng nhưng chưa được gắn mã hồ sơ KHACH_HANG
                    ThieuThongTinHoSo = true;
                    DanhSachTruongThieu = "Họ và tên, Số CCCD, Số điện thoại, Email";
                }
            }

            return true;
        }

        public async Task<IActionResult> OnGetAsync(string id, DateTime? ngayNhan, DateTime? ngayTra, int? soKhach)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage("/QuanLySoDoPhong/TimPhong");
            }

            try
            {
                bool roomExists = await LoadPageDataAsync(id, isGetRequest: true);
                if (!roomExists) return NotFound();

                // Thiết lập thời gian nhận/trả phòng mặc định (Check-in 14:00, Check-out 12:00)
                if (ngayNhan.HasValue && ngayTra.HasValue)
                {
                    ThongTinDatPhong.NgayNhan = ngayNhan.Value.Date.AddHours(14);
                    ThongTinDatPhong.NgayTra = ngayTra.Value.Date.AddHours(12);
                }
                else
                {
                    ThongTinDatPhong.NgayNhan = DateTime.Now.Date.AddHours(14);
                    ThongTinDatPhong.NgayTra = DateTime.Now.Date.AddDays(1).AddHours(12);
                }

                // Thiết lập số lượng khách và tiền cọc mặc định
                ThongTinDatPhong.SoLuongKhach = soKhach ?? 1;
                ThongTinDatPhong.TienCoc = 0;

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi tải thông tin phòng: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            // Bắt buộc đăng nhập trước khi xác nhận đặt phòng
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Auth/DangNhap");
            }

            // Loại bỏ kiểm tra ModelState cho các trường hệ thống tự sinh
            ModelState.Remove("ThongTinDatPhong.MaDatPhong");
            ModelState.Remove("ThongTinDatPhong.TrangThaiDatPhong");
            ModelState.Remove("ThongTinDatPhong.KenhDatPhong");
            ModelState.Remove("ThongTinDatPhong.KHACH_HANGMaKH");
            ModelState.Remove("ThongTinDatPhong.NgayDat");

            // Tải lại thông tin phòng để giữ nguyên hiển thị cột bên trái nếu có lỗi
            bool roomExists = await LoadPageDataAsync(id, isGetRequest: false);
            if (!roomExists) return NotFound();

            // Kiểm tra: Nếu khách hàng để trống bất kỳ thông tin cá nhân nào (bao gồm CCCD) -> Báo lỗi yêu cầu cập nhật
            if (string.IsNullOrWhiteSpace(HoTenKhach) ||
                string.IsNullOrWhiteSpace(SoCCCDKhach) ||
                string.IsNullOrWhiteSpace(SDTKhach) ||
                string.IsNullOrWhiteSpace(EmailKhach))
            {
                ThieuThongTinHoSo = true;
                ModelState.AddModelError(string.Empty, "Hồ sơ của bạn còn thiếu thông tin bắt buộc (Họ tên, Số CCCD, SĐT hoặc Email). Vui lòng điền đầy đủ để cập nhật thông tin trước khi đặt phòng!");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng kiểm tra và cập nhật chính xác các thông tin được yêu cầu!");
                return Page();
            }

            if (!ThongTinDatPhong.NgayNhan.HasValue || !ThongTinDatPhong.NgayTra.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: Bạn phải chọn đầy đủ Ngày nhận và Ngày trả phòng.");
                return Page();
            }

            DateTime today = DateTime.Now.Date;
            DateTime requestedIn = ThongTinDatPhong.NgayNhan.Value.Date.AddHours(14);
            DateTime requestedOut = ThongTinDatPhong.NgayTra.Value.Date.AddHours(12);

            // Lớp phòng thủ 1: Kiểm tra tính hợp lệ của ngày nhận và ngày trả
            if (requestedIn.Date < today)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: Ngày nhận phòng không được chọn trong quá khứ.");
                return Page();
            }

            if (requestedOut.Date <= requestedIn.Date)
            {
                ModelState.AddModelError(string.Empty, "Lỗi: Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 đêm.");
                return Page();
            }

            // Sử dụng Transaction để đảm bảo tính nguyên tử (Atomic) và chống lỗi Double-Booking
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var phong = await _context.PHONG.FirstOrDefaultAsync(p => p.MaPhong == id);
                if (phong == null) return NotFound();

                // Chặn đặt phòng nếu phòng đang bị Khóa hoặc Bảo trì (TT04)
                if (phong.MaTrangThai == "TT04")
                {
                    ModelState.AddModelError(string.Empty, "Phòng này hiện đang được bảo trì hoặc bị khóa. Vui lòng chọn phòng khác.");
                    return Page();
                }

                // Kiểm tra sức chứa tối đa của phòng
                if (ThongTinDatPhong.SoLuongKhach > phong.SucChuaToiDa)
                {
                    ModelState.AddModelError(string.Empty, $"Phòng {phong.SoPhong} chỉ cho phép tối đa {phong.SucChuaToiDa} khách.");
                    return Page();
                }

                // Lớp phòng thủ 2: Kiểm tra trùng lịch đặt phòng (Check Overlap Date) bằng LINQ
                bool isDoubleBooked = await (from dp in _context.DAT_PHONG
                                             join ct in _context.CHI_TIET_DAT_PHONG on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                             where ct.PHONGMaPhong == phong.MaPhong
                                                && dp.TrangThaiDatPhong != "Đã hủy"
                                                && dp.TrangThaiDatPhong != "Đã check-out"
                                                && dp.TrangThaiDatPhong != "Đã hoàn tất"
                                                && dp.NgayNhan < requestedOut
                                                && dp.NgayTra > requestedIn
                                             select dp.MaDatPhong).AnyAsync();

                if (isDoubleBooked)
                {
                    ModelState.AddModelError(string.Empty, "Rất tiếc! Phòng này đã có người đặt trong khoảng thời gian bạn chọn. Vui lòng chọn ngày khác.");
                    return Page();
                }

                // Chuẩn hóa dữ liệu liên hệ người dùng vừa nhập/cập nhật
                string hoTenChuan = HoTenKhach.Trim();
                string cccdChuan = SoCCCDKhach.Trim();
                string sdtChuan = SDTKhach.Trim();
                string emailChuan = EmailKhach.Trim();

                string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                string timestamp = DateTime.Now.ToString("yyMMddHHmmss");

                // =================================================================
                // ĐỒNG BỘ & CẬP NHẬT HỒ SƠ KHÁCH HÀNG (UC_03 & UC_05)
                // =================================================================
                string maKhachHangChot = string.Empty;
                var tenDangNhap = User.Identity.Name;
                var taiKhoanDangNhap = await _context.TAI_KHOAN
                    .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                bool isStaffBooking = taiKhoanDangNhap != null &&
                                     (taiKhoanDangNhap.VaiTro == "Lễ tân" ||
                                      taiKhoanDangNhap.VaiTro == "Quản lý" ||
                                      taiKhoanDangNhap.VaiTro == "Sales" ||
                                      taiKhoanDangNhap.VaiTro == "Quản trị viên");

                if (isStaffBooking)
                {
                    // Trường hợp Nhân viên (Lễ tân/Sales) đặt phòng cho khách Walk-in: Tìm theo CCCD hoặc SĐT
                    var khachHangCu = await _context.KHACH_HANG
                        .FirstOrDefaultAsync(k => k.SoCCCD == cccdChuan || k.SDT == sdtChuan);

                    if (khachHangCu != null)
                    {
                        // Cảnh báo nếu khách nằm trong danh sách Blacklist (UC_05)
                        if (khachHangCu.Blacklist == "Có" || khachHangCu.Blacklist == "True")
                        {
                            ModelState.AddModelError(string.Empty, $"Cảnh báo: Khách hàng {khachHangCu.HoTen} (CCCD: {cccdChuan}) đang nằm trong danh sách hạn chế (Blacklist)!");
                            return Page();
                        }

                        maKhachHangChot = khachHangCu.MaKH;
                        khachHangCu.HoTen = hoTenChuan;
                        khachHangCu.SoCCCD = cccdChuan;
                        khachHangCu.SDT = sdtChuan;
                        khachHangCu.Email = emailChuan;
                        _context.KHACH_HANG.Update(khachHangCu);
                    }
                    else
                    {
                        maKhachHangChot = $"KH{timestamp}{randomSuffix}";
                        var khachHangMoi = new KHACH_HANG
                        {
                            MaKH = maKhachHangChot,
                            HoTen = hoTenChuan,
                            SoCCCD = cccdChuan,
                            SDT = sdtChuan,
                            Email = emailChuan,
                            HangThanhVien = "Thành viên mới",
                            DiemThuong = 0,
                            Blacklist = "Không"
                        };
                        _context.KHACH_HANG.Add(khachHangMoi);
                    }
                }
                else if (taiKhoanDangNhap != null)
                {
                    // Trường hợp Khách hàng tự đặt phòng trực tuyến
                    // Kiểm tra xem Số CCCD mới nhập có bị trùng với một hồ sơ khách hàng khác không
                    bool trungCCCD = await _context.KHACH_HANG
                        .AnyAsync(k => k.SoCCCD == cccdChuan && k.MaKH != taiKhoanDangNhap.KHACH_HANGMaKH);

                    if (trungCCCD)
                    {
                        ModelState.AddModelError("SoCCCDKhach", "Số CCCD này đã được đăng ký cho một hồ sơ khách hàng khác.");
                        return Page();
                    }

                    if (!string.IsNullOrWhiteSpace(taiKhoanDangNhap.KHACH_HANGMaKH))
                    {
                        maKhachHangChot = taiKhoanDangNhap.KHACH_HANGMaKH;
                        var khUpdate = await _context.KHACH_HANG
                            .FirstOrDefaultAsync(k => k.MaKH == maKhachHangChot);

                        if (khUpdate != null)
                        {
                            // Tự động cập nhật bổ sung các thông tin còn thiếu (như CCCD, SĐT...) vào hồ sơ của khách
                            khUpdate.HoTen = hoTenChuan;
                            khUpdate.SoCCCD = cccdChuan;
                            khUpdate.SDT = sdtChuan;
                            khUpdate.Email = emailChuan;
                            _context.KHACH_HANG.Update(khUpdate);
                        }
                    }
                    else
                    {
                        // Nếu tài khoản khách hàng chưa có bản ghi KHACH_HANG thì khởi tạo và liên kết ngay
                        maKhachHangChot = $"KH{timestamp}{randomSuffix}";
                        var khMoi = new KHACH_HANG
                        {
                            MaKH = maKhachHangChot,
                            HoTen = hoTenChuan,
                            SoCCCD = cccdChuan,
                            SDT = sdtChuan,
                            Email = emailChuan,
                            HangThanhVien = "Thành viên mới",
                            DiemThuong = 0,
                            Blacklist = "Không"
                        };
                        _context.KHACH_HANG.Add(khMoi);

                        taiKhoanDangNhap.KHACH_HANGMaKH = maKhachHangChot;
                        _context.TAI_KHOAN.Update(taiKhoanDangNhap);
                    }
                }

                // =================================================================
                // LƯU ĐƠN ĐẶT PHÒNG & CHI TIẾT ĐẶT PHÒNG (UC_03)
                // =================================================================
                ThongTinDatPhong.MaDatPhong = $"DP{timestamp}{randomSuffix}";
                ThongTinDatPhong.KHACH_HANGMaKH = maKhachHangChot;
                ThongTinDatPhong.NgayDat = DateTime.Now;
                ThongTinDatPhong.NgayNhan = requestedIn;
                ThongTinDatPhong.NgayTra = requestedOut;
                ThongTinDatPhong.TrangThaiDatPhong = isStaffBooking ? "Đã xác nhận" : "Chờ xác nhận";
                ThongTinDatPhong.KenhDatPhong = isStaffBooking ? "Trực tiếp tại quầy" : "Website Trực tuyến";

                int soNgayO = Math.Max(1, (requestedOut.Date - requestedIn.Date).Days);

                _context.DAT_PHONG.Add(ThongTinDatPhong);

                var chiTiet = new CHI_TIET_DAT_PHONG
                {
                    MaChiTietDatPhong = $"CT{timestamp}{randomSuffix}",
                    DAT_PHONGMaDatPhong = ThongTinDatPhong.MaDatPhong,
                    PHONGMaPhong = phong.MaPhong,
                    DonGia = phong.GiaTheoNgay,
                    SoNgayO = soNgayO
                };
                _context.CHI_TIET_DAT_PHONG.Add(chiTiet);

                // Cập nhật trạng thái phòng sang TT02 nếu nhận phòng ngay trong ngày hôm nay
                if (requestedIn.Date == today)
                {
                    phong.MaTrangThai = "TT02";
                    _context.PHONG.Update(phong);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToPage("./XacNhanDatPhong", new { maDatPhong = ThongTinDatPhong.MaDatPhong });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi xử lý đặt phòng: " + ex.Message);
                return Page();
            }
        }
    }
}