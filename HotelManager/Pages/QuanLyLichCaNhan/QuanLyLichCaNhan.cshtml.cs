using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "month";

        [TempData]
        public string? AlertMessage { get; set; }

        public string EmployeeDataJson { get; set; } = "{}";

        // ==========================================
        // 1. CÁC THUỘC TÍNH RENDER GIAO DIỆN LỊCH (Fix lỗi CS1061)
        // ==========================================
        public DateTime CurrentDate { get; set; }
        public int DaysInMonth { get; set; }
        public int StartDayOfWeek { get; set; }

        // ==========================================
        // 2. VIEWMODEL TRUNG GIAN CHO GIAO DIỆN 
        // ==========================================
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

        public List<ScheduleViewModel> MySchedules { get; set; } = new List<ScheduleViewModel>();

        // ==========================================
        // 3. VIEWMODEL HỨNG DỮ LIỆU TỪ FORM TẠO MỚI
        // ==========================================
        public class InputScheduleViewModel
        {
            public DateTime NgayLamViec { get; set; } = DateTime.Today;
            public string ChucVu { get; set; } = string.Empty;
            public string TenNhanVien { get; set; } = string.Empty;
            public string MaNhanVien { get; set; } = string.Empty;
            public string CaLamViec { get; set; } = "CaSang";

            // Hai biến mới để hứng giờ tùy chọn (nếu có)
            public string? TuChonBatDau { get; set; }
            public string? TuChonKetThuc { get; set; }

            public string TieuDe { get; set; } = string.Empty;
            public string Checklist { get; set; } = string.Empty;
            public string GhiChu { get; set; } = string.Empty;
        }

        [BindProperty]
        public InputScheduleViewModel InputData { get; set; } = new InputScheduleViewModel();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Logic 1: Tính toán ngày tháng cho Calendar
                DateTime refDate = SelectedDate ?? DateTime.Today;
                SelectedDate = refDate;
                CurrentDate = new DateTime(refDate.Year, refDate.Month, 1);
                DaysInMonth = DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month);
                StartDayOfWeek = CurrentDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)CurrentDate.DayOfWeek - 1;

                // Logic 2: Lấy dữ liệu lịch làm việc & Map sang ViewModel
                var rawSchedules = await _context.LICH_LAM_VIEC.AsNoTracking().ToListAsync();
                foreach (var item in rawSchedules)
                {
                    var vm = new ScheduleViewModel
                    {
                        MaLich = item.MaLich,
                        CaLamViec = item.CaLamViec ?? "Ca sáng",
                        GhiChu = item.GhiChu ?? "",
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
                        }
                        catch { }
                    }
                    // Đọc và xử lý an toàn Ca làm việc
                    string dbCaLam = item.CaLamViec ?? "CaSang";
                    DateTime baseDate = item.NgayLamViec ?? DateTime.Today;
                    baseDate = baseDate.Date; // Đảm bảo luôn lấy mốc 0h00 của ngày

                    if (dbCaLam == "CaSang")
                    {
                        vm.CaLamViec = "Ca sáng";
                        vm.ThoiGianBatDau = baseDate.AddHours(6);
                        vm.ThoiGianKetThuc = baseDate.AddHours(14);
                    }
                    else if (dbCaLam == "CaChieu")
                    {
                        vm.CaLamViec = "Ca chiều";
                        vm.ThoiGianBatDau = baseDate.AddHours(14);
                        vm.ThoiGianKetThuc = baseDate.AddHours(22);
                    }
                    else if (dbCaLam == "CaDem")
                    {
                        vm.CaLamViec = "Ca đêm";
                        vm.ThoiGianBatDau = baseDate.AddHours(22);
                        vm.ThoiGianKetThuc = baseDate.AddDays(1).AddHours(6);
                    }
                    else if (dbCaLam == "CaHanhChinh")
                    {
                        vm.CaLamViec = "Ca hành chính";
                        vm.ThoiGianBatDau = baseDate.AddHours(8);
                        vm.ThoiGianKetThuc = baseDate.AddHours(17);
                    }
                    else if (dbCaLam.StartsWith("Khac"))
                    {
                        vm.CaLamViec = "Khác (Tùy chọn)";
                        try
                        {
                            // Cố gắng bóc tách chuỗi Khac|HH:mm|HH:mm
                            var parts = dbCaLam.Split('|');
                            if (parts.Length == 3 && TimeSpan.TryParse(parts[1], out var start) && TimeSpan.TryParse(parts[2], out var end))
                            {
                                vm.ThoiGianBatDau = baseDate.Add(start);
                                vm.ThoiGianKetThuc = baseDate.Add(end);

                                // Nếu giờ kết thúc nhỏ hơn giờ bắt đầu (vd làm xuyên đêm), tự động cộng thêm 1 ngày
                                if (vm.ThoiGianKetThuc <= vm.ThoiGianBatDau)
                                {
                                    vm.ThoiGianKetThuc = vm.ThoiGianKetThuc.AddDays(1);
                                }
                            }
                            else
                            {
                                // Dữ liệu lỗi định dạng, đưa về mặc định
                                vm.ThoiGianBatDau = baseDate.AddHours(8);
                                vm.ThoiGianKetThuc = baseDate.AddHours(17);
                            }
                        }
                        catch
                        {
                            vm.ThoiGianBatDau = baseDate.AddHours(8);
                            vm.ThoiGianKetThuc = baseDate.AddHours(17);
                        }
                    }
                    else
                    {
                        vm.CaLamViec = "Chưa rõ";
                        vm.ThoiGianBatDau = baseDate.AddHours(8);
                        vm.ThoiGianKetThuc = baseDate.AddHours(17);
                    }
                }

                MySchedules = MySchedules.OrderBy(x => x.ThoiGianBatDau).ToList();

                // Logic 3: Tạm thời sử dụng Mock Data cho đến khi UC_11 (Quản lý Nhân sự) hoàn thiện DB ở Sprint 5
                var mockEmployees = new List<dynamic> {
                    new { MaNV = "LT001", HoTen = "Nguyễn Thu Hà", ChucVu = "Lễ tân" },
                    new { MaNV = "LT002", HoTen = "Trần Bảo Ngọc", ChucVu = "Lễ tân" },
                    new { MaNV = "BP001", HoTen = "Lê Văn Hùng", ChucVu = "Buồng phòng" },
                    new { MaNV = "KT001", HoTen = "Phạm Kế Toán", ChucVu = "Kế toán" },
                    new { MaNV = "B001", HoTen = "Võ Đầu Bếp", ChucVu = "Đầu bếp" },
                    new { MaNV = "S001", HoTen = "Đoàn Sales", ChucVu = "Sales" }
                };

                var groupedEmployees = mockEmployees
                    .GroupBy(x => x.ChucVu)
                    .ToDictionary(g => g.Key, g => g.Select(nv => new { id = nv.MaNV, name = nv.HoTen }).ToList());

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
                // Gộp giờ nếu user chọn Ca tùy chỉnh
                string finalCaLamViec = InputData.CaLamViec;
                if (finalCaLamViec == "Khac" && !string.IsNullOrEmpty(InputData.TuChonBatDau) && !string.IsNullOrEmpty(InputData.TuChonKetThuc))
                {
                    finalCaLamViec = $"Khac|{InputData.TuChonBatDau}|{InputData.TuChonKetThuc}";
                }

                string noiDungGom = $"[{InputData.TieuDe}] NV: {InputData.TenNhanVien} ({InputData.MaNhanVien}). Ghi chú: {InputData.GhiChu}. Checklist: {InputData.Checklist}";

                var newSchedule = new LICH_LAM_VIEC
                {
                    MaLich = "LLV" + DateTime.Now.Ticks.ToString().Substring(10),
                    NgayLamViec = InputData.NgayLamViec,
                    CaLamViec = finalCaLamViec, // Đã được xử lý
                    GhiChu = noiDungGom,
                    TrangThai = "Chưa bắt đầu"
                };
            }
            catch (Exception ex)
            {
                // Đảm bảo an toàn hệ thống thông qua Try...Catch
                AlertMessage = $"Lỗi khi lưu: {ex.Message}";
            }

            return RedirectToPage(new { SelectedDate = InputData.NgayLamViec.ToString("yyyy-MM-dd"), ActiveTab = ActiveTab });
        }
        // ==========================================
        // 4. XỬ LÝ XÓA LỊCH LÀM VIỆC (UC_10)
        // ==========================================z
        public async Task<IActionResult> OnPostDeleteAsync(string deleteId, string selectedDate, string activeTab)
        {
            try
            {
                // Truy vấn lịch làm việc theo ID từ CSDL
                var schedule = await _context.LICH_LAM_VIEC.FirstOrDefaultAsync(x => x.MaLich == deleteId);

                if (schedule != null)
                {
                    // Xóa và lưu thay đổi
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

            // QUAN TRỌNG: Bắt buộc dùng RedirectToPage để vòng đời Razor Pages chạy lại hàm OnGetAsync() 
            // Điều này giúp khởi tạo lại CurrentDate và giữ nguyên Tab/Ngày người dùng đang xem
            return RedirectToPage(new { SelectedDate = selectedDate, ActiveTab = activeTab });
        }
    }
}