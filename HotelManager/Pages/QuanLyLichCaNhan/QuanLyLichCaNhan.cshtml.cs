using Microsoft.AspNetCore.Authorization;
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
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyLichCaNhan
{
    [Authorize(Policy = HangSoHeThong.PolicyNoiBo)]
    public class QuanLyLichCaNhanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuanLyLichCaNhanModel> _logger;

        public QuanLyLichCaNhanModel(ApplicationDbContext context, ILogger<QuanLyLichCaNhanModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeRole { get; set; } = string.Empty;
        public bool IsManager { get; set; } = false;
        public int ShiftCountToday { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "month";

        public DateTime CurrentDate { get; set; }

        public List<ScheduleViewModel> MySchedules { get; set; } = new List<ScheduleViewModel>();
        public List<DON_XIN_NGHI> PendingRequests { get; set; } = new List<DON_XIN_NGHI>();
        public List<DON_XIN_NGHI> EmployeeNotifications { get; set; } = new List<DON_XIN_NGHI>();

        public int DaysInMonth { get; set; }
        public int StartDayOfWeek { get; set; }
        public List<string> DanhSachChucVu { get; set; } = new List<string>();

        public class ScheduleViewModel
        {
            public string MaLich { get; set; } = string.Empty;
            public DateTime ThoiGianBatDau { get; set; }
            public DateTime ThoiGianKetThuc { get; set; }
            public string ChucVu { get; set; } = string.Empty;
            public string TenNhanVien { get; set; } = string.Empty;
            public string CaLamViec { get; set; } = string.Empty;
            public string TieuDe { get; set; } = string.Empty;
            public string GhiChu { get; set; } = string.Empty;
        }

        public class InputScheduleViewModel
        {
            public DateTime NgayLamViec { get; set; } = DateTime.Today;
            public string ChucVu { get; set; } = string.Empty;
            public string TenNhanVien { get; set; } = string.Empty;
            public string MaNhanVien { get; set; } = string.Empty;
            public string CaLamViec { get; set; } = "CaSang";
            public string? TuChonBatDau { get; set; }
            public string? TuChonKetThuc { get; set; }
            public string TieuDe { get; set; } = string.Empty;
            public string Checklist { get; set; } = string.Empty;
            public string GhiChu { get; set; } = string.Empty;
        }

        [BindProperty]
        public InputScheduleViewModel InputData { get; set; } = new InputScheduleViewModel();

        [BindProperty]
        public DON_XIN_NGHI DonNghiData { get; set; } = new DON_XIN_NGHI();

        [TempData]
        public string? AlertMessage { get; set; }

        public string EmployeeDataJson { get; set; } = "{}";

        private bool LaCapQuanLy => User.IsInRole(HangSoHeThong.VaiTroQuanLy) || User.IsInRole(HangSoHeThong.VaiTroGiamDoc);

        // Lấy chính xác (MaNV, HoTen) của nhân viên đang đăng nhập từ Cookie Claim & SQL Server
        private async Task<(string? MaNV, string HoTen)> LayNhanVienDangNhapAsync()
        {
            string username = User.Identity?.Name ?? string.Empty;
            string? maNV = User.FindFirst("MaNV")?.Value;

            if (string.IsNullOrWhiteSpace(maNV))
            {
                var tk = await _context.TAI_KHOAN.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == username);
                maNV = tk?.NHAN_VIENMaNV;
            }

            if (!string.IsNullOrWhiteSpace(maNV))
            {
                var nv = await _context.NHAN_VIEN.AsNoTracking().FirstOrDefaultAsync(n => n.MaNV == maNV);
                if (nv != null && !string.IsNullOrWhiteSpace(nv.HoTen))
                    return (maNV, nv.HoTen);
            }

            return (maNV, username);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (User.Identity == null || !User.Identity.IsAuthenticated)
                    return RedirectToPage("/Auth/DangNhap");

                var (maNVHienTai, hoTenHienTai) = await LayNhanVienDangNhapAsync();
                EmployeeName = hoTenHienTai;

                var roleClaim = User.FindFirst(ClaimTypes.Role);
                EmployeeRole = roleClaim != null ? roleClaim.Value : "Nhân viên";

                if (EmployeeRole == HangSoHeThong.VaiTroQuanTri)
                {
                    TempData["ThongBaoLoi"] = "Quản trị viên hệ thống không tham gia vào lịch vận hành.";
                    return RedirectToPage("/QuanTriHeThong/QuanLyTaiKhoan");
                }

                IsManager = LaCapQuanLy;

                DateTime refDate = SelectedDate ?? DateTime.Today;
                SelectedDate = refDate;
                CurrentDate = new DateTime(refDate.Year, refDate.Month, 1);

                // 1. Tải đơn xin nghỉ phép trực tiếp từ bảng SQL DON_XIN_NGHI (bỏ danh sách static trên RAM)
                if (IsManager)
                {
                    PendingRequests = await _context.DON_XIN_NGHI.AsNoTracking()
                        .Where(x => x.TrangThai == "Chờ duyệt")
                        .OrderByDescending(x => x.NgayTao)
                        .ToListAsync();
                }
                else
                {
                    EmployeeNotifications = await _context.DON_XIN_NGHI.AsNoTracking()
                        .Where(x => ((!string.IsNullOrEmpty(maNVHienTai) && x.NHAN_VIENMaNV == maNVHienTai)
                                     || x.TenNhanVien == hoTenHienTai
                                     || x.TenNhanVien == User.Identity.Name)
                                    && x.TrangThai != "Chờ duyệt"
                                    && !x.IsReadByEmployee)
                        .OrderByDescending(x => x.NgayTao)
                        .ToListAsync();
                }

                // 2. Tải danh sách nhân viên và chức vụ trực tiếp từ SQL Server bằng LINQ
                var chucVuDict = await _context.CHUC_VU.AsNoTracking()
                    .ToDictionaryAsync(c => c.MaChucVu, c => c.TenChucVu ?? string.Empty);

                var danhSachNhanVienDB = await _context.NHAN_VIEN.AsNoTracking().ToListAsync();
                var nvHoTenDict = danhSachNhanVienDB.ToDictionary(n => n.MaNV, n => n.HoTen ?? n.MaNV);

                // 3. Tải lịch làm việc: Lọc chính xác theo NHAN_VIENMaNV (khắc phục lỗi lệch TenDangNhap vs HoTen)
                var lichQuery = _context.LICH_LAM_VIEC.AsNoTracking().AsQueryable();
                if (!IsManager && !string.IsNullOrWhiteSpace(maNVHienTai))
                {
                    lichQuery = lichQuery.Where(l =>
                        l.NHAN_VIENMaNV == maNVHienTai
                        || (string.IsNullOrEmpty(l.NHAN_VIENMaNV) && l.GhiChu != null && l.GhiChu.Contains(hoTenHienTai)));
                }

                var rawSchedules = await lichQuery.ToListAsync();
                foreach (var item in rawSchedules)
                {
                    var vm = new ScheduleViewModel
                    {
                        MaLich = item.MaLich,
                        CaLamViec = item.CaLamViec ?? "Chưa rõ",
                        GhiChu = item.GhiChu ?? string.Empty,
                        ThoiGianBatDau = item.GioCheckIn ?? item.NgayLamViec?.AddHours(6) ?? DateTime.Today.AddHours(6),
                        ThoiGianKetThuc = item.GioCheckOut ?? item.NgayLamViec?.AddHours(14) ?? DateTime.Today.AddHours(14),
                        TenNhanVien = (!string.IsNullOrEmpty(item.NHAN_VIENMaNV) && nvHoTenDict.TryGetValue(item.NHAN_VIENMaNV, out var ten))
                            ? ten : "Nhân viên",
                        ChucVu = !string.IsNullOrWhiteSpace(item.ChucVu) ? item.ChucVu : "Lễ tân",
                        TieuDe = !string.IsNullOrWhiteSpace(item.TieuDe) ? item.TieuDe : "Lịch làm việc"
                    };

                    // Tương thích ngược với các bản ghi lịch cũ lưu metadata trong GhiChu
                    if (!string.IsNullOrEmpty(item.GhiChu) && item.GhiChu.StartsWith("["))
                    {
                        try
                        {
                            int endTitle = item.GhiChu.IndexOf("]");
                            if (endTitle > 0 && string.IsNullOrWhiteSpace(item.TieuDe))
                                vm.TieuDe = item.GhiChu.Substring(1, endTitle - 1);

                            int startNV = item.GhiChu.IndexOf("NV: ") + 4;
                            int endNV = item.GhiChu.IndexOf(" (");
                            if (startNV > 3 && endNV > startNV && vm.TenNhanVien == "Nhân viên")
                                vm.TenNhanVien = item.GhiChu.Substring(startNV, endNV - startNV);

                            int startCV = item.GhiChu.IndexOf("- CV: ") + 6;
                            int endCV = item.GhiChu.IndexOf(". Ghi chú:");
                            if (startCV > 5 && endCV > startCV && string.IsNullOrWhiteSpace(item.ChucVu))
                                vm.ChucVu = item.GhiChu.Substring(startCV, endCV - startCV);
                        }
                        catch { }
                    }

                    if (IsManager
                        || (!string.IsNullOrEmpty(maNVHienTai) && item.NHAN_VIENMaNV == maNVHienTai)
                        || vm.TenNhanVien == hoTenHienTai
                        || vm.TenNhanVien == User.Identity.Name)
                    {
                        MySchedules.Add(vm);
                    }
                }

                MySchedules = MySchedules.OrderBy(x => x.ThoiGianBatDau).ToList();

                if (!IsManager)
                {
                    ShiftCountToday = MySchedules.Count(x =>
                        x.ThoiGianBatDau.Date == SelectedDate.Value.Date ||
                        (x.ThoiGianKetThuc.Date == SelectedDate.Value.Date && x.ThoiGianBatDau.Date != x.ThoiGianKetThuc.Date));
                }

                DaysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);
                StartDayOfWeek = CurrentDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)CurrentDate.DayOfWeek - 1;

                var employeeList = danhSachNhanVienDB.Select(nv => new
                {
                    MaNV = nv.MaNV,
                    HoTen = nv.HoTen ?? nv.MaNV,
                    ChucVu = (!string.IsNullOrWhiteSpace(nv.CHUC_VUMaChucVu) && chucVuDict.TryGetValue(nv.CHUC_VUMaChucVu, out var tenCV) && !string.IsNullOrWhiteSpace(tenCV))
                        ? tenCV : (nv.CHUC_VUMaChucVu ?? "Nhân viên")
                }).ToList();

                var groupedEmployees = employeeList
                    .GroupBy(x => x.ChucVu)
                    .ToDictionary(g => g.Key, g => g.Select(nv => new { id = nv.MaNV, name = nv.HoTen }).ToList());
                EmployeeDataJson = JsonSerializer.Serialize(groupedEmployees);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải trang Quản lý lịch cá nhân.");
                AlertMessage = "Đã xảy ra lỗi khi tải dữ liệu lịch làm việc.";
                return Page();
            }
        }

        // Chỉ Quản lý / Giám đốc được phân công ca làm việc
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!LaCapQuanLy) return Forbid();

            try
            {
                string finalCaLamViec = InputData.CaLamViec;
                if (finalCaLamViec == "Khac" && !string.IsNullOrEmpty(InputData.TuChonBatDau) && !string.IsNullOrEmpty(InputData.TuChonKetThuc))
                {
                    finalCaLamViec = $"Khac|{InputData.TuChonBatDau}|{InputData.TuChonKetThuc}";
                }

                DateTime gioCheckIn = InputData.NgayLamViec.Date.AddHours(6);
                DateTime gioCheckOut = InputData.NgayLamViec.Date.AddHours(14);
                if (InputData.CaLamViec == "CaChieu") { gioCheckIn = InputData.NgayLamViec.Date.AddHours(14); gioCheckOut = InputData.NgayLamViec.Date.AddHours(22); }
                else if (InputData.CaLamViec == "CaDem") { gioCheckIn = InputData.NgayLamViec.Date.AddHours(22); gioCheckOut = InputData.NgayLamViec.Date.AddDays(1).AddHours(6); }
                else if (InputData.CaLamViec == "Khac" && TimeSpan.TryParse(InputData.TuChonBatDau, out var start) && TimeSpan.TryParse(InputData.TuChonKetThuc, out var end))
                {
                    gioCheckIn = InputData.NgayLamViec.Date.Add(start);
                    gioCheckOut = InputData.NgayLamViec.Date.Add(end);
                    if (gioCheckOut <= gioCheckIn) gioCheckOut = gioCheckOut.AddDays(1);
                }

                // Kiểm tra trùng ca làm việc của cùng nhân viên
                if (!string.IsNullOrWhiteSpace(InputData.MaNhanVien))
                {
                    bool trungCa = await _context.LICH_LAM_VIEC.AsNoTracking()
                        .AnyAsync(l => l.NHAN_VIENMaNV == InputData.MaNhanVien
                            && l.GioCheckIn < gioCheckOut
                            && l.GioCheckOut > gioCheckIn);
                    if (trungCa)
                    {
                        AlertMessage = $"Nhân viên {InputData.TenNhanVien} đã có ca làm việc trùng khung giờ này!";
                        return RedirectToPage(new { SelectedDate = InputData.NgayLamViec.ToString("yyyy-MM-dd"), ActiveTab });
                    }
                }

                string noiDungGom = $"[{InputData.TieuDe}] NV: {InputData.TenNhanVien} ({InputData.MaNhanVien}) - CV: {InputData.ChucVu}. Ghi chú: {InputData.GhiChu}. Checklist: {InputData.Checklist}";
                if (noiDungGom.Length > 255) noiDungGom = noiDungGom.Substring(0, 255);

                var newSchedule = new LICH_LAM_VIEC
                {
                    MaLich = BaoMatHelper.SinhMaNgauNhien("LLV"),
                    NHAN_VIENMaNV = InputData.MaNhanVien?.Trim(),
                    TieuDe = string.IsNullOrWhiteSpace(InputData.TieuDe) ? "Ca trực" : InputData.TieuDe.Trim(),
                    ChucVu = InputData.ChucVu?.Trim(),
                    NgayLamViec = InputData.NgayLamViec.Date,
                    CaLamViec = finalCaLamViec.Length > 20 ? finalCaLamViec.Substring(0, 20) : finalCaLamViec,
                    GioCheckIn = gioCheckIn,
                    GioCheckOut = gioCheckOut,
                    GhiChu = noiDungGom,
                    TrangThai = "Sắp diễn ra"
                };

                _context.LICH_LAM_VIEC.Add(newSchedule);
                await _context.SaveChangesAsync();

                AlertMessage = "Đã phân công ca làm việc thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi phân công ca làm việc.");
                AlertMessage = "Đã xảy ra lỗi hệ thống khi phân công ca làm việc.";
            }

            return RedirectToPage(new { SelectedDate = InputData.NgayLamViec.ToString("yyyy-MM-dd"), ActiveTab });
        }

        // Chỉ Quản lý / Giám đốc được xóa lịch làm việc
        public async Task<IActionResult> OnPostDeleteAsync(string deleteId, string selectedDate, string activeTab)
        {
            if (!LaCapQuanLy) return Forbid();

            try
            {
                var schedule = await _context.LICH_LAM_VIEC.FirstOrDefaultAsync(x => x.MaLich == deleteId);
                if (schedule != null)
                {
                    _context.LICH_LAM_VIEC.Remove(schedule);
                    await _context.SaveChangesAsync();
                    AlertMessage = "Đã xóa lịch làm việc thành công!";
                }
                else
                {
                    AlertMessage = "Không tìm thấy lịch làm việc để xóa!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa lịch làm việc {Id}", deleteId);
                AlertMessage = "Đã xảy ra lỗi hệ thống khi xóa lịch làm việc.";
            }
            return RedirectToPage(new { SelectedDate = selectedDate, ActiveTab = activeTab });
        }

        // Nhân viên gửi đơn xin nghỉ phép lưu bền vững vào bảng SQL DON_XIN_NGHI
        public async Task<IActionResult> OnPostCreateDonNghiAsync()
        {
            try
            {
                var (maNV, hoTen) = await LayNhanVienDangNhapAsync();
                string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Nhân viên";

                var donMoi = new DON_XIN_NGHI
                {
                    NHAN_VIENMaNV = maNV,
                    TenNhanVien = hoTen,
                    ChucVu = role,
                    TuNgay = DonNghiData.TuNgay.Date,
                    DenNgay = DonNghiData.DenNgay < DonNghiData.TuNgay ? DonNghiData.TuNgay.Date : DonNghiData.DenNgay.Date,
                    SoNgay = Math.Max(1, (DonNghiData.DenNgay.Date - DonNghiData.TuNgay.Date).Days + 1),
                    LyDo = (DonNghiData.LyDo ?? string.Empty).Trim(),
                    TrangThai = "Chờ duyệt",
                    NgayTao = DateTime.Now,
                    IsReadByEmployee = false
                };

                _context.DON_XIN_NGHI.Add(donMoi);
                await _context.SaveChangesAsync();

                AlertMessage = "Đã gửi đơn xin nghỉ phép thành công. Vui lòng chờ quản lý duyệt!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi đơn xin nghỉ phép.");
                AlertMessage = "Đã xảy ra lỗi hệ thống khi gửi đơn xin nghỉ phép.";
            }

            return RedirectToPage(new { SelectedDate = Request.Form["SelectedDate"].ToString(), ActiveTab = Request.Form["ActiveTab"].ToString() });
        }

        // Chỉ Quản lý / Giám đốc được duyệt đơn nghỉ phép (chặn tự duyệt đơn của chính mình)
        public async Task<IActionResult> OnPostDuyetDonAsync(int donId, string actionType)
        {
            if (!LaCapQuanLy) return Forbid();

            try
            {
                var don = await _context.DON_XIN_NGHI.FirstOrDefaultAsync(x => x.Id == donId);
                if (don != null)
                {
                    var (maNVNguoiDuyet, hoTenNguoiDuyet) = await LayNhanVienDangNhapAsync();
                    if (!string.IsNullOrEmpty(maNVNguoiDuyet) && don.NHAN_VIENMaNV == maNVNguoiDuyet)
                    {
                        AlertMessage = "Bạn không thể tự duyệt đơn xin nghỉ phép của chính mình!";
                        return RedirectToPage(new { SelectedDate = Request.Form["SelectedDate"].ToString(), ActiveTab = Request.Form["ActiveTab"].ToString() });
                    }

                    if (actionType == "Approve")
                    {
                        don.TrangThai = "Đã duyệt";
                        don.NguoiDuyet = hoTenNguoiDuyet;
                        AlertMessage = $"Đã DUYỆT đơn xin nghỉ của nhân viên {don.TenNhanVien}!";
                    }
                    else if (actionType == "Reject")
                    {
                        don.TrangThai = "Từ chối";
                        don.NguoiDuyet = hoTenNguoiDuyet;
                        AlertMessage = $"Đã TỪ CHỐI đơn xin nghỉ của {don.TenNhanVien}!";
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi duyệt đơn nghỉ phép {DonId}", donId);
                AlertMessage = "Đã xảy ra lỗi hệ thống khi xử lý đơn nghỉ phép.";
            }
            return RedirectToPage(new { SelectedDate = Request.Form["SelectedDate"].ToString(), ActiveTab = Request.Form["ActiveTab"].ToString() });
        }

        public async Task<IActionResult> OnPostMarkNotificationReadAsync(int notifId)
        {
            try
            {
                var (maNV, hoTen) = await LayNhanVienDangNhapAsync();
                var don = await _context.DON_XIN_NGHI.FirstOrDefaultAsync(x => x.Id == notifId);
                if (don != null && ((!string.IsNullOrEmpty(maNV) && don.NHAN_VIENMaNV == maNV)
                    || don.TenNhanVien == hoTen || don.TenNhanVien == User.Identity?.Name))
                {
                    don.IsReadByEmployee = true;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đánh dấu đã đọc thông báo {Id}", notifId);
            }
            return RedirectToPage(new { SelectedDate = Request.Form["SelectedDate"].ToString(), ActiveTab = Request.Form["ActiveTab"].ToString() });
        }
    }
}
