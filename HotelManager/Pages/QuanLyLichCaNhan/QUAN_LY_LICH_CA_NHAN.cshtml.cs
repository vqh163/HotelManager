using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotelManager.Pages
{
    public class ScheduleModel : PageModel
    {
        public string EmployeeName { get; set; } = "D";
        public DateTime CurrentDate { get; set; } = DateTime.Today;

        // Dùng 'static' để mô phỏng Database (giữ dữ liệu không bị mất khi load lại trang)
        public static List<LICH_LAM_VIEC> DatabaseMock = new List<LICH_LAM_VIEC>();

        // Biến để hiển thị dữ liệu ra giao diện
        public List<LICH_LAM_VIEC> MySchedules { get; set; } = new List<LICH_LAM_VIEC>();
        public int DaysInMonth { get; set; }
        public int StartDayOfWeek { get; set; } // 0 = Thứ 2, 6 = Chủ Nhật

        // Biến để nhận dữ liệu từ Modal Form tạo lịch mới
        [BindProperty]
        public LICH_LAM_VIEC NewSchedule { get; set; } = new LICH_LAM_VIEC();

        public void OnGet()
        {
            // Nếu Database ảo trống, tạo sẵn 1 lịch mẫu của ngày hôm nay
            if (DatabaseMock.Count == 0)
            {
                DatabaseMock.Add(new LICH_LAM_VIEC
                {
                    Id = 1,
                    Title = "Trực Sảnh Chính",
                    ShiftType = "Ca Sáng",
                    AssignedArea = "Tầng G - Vị trí A1",
                    StartTime = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, 6, 0, 0),
                    EndTime = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, 14, 0, 0),
                    Status = "Đang thực hiện"
                });
            }

            MySchedules = DatabaseMock.OrderBy(x => x.StartTime).ToList();

            // Tính toán để vẽ Lịch cho tháng hiện tại
            DaysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);
            DateTime firstDayOfMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
            // Quy đổi: Chủ nhật (0) -> 6, Thứ 2 (1) -> 0... để phù hợp với lịch bắt đầu từ Thứ 2
            StartDayOfWeek = firstDayOfMonth.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)firstDayOfMonth.DayOfWeek - 1;
        }

        // Hàm xử lý khi bấm nút "Lưu Phân Công" trên Modal
        public IActionResult OnPostCreate()
        {
            // Tự động tăng ID
            NewSchedule.Id = DatabaseMock.Any() ? DatabaseMock.Max(x => x.Id) + 1 : 1;
            NewSchedule.Status = "Sắp diễn ra";

            // Lưu vào Database ảo
            DatabaseMock.Add(NewSchedule);

            // Tải lại trang để Lịch cập nhật
            return RedirectToPage();
        }
    }
}