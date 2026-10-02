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
        public int? ViewMonth { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? ViewYear { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public DateTime CurrentDate { get; set; }

        public static List<LICH_LAM_VIEC> DatabaseMock = new List<LICH_LAM_VIEC>();
        public List<LICH_LAM_VIEC> MySchedules { get; set; } = new List<LICH_LAM_VIEC>();

        public int DaysInMonth { get; set; }
        public int StartDayOfWeek { get; set; }

        [BindProperty]
        public LICH_LAM_VIEC ScheduleData { get; set; } = new LICH_LAM_VIEC();

        [TempData]
        public string AlertMessage { get; set; }

        public void OnGet()
        {
            int targetMonth = ViewMonth ?? DateTime.Today.Month;
            int targetYear = ViewYear ?? DateTime.Today.Year;
            CurrentDate = new DateTime(targetYear, targetMonth, 1);

            ScheduleData.ThoiGianBatDau = DateTime.Today.AddHours(6);
            ScheduleData.ThoiGianKetThuc = DateTime.Today.AddHours(14);

            MySchedules = DatabaseMock.OrderBy(x => x.ThoiGianBatDau).ToList();

            DaysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);
            StartDayOfWeek = CurrentDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)CurrentDate.DayOfWeek - 1;
        }

        public IActionResult OnPostCreate()
        {
            if (ScheduleData.ThoiGianBatDau.Date < DateTime.Today)
            {
                AlertMessage = "⚠️ Lỗi: Không được chọn ngày đã qua! Vui lòng chọn từ ngày hôm nay trở đi.";
                return RedirectToPage(new { ViewMonth = DateTime.Today.Month, ViewYear = DateTime.Today.Year });
            }

            if (ScheduleData.ThoiGianKetThuc <= ScheduleData.ThoiGianBatDau)
            {
                AlertMessage = "⚠️ Lỗi: Giờ kết thúc phải lớn hơn giờ bắt đầu. Vui lòng chọn lại!";
                return RedirectToPage(new { ViewMonth = DateTime.Today.Month, ViewYear = DateTime.Today.Year });
            }

            var conflict = DatabaseMock.FirstOrDefault(x =>
                ScheduleData.ThoiGianBatDau < x.ThoiGianKetThuc &&
                ScheduleData.ThoiGianKetThuc > x.ThoiGianBatDau);

            if (conflict != null)
            {
                AlertMessage = $"⚠️ Bị trùng giờ với ca: {conflict.TenNhanVien} ({conflict.ThoiGianBatDau:HH:mm} - {conflict.ThoiGianKetThuc:HH:mm}). Vui lòng chọn lại!";
                return RedirectToPage(new { ViewMonth = ScheduleData.ThoiGianBatDau.Month, ViewYear = ScheduleData.ThoiGianBatDau.Year });
            }

            ScheduleData.Id = DatabaseMock.Any() ? DatabaseMock.Max(x => x.Id) + 1 : 1;
            if (string.IsNullOrEmpty(ScheduleData.TrangThai)) ScheduleData.TrangThai = "Chưa bắt đầu";

            DatabaseMock.Add(ScheduleData);
            AlertMessage = "✅ Đã phân công ca làm việc thành công!";
            return RedirectToPage(new { ViewMonth = ScheduleData.ThoiGianBatDau.Month, ViewYear = ScheduleData.ThoiGianBatDau.Year });
        }

        public IActionResult OnPostEdit()
        {
            if (ScheduleData.ThoiGianKetThuc <= ScheduleData.ThoiGianBatDau)
            {
                AlertMessage = "⚠️ Lỗi: Giờ kết thúc phải lớn hơn giờ bắt đầu. Vui lòng chọn lại!";
                return RedirectToPage(new { ViewMonth = ScheduleData.ThoiGianBatDau.Month, ViewYear = ScheduleData.ThoiGianBatDau.Year });
            }

            var conflict = DatabaseMock.FirstOrDefault(x =>
                x.Id != ScheduleData.Id &&
                ScheduleData.ThoiGianBatDau < x.ThoiGianKetThuc &&
                ScheduleData.ThoiGianKetThuc > x.ThoiGianBatDau);

            if (conflict != null)
            {
                AlertMessage = $"⚠️ Bị trùng giờ với ca: {conflict.TenNhanVien} ({conflict.ThoiGianBatDau:HH:mm} - {conflict.ThoiGianKetThuc:HH:mm}). Vui lòng chọn lại!";
                return RedirectToPage(new { ViewMonth = ScheduleData.ThoiGianBatDau.Month, ViewYear = ScheduleData.ThoiGianBatDau.Year });
            }

            var item = DatabaseMock.FirstOrDefault(x => x.Id == ScheduleData.Id);
            if (item != null)
            {
                item.TenNhanVien = ScheduleData.TenNhanVien;
                item.MaNhanVien = ScheduleData.MaNhanVien;
                item.BoPhan = ScheduleData.BoPhan;
                item.ChucVu = ScheduleData.ChucVu;
                item.LoaiCa = ScheduleData.LoaiCa;
                item.KhuVuc = ScheduleData.KhuVuc;
                item.ThoiGianBatDau = ScheduleData.ThoiGianBatDau;
                item.ThoiGianKetThuc = ScheduleData.ThoiGianKetThuc;
                item.TieuDe = ScheduleData.TieuDe;
                item.GhiChu = ScheduleData.GhiChu;
                item.Checklist = ScheduleData.Checklist;
                item.VatTu = ScheduleData.VatTu;
                item.DoUuTien = ScheduleData.DoUuTien;

                AlertMessage = "✅ Cập nhật thông tin ca làm việc thành công!";
            }
            return RedirectToPage(new { ViewMonth = ScheduleData.ThoiGianBatDau.Month, ViewYear = ScheduleData.ThoiGianBatDau.Year });
        }

        public IActionResult OnPostDelete(int id, int month, int year)
        {
            var item = DatabaseMock.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                DatabaseMock.Remove(item);
                AlertMessage = "🗑 Đã xóa ca làm việc khỏi hệ thống!";
            }
            return RedirectToPage(new { ViewMonth = month, ViewYear = year });
        }
    }

    public class LICH_LAM_VIEC
    {
        public int Id { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string MaNhanVien { get; set; } = string.Empty;
        public string BoPhan { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;
        public string LoaiCa { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; } = DateTime.Today.AddHours(6);
        public DateTime ThoiGianKetThuc { get; set; } = DateTime.Today.AddHours(14);
        public string KhuVuc { get; set; } = string.Empty;
        public string TieuDe { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public string Checklist { get; set; } = string.Empty;
        public string VatTu { get; set; } = string.Empty;
        public string DoUuTien { get; set; } = "Bình thường";
        public string TrangThai { get; set; } = "Chưa bắt đầu";
        public string HinhAnh { get; set; } = string.Empty;
    }
}