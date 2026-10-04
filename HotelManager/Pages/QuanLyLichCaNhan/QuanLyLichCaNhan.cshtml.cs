using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotelManager.Pages.QuanLyLichCaNhan
{
    public class QuanLyLichCaNhanModel : PageModel
    {
        public string EmployeeName { get; set; } = "Lê Thị D";

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "month";

        public DateTime CurrentDate { get; set; }

        public static List<LICH_LAM_VIEC> DatabaseMock = new List<LICH_LAM_VIEC>();
        public List<LICH_LAM_VIEC> MySchedules { get; set; } = new List<LICH_LAM_VIEC>();

        public int DaysInMonth { get; set; }
        public int StartDayOfWeek { get; set; }

        [BindProperty]
        public LICH_LAM_VIEC ScheduleData { get; set; } = new LICH_LAM_VIEC();

        [BindProperty]
        public DateTime InputNgayLamViec { get; set; } = DateTime.Today;

        // Biến hứng giờ tùy chỉnh khi chọn ca "Khác"
        [BindProperty]
        public string CustomStartTime { get; set; } = "06:00";
        [BindProperty]
        public string CustomEndTime { get; set; } = "14:00";

        [TempData]
        public string AlertMessage { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");
            if (!string.IsNullOrEmpty(User.Identity.Name)) EmployeeName = User.Identity.Name;

            if (User.IsInRole("Quản trị viên"))
            {
                TempData["ThongBaoLoi"] = "Quản trị viên không tham gia vào lịch vận hành.";
                return RedirectToPage("/QuanTriHeThong/QuanLyTaiKhoan");
            }

            DateTime refDate = SelectedDate ?? DateTime.Today;
            SelectedDate = refDate;
            CurrentDate = new DateTime(refDate.Year, refDate.Month, 1);

            MySchedules = DatabaseMock.OrderBy(x => x.ThoiGianBatDau).ToList();

            DaysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);
            StartDayOfWeek = CurrentDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)CurrentDate.DayOfWeek - 1;

            return Page();
        }

        private void CalculateShiftTimes()
        {
            if (ScheduleData.LoaiCa == "Ca sáng")
            {
                ScheduleData.ThoiGianBatDau = InputNgayLamViec.Date.AddHours(6);
                ScheduleData.ThoiGianKetThuc = InputNgayLamViec.Date.AddHours(14);
            }
            else if (ScheduleData.LoaiCa == "Ca chiều")
            {
                ScheduleData.ThoiGianBatDau = InputNgayLamViec.Date.AddHours(14);
                ScheduleData.ThoiGianKetThuc = InputNgayLamViec.Date.AddHours(22);
            }
            else if (ScheduleData.LoaiCa == "Ca đêm")
            {
                ScheduleData.ThoiGianBatDau = InputNgayLamViec.Date.AddHours(22);
                ScheduleData.ThoiGianKetThuc = InputNgayLamViec.Date.AddDays(1).AddHours(6);
            }
            else if (ScheduleData.LoaiCa == "Khác")
            {
                // Xử lý ghép ngày làm việc với giờ tùy chọn do người dùng chọn
                if (TimeSpan.TryParse(CustomStartTime, out TimeSpan startTs) && TimeSpan.TryParse(CustomEndTime, out TimeSpan endTs))
                {
                    ScheduleData.ThoiGianBatDau = InputNgayLamViec.Date.Add(startTs);
                    ScheduleData.ThoiGianKetThuc = InputNgayLamViec.Date.Add(endTs);

                    // Nếu giờ kết thúc nhỏ hơn hoặc bằng giờ bắt đầu thì tự động đẩy sang ngày hôm sau (Ví dụ: 22:00 đến 02:00 sáng hôm sau)
                    if (ScheduleData.ThoiGianKetThuc <= ScheduleData.ThoiGianBatDau)
                    {
                        ScheduleData.ThoiGianKetThuc = ScheduleData.ThoiGianKetThuc.AddDays(1);
                    }
                }
            }
        }

        public IActionResult OnPostCreate()
        {
            if (InputNgayLamViec.Date < DateTime.Today)
            {
                AlertMessage = "⚠️ Lỗi: Không được chọn ngày đã qua!";
                return RedirectToPage(new { SelectedDate = InputNgayLamViec, ActiveTab = ActiveTab });
            }

            CalculateShiftTimes();

            // ĐÃ XÓA ĐOẠN IF CHẶN LỖI CA "KHÁC" Ở ĐÂY

            var conflict = DatabaseMock.FirstOrDefault(x =>
                x.MaNhanVien == ScheduleData.MaNhanVien &&
                ScheduleData.ThoiGianBatDau < x.ThoiGianKetThuc &&
                ScheduleData.ThoiGianKetThuc > x.ThoiGianBatDau);

            if (conflict != null)
            {
                AlertMessage = $"⚠️ Nhân viên {conflict.TenNhanVien} đã có ca làm việc trùng lặp trong khung giờ này!";
                return RedirectToPage(new { SelectedDate = InputNgayLamViec, ActiveTab = ActiveTab });
            }

            ScheduleData.Id = DatabaseMock.Any() ? DatabaseMock.Max(x => x.Id) + 1 : 1;
            ScheduleData.TrangThai = "Chưa bắt đầu";

            DatabaseMock.Add(ScheduleData);
            AlertMessage = "✅ Đã phân công ca làm việc thành công!";
            return RedirectToPage(new { SelectedDate = InputNgayLamViec, ActiveTab = ActiveTab });
        }

        public IActionResult OnPostEdit()
        {
            CalculateShiftTimes();

            var conflict = DatabaseMock.FirstOrDefault(x =>
                x.Id != ScheduleData.Id &&
                x.MaNhanVien == ScheduleData.MaNhanVien &&
                ScheduleData.ThoiGianBatDau < x.ThoiGianKetThuc &&
                ScheduleData.ThoiGianKetThuc > x.ThoiGianBatDau);

            if (conflict != null)
            {
                AlertMessage = $"⚠️ Nhân viên {conflict.TenNhanVien} đã có ca làm việc trùng lặp trong khung giờ này!";
                return RedirectToPage(new { SelectedDate = InputNgayLamViec, ActiveTab = ActiveTab });
            }

            var item = DatabaseMock.FirstOrDefault(x => x.Id == ScheduleData.Id);
            if (item != null)
            {
                item.TenNhanVien = ScheduleData.TenNhanVien;
                item.MaNhanVien = ScheduleData.MaNhanVien;
                item.ChucVu = ScheduleData.ChucVu;
                item.LoaiCa = ScheduleData.LoaiCa;
                item.ThoiGianBatDau = ScheduleData.ThoiGianBatDau;
                item.ThoiGianKetThuc = ScheduleData.ThoiGianKetThuc;
                item.TieuDe = ScheduleData.TieuDe;
                item.GhiChu = ScheduleData.GhiChu;
                item.Checklist = ScheduleData.Checklist;

                AlertMessage = "✅ Cập nhật thông tin ca làm việc thành công!";
            }
            return RedirectToPage(new { SelectedDate = InputNgayLamViec, ActiveTab = ActiveTab });
        }

        public IActionResult OnPostDelete(int deleteId, DateTime selectedDate, string activeTab)
        {
            var item = DatabaseMock.FirstOrDefault(x => x.Id == deleteId);
            if (item != null)
            {
                DatabaseMock.Remove(item);
                AlertMessage = "🗑 Đã xóa ca làm việc khỏi hệ thống!";
            }
            return RedirectToPage(new { SelectedDate = selectedDate, ActiveTab = activeTab });
        }
    }

    public class LICH_LAM_VIEC
    {
        public int Id { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string MaNhanVien { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public string LoaiCa { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public string Checklist { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "Chưa bắt đầu";

        public string TrangThaiHienTai
        {
            get
            {
                if (TrangThai == "Chưa bắt đầu" && DateTime.Now >= ThoiGianBatDau && DateTime.Now <= ThoiGianKetThuc)
                {
                    return "Đang hoạt động";
                }
                return TrangThai;
            }
        }
    }
}