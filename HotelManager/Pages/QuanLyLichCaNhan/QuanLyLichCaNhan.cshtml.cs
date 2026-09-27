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

            MySchedules = DatabaseMock.OrderBy(x => x.ThoiGianBatDau).ToList();

            DaysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);
            StartDayOfWeek = CurrentDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)CurrentDate.DayOfWeek - 1;
        }

        // 1. Thêm lịch mới
        public IActionResult OnPostCreate()
        {
            ScheduleData.Id = DatabaseMock.Any() ? DatabaseMock.Max(x => x.Id) + 1 : 1;
            ScheduleData.TrangThai = "Sắp diễn ra";
            DatabaseMock.Add(ScheduleData);

            AlertMessage = "✅ Đã phân công ca làm việc thành công!";
            return RedirectToPage(new { ViewMonth = ScheduleData.ThoiGianBatDau.Month, ViewYear = ScheduleData.ThoiGianBatDau.Year });
        }

        // 2. Sửa thông tin lịch
        public IActionResult OnPostEdit()
        {
            var item = DatabaseMock.FirstOrDefault(x => x.Id == ScheduleData.Id);
            if (item != null)
            {
                item.TenNhanVien = ScheduleData.TenNhanVien;
                item.MaNhanVien = ScheduleData.MaNhanVien;
                item.BoPhan = ScheduleData.BoPhan;
                item.ChucVu = ScheduleData.ChucVu;
                item.TieuDe = ScheduleData.TieuDe;
                item.LoaiCa = ScheduleData.LoaiCa;
                item.KhuVuc = ScheduleData.KhuVuc;
                item.ThoiGianBatDau = ScheduleData.ThoiGianBatDau;
                item.ThoiGianKetThuc = ScheduleData.ThoiGianKetThuc;
                item.GhiChu = ScheduleData.GhiChu;

                AlertMessage = "✅ Cập nhật thông tin ca làm việc thành công!";
            }
            return RedirectToPage(new { ViewMonth = ScheduleData.ThoiGianBatDau.Month, ViewYear = ScheduleData.ThoiGianBatDau.Year });
        }

        // 3. Xóa lịch
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

    // Class Model chứa dữ liệu lịch làm việc
    public class LICH_LAM_VIEC
    {
        public int Id { get; set; }

        // Thông tin nhân viên
        public string MaNhanVien { get; set; } = string.Empty;
        public string TenNhanVien { get; set; } = string.Empty;
        public string BoPhan { get; set; } = string.Empty;
        public string ChucVu { get; set; } = string.Empty;

        // Thông tin ca làm việc
        public string TieuDe { get; set; } = string.Empty;
        public string LoaiCa { get; set; } = string.Empty;
        public string KhuVuc { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string TrangThai { get; set; } = "Sắp diễn ra";

        // Chi tiết công việc
        public string GhiChu { get; set; } = string.Empty;
    }
}