using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using System.Text.Json;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyLichCaNhan
{
    public class QuanLyLichCaNhanModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public QuanLyLichCaNhanModel(ApplicationDbContext context) { _context = context; }

        public string EmployeeName { get; set; } = "";
        public string EmployeeRole { get; set; } = "";
        public bool IsManager { get; set; } = false;
        public int ShiftCountToday { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "month";

        public DateTime CurrentDate { get; set; }

        public static List<DON_XIN_NGHI> DatabaseDonNghi = new List<DON_XIN_NGHI>();

        public List<ScheduleViewModel> MySchedules { get; set; } = new List<ScheduleViewModel>();
        public List<DON_XIN_NGHI> PendingRequests { get; set; } = new List<DON_XIN_NGHI>();

        // MỚI: DANH SÁCH THÔNG BÁO CHO NHÂN VIÊN
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
        public string AlertMessage { get; set; }

        public string EmployeeDataJson { get; set; } = "{}";

        public async Task<IActionResult> OnGetLogout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (User.Identity == null || !User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Auth/DangNhap");
                }

                EmployeeName = User.Identity.Name ?? "Tài khoản ẩn danh";

                var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
                EmployeeRole = roleClaim != null ? roleClaim.Value : "Nhân viên";

                if (EmployeeRole == "Quản trị viên")
                {
                    TempData["ThongBaoLoi"] = "Quản trị viên hệ thống không tham gia vào lịch vận hành.";
                    return RedirectToPage("/QuanTriHeThong/QuanLyTaiKhoan");
                }

                IsManager = (EmployeeRole == "Quản lý" || EmployeeRole == "Trưởng bộ phận" || User.IsInRole("Quản lý"));

                DateTime refDate = SelectedDate ?? DateTime.Today;
                SelectedDate = refDate;
                CurrentDate = new DateTime(refDate.Year, refDate.Month, 1);

                // --- MỚI: PHÂN LUỒNG THÔNG BÁO CHUÔNG ---
                if (IsManager)
                {
                    // Quản lý thấy đơn chờ duyệt
                    PendingRequests = DatabaseDonNghi.Where(x => x.TrangThai == "Chờ duyệt").OrderByDescending(x => x.NgayTao).ToList();
                }
                else
                {
                    // Nhân viên thấy kết quả duyệt đơn chưa đọc
                    EmployeeNotifications = DatabaseDonNghi.Where(x => x.TenNhanVien == EmployeeName && x.TrangThai != "Chờ duyệt" && !x.IsReadByEmployee).OrderByDescending(x => x.NgayTao).ToList();
                }

                var rawSchedules = await _context.LICH_LAM_VIEC.AsNoTracking().ToListAsync();
                foreach (var item in rawSchedules)
                {
                    var vm = new ScheduleViewModel
                    {
                        MaLich = item.MaLich,
                        CaLamViec = item.CaLamViec ?? "Chưa rõ",
                        GhiChu = item.GhiChu ?? "",
                        ThoiGianBatDau = item.GioCheckIn ?? item.NgayLamViec?.AddHours(6) ?? DateTime.Today.AddHours(6),
                        ThoiGianKetThuc = item.GioCheckOut ?? item.NgayLamViec?.AddHours(14) ?? DateTime.Today.AddHours(14),
                        TenNhanVien = "Nhân viên",
                        ChucVu = "Lễ tân",
                        TieuDe = "Lịch làm việc"
                    };

                    if (!string.IsNullOrEmpty(item.GhiChu) && item.GhiChu.StartsWith("["))
                    {
                        try
                        {
                            int endTitle = item.GhiChu.IndexOf("]");
                            if (endTitle > 0) vm.TieuDe = item.GhiChu.Substring(1, endTitle - 1);

                            int startNV = item.GhiChu.IndexOf("NV: ") + 4;
                            int endNV = item.GhiChu.IndexOf(" (");
                            if (startNV > 3 && endNV > startNV)
                                vm.TenNhanVien = item.GhiChu.Substring(startNV, endNV - startNV);

                            int startCV = item.GhiChu.IndexOf("- CV: ") + 6;
                            int endCV = item.GhiChu.IndexOf(". Ghi chú:");
                            if (startCV > 5 && endCV > startCV)
                                vm.ChucVu = item.GhiChu.Substring(startCV, endCV - startCV);
                        }
                        catch { }
                    }

                    if (IsManager || vm.TenNhanVien == EmployeeName)
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

                // ============================================
                // LẤY DANH SÁCH NHÂN VIÊN THỰC TẾ TỪ CSDL
                // ============================================
                var dsNhanVienDB = await _context.TAI_KHOAN
                    .Where(x => x.VaiTro != "Khách hàng" && x.VaiTro != "Quản trị viên") // Chỉ lấy nhân viên, bỏ qua khách hàng và Admin
                    .Select(x => new
                    {
                        MaNV = x.MaTaiKhoan,      // Lấy mã tài khoản
                        HoTen = x.TenDangNhap,    // Dùng tên đăng nhập làm tên hiển thị
                        ChucVu = x.VaiTro         // Dùng cột VaiTro làm chức vụ phân ca
                    })
                    .ToListAsync();

                // Gom nhóm nhân viên theo từng chức vụ
                var groupedEmployees = dsNhanVienDB
                    .GroupBy(x => !string.IsNullOrEmpty(x.ChucVu) ? x.ChucVu : "Chưa phân công")
                    .ToDictionary(g => g.Key, g => g.Select(nv => new { id = nv.MaNV, name = nv.HoTen }).ToList());

                // Lấy danh sách chức vụ tự động để hiển thị ở Dropdown Lọc và Giao việc
                DanhSachChucVu = groupedEmployees.Keys.OrderBy(k => k).ToList();

                // Chuyển dữ liệu sang JSON để Javascript ở file .cshtml đọc
                EmployeeDataJson = JsonSerializer.Serialize(groupedEmployees);

                EmployeeDataJson = JsonSerializer.Serialize(groupedEmployees);

                return Page();
            }
            catch (Exception ex)
            {
                AlertMessage = $"Lỗi tải dữ liệu: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                string finalCaLamViec = InputData.CaLamViec;
                if (finalCaLamViec == "Khac" && !string.IsNullOrEmpty(InputData.TuChonBatDau) && !string.IsNullOrEmpty(InputData.TuChonKetThuc))
                {
                    finalCaLamViec = $"Khac|{InputData.TuChonBatDau}|{InputData.TuChonKetThuc}";
                }

                string noiDungGom = $"[{InputData.TieuDe}] NV: {InputData.TenNhanVien} ({InputData.MaNhanVien}) - CV: {InputData.ChucVu}. Ghi chú: {InputData.GhiChu}. Checklist: {InputData.Checklist}";

                DateTime gioCheckIn = InputData.NgayLamViec.AddHours(6);
                DateTime gioCheckOut = InputData.NgayLamViec.AddHours(14);
                if (InputData.CaLamViec == "CaChieu") { gioCheckIn = InputData.NgayLamViec.AddHours(14); gioCheckOut = InputData.NgayLamViec.AddHours(22); }
                else if (InputData.CaLamViec == "CaDem") { gioCheckIn = InputData.NgayLamViec.AddHours(22); gioCheckOut = InputData.NgayLamViec.AddDays(1).AddHours(6); }
                else if (InputData.CaLamViec == "Khac" && TimeSpan.TryParse(InputData.TuChonBatDau, out var start) && TimeSpan.TryParse(InputData.TuChonKetThuc, out var end))
                {
                    gioCheckIn = InputData.NgayLamViec.Add(start);
                    gioCheckOut = InputData.NgayLamViec.Add(end);
                    if (gioCheckOut <= gioCheckIn) gioCheckOut = gioCheckOut.AddDays(1);
                }

                var newSchedule = new LICH_LAM_VIEC
                {
                    MaLich = "LLV" + DateTime.Now.Ticks.ToString().Substring(10),
                    NgayLamViec = InputData.NgayLamViec,
                    CaLamViec = finalCaLamViec,
                    GioCheckIn = gioCheckIn,
                    GioCheckOut = gioCheckOut,
                    GhiChu = noiDungGom,
                    TrangThai = "Sắp diễn ra"
                };

                _context.LICH_LAM_VIEC.Add(newSchedule);
                await _context.SaveChangesAsync();

                AlertMessage = "✅ Đã phân công ca làm việc thành công!";
            }
            catch (Exception ex)
            {
                AlertMessage = $"Lỗi khi lưu: {ex.Message}";
            }

            return RedirectToPage(new { SelectedDate = InputData.NgayLamViec.ToString("yyyy-MM-dd"), ActiveTab = ActiveTab });
        }

        public async Task<IActionResult> OnPostDeleteAsync(string deleteId, string selectedDate, string activeTab)
        {
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
                AlertMessage = $"Lỗi khi xóa: {ex.Message}";
            }
            return RedirectToPage(new { SelectedDate = selectedDate, ActiveTab = activeTab });
        }

        public IActionResult OnPostCreateDonNghi()
        {
            DonNghiData.Id = DatabaseDonNghi.Any() ? DatabaseDonNghi.Max(x => x.Id) + 1 : 1;
            DonNghiData.TrangThai = "Chờ duyệt";
            DonNghiData.NgayTao = DateTime.Now;
            DonNghiData.TenNhanVien = User.Identity?.Name ?? "Tài khoản ẩn danh";
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            DonNghiData.ChucVu = roleClaim != null ? roleClaim.Value : "Nhân viên";

            DatabaseDonNghi.Add(DonNghiData);
            AlertMessage = "✅ Đã gửi đơn xin nghỉ phép thành công. Vui lòng chờ quản lý duyệt!";

            return RedirectToPage(new { SelectedDate = Request.Form["SelectedDate"], ActiveTab = Request.Form["ActiveTab"] });
        }

        public IActionResult OnPostDuyetDon(int donId, string actionType)
        {
            var don = DatabaseDonNghi.FirstOrDefault(x => x.Id == donId);
            if (don != null)
            {
                if (actionType == "Approve")
                {
                    don.TrangThai = "Đã duyệt";
                    AlertMessage = $"✅ Đã DUYỆT đơn xin nghỉ của nhân viên {don.TenNhanVien}!";
                }
                else if (actionType == "Reject")
                {
                    don.TrangThai = "Từ chối";
                    AlertMessage = $"❌ Đã TỪ CHỐI đơn xin nghỉ của {don.TenNhanVien}!";
                }
            }
            return RedirectToPage(new { SelectedDate = Request.Form["SelectedDate"], ActiveTab = Request.Form["ActiveTab"] });
        }

        // --- MỚI: HÀM ĐÁNH DẤU NHÂN VIÊN ĐÃ ĐỌC THÔNG BÁO ---
        public IActionResult OnPostMarkNotificationRead(int notifId)
        {
            var don = DatabaseDonNghi.FirstOrDefault(x => x.Id == notifId);
            if (don != null)
            {
                don.IsReadByEmployee = true;
            }
            return RedirectToPage(new { SelectedDate = Request.Form["SelectedDate"], ActiveTab = Request.Form["ActiveTab"] });
        }
    }

    public class DON_XIN_NGHI
    {
        public int Id { get; set; }
        public string MaNhanVien { get; set; } = string.Empty;
        public string TenNhanVien { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public int SoNgay { get; set; } = 1;
        public DateTime TuNgay { get; set; } = DateTime.Today;
        public DateTime DenNgay { get; set; } = DateTime.Today;
        public string LyDo { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "Chờ duyệt";
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Cờ đánh dấu để đếm thông báo cho nhân viên
        public bool IsReadByEmployee { get; set; } = false;
    }
}