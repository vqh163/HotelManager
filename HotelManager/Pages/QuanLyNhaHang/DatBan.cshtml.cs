using HotelManager.Data;
using HotelManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelManagement.Pages.NhaHang.PhucVu
{
    // ViewModel nhận dữ liệu từ Form Đặt Bàn (Chuẩn PascalCase)
    public class DatBanViewModel
    {
        public string MaBan { get; set; } = string.Empty;
        public string TenBan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng.")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số người.")]
        public int? SoNguoi { get; set; }

        // Tách thành Ngày đặt (Date) và Thời gian (Khung giờ cố định HH:mm từ 07:00 đến 22:00)
        [Required(ErrorMessage = "Vui lòng chọn ngày đặt bàn.")]
        [DataType(DataType.Date)]
        public DateTime NgayDat { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn thời gian đặt bàn.")]
        public TimeSpan ThoiGian { get; set; } = new TimeSpan(9, 0, 0);

        // Giữ nguyên NgayGioDat để tương thích hoàn toàn với dữ liệu lưu trữ
        public DateTime NgayGioDat { get; set; } = DateTime.Now;

        public bool LaKhachLuuTru { get; set; } = false;

        public string? SoPhong { get; set; }

        // Mục Yêu cầu đặc biệt (Không bắt buộc nhập)
        public string? YeuCauDacBiet { get; set; }
    }

    // Lưu trữ thông tin phụ trợ của Đặt bàn để đảm bảo không làm thay đổi cấu trúc bảng BAN_NHA_HANG trong DB
    public static class DatBanStore
    {
        private static string FilePath => Path.Combine(Directory.GetCurrentDirectory(), "DatBanData.json");
        public static Dictionary<string, DatBanViewModel> DuLieu { get; set; } = new Dictionary<string, DatBanViewModel>();

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, DatBanViewModel>>(json);
                    if (data != null) DuLieu = data;
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(DuLieu);
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
    }

    public class DatBanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DatBanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DatBanViewModel DatBanInfo { get; set; } = new DatBanViewModel();

        // Hàm hỗ trợ làm tròn thời gian về khung giờ 30 phút gần nhất trong khoảng 07:00 - 22:00
        private TimeSpan GetNearestTimeSlot(DateTime dt)
        {
            int hour = dt.Hour;
            int minute = dt.Minute < 15 ? 0 : (dt.Minute < 45 ? 30 : 0);
            if (dt.Minute >= 45) hour++;

            if (hour < 7) return new TimeSpan(7, 0, 0);
            if (hour >= 22) return new TimeSpan(22, 0, 0);
            return new TimeSpan(hour, minute, 0);
        }

        // Tải thông tin bàn và lịch đặt bàn hiện tại (nếu có)
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return RedirectToPage("./SoDoBan");

            try
            {
                DatBanStore.Load();

                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == id);
                if (ban == null) return RedirectToPage("./SoDoBan");

                // Nếu bàn đã có dữ liệu đặt trước đó thì nạp lại lên Form
                if (DatBanStore.DuLieu.ContainsKey(id))
                {
                    DatBanInfo = DatBanStore.DuLieu[id];
                    DatBanInfo.MaBan = ban.MaBan;
                    DatBanInfo.TenBan = ban.TenBan ?? ban.MaBan;

                    // Đồng bộ lại Ngày đặt và Thời gian (khớp khung 30 phút) từ NgayGioDat nếu dữ liệu cũ đã lưu
                    if (DatBanInfo.NgayGioDat != default)
                    {
                        DatBanInfo.NgayDat = DatBanInfo.NgayGioDat.Date;
                        DatBanInfo.ThoiGian = GetNearestTimeSlot(DatBanInfo.NgayGioDat);
                    }
                }
                else
                {
                    string? tenKhachDb = ban.GetType().GetProperty("TenKhachDat")?.GetValue(ban)?.ToString();
                    string? soPhongDb = ban.GetType().GetProperty("SoPhongDat")?.GetValue(ban)?.ToString();
                    DateTime defaultTime = DateTime.Now.AddMinutes(30);
                    TimeSpan defaultSlot = GetNearestTimeSlot(defaultTime);

                    DatBanInfo = new DatBanViewModel
                    {
                        MaBan = ban.MaBan,
                        TenBan = ban.TenBan ?? ban.MaBan,
                        HoTen = tenKhachDb ?? string.Empty,
                        SoNguoi = int.TryParse(ban.SucChua?.ToString(), out int sc) ? sc : 2,
                        NgayDat = defaultTime.Date,
                        ThoiGian = defaultSlot,
                        NgayGioDat = defaultTime.Date.Add(defaultSlot),
                        LaKhachLuuTru = !string.IsNullOrEmpty(soPhongDb),
                        SoPhong = soPhongDb,
                        YeuCauDacBiet = string.Empty
                    };
                }

                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi tải thông tin đặt bàn: " + ex.Message;
                return RedirectToPage("./SoDoBan");
            }
        }

        // Xử lý khi bấm nút "Lưu thông tin"
        public async Task<IActionResult> OnPostLuuAsync()
        {
            // Yêu cầu đặc biệt không bắt buộc nhập nên loại bỏ khỏi kiểm tra ModelState
            ModelState.Remove("DatBanInfo.YeuCauDacBiet");

            // Nếu là khách vãng lai thì bỏ qua kiểm tra Số phòng
            if (!DatBanInfo.LaKhachLuuTru)
            {
                DatBanInfo.SoPhong = null;
                ModelState.Remove("DatBanInfo.SoPhong");
            }
            else if (string.IsNullOrWhiteSpace(DatBanInfo.SoPhong))
            {
                ModelState.AddModelError("DatBanInfo.SoPhong", "Vui lòng nhập số phòng cho khách đang lưu trú.");
            }

            if (!ModelState.IsValid) return Page();

            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == DatBanInfo.MaBan);
                if (ban == null) return RedirectToPage("./SoDoBan");

                // Ghép Ngày đặt và Thời gian (HH:mm) thành NgayGioDat
                DatBanInfo.NgayGioDat = DatBanInfo.NgayDat.Date.Add(new TimeSpan(DatBanInfo.ThoiGian.Hours, DatBanInfo.ThoiGian.Minutes, 0));

                // Cập nhật trạng thái bàn sang "Đã đặt trước" để Sơ đồ bàn đổi sang màu xanh dương nhạt
                ban.TrangThai = "Đã đặt trước";

                // Gán thông tin khách đặt vào Entity BAN_NHA_HANG (nếu Model C# có thuộc tính này)
                if (ban.GetType().GetProperty("TenKhachDat") != null)
                {
                    ban.GetType().GetProperty("TenKhachDat")?.SetValue(ban, DatBanInfo.HoTen.Trim());
                }
                if (ban.GetType().GetProperty("SoPhongDat") != null)
                {
                    ban.GetType().GetProperty("SoPhongDat")?.SetValue(ban, DatBanInfo.LaKhachLuuTru ? DatBanInfo.SoPhong?.Trim() : null);
                }

                // Lưu chi tiết đặt bàn (Họ tên, SĐT, Số người, Ngày đặt, Thời gian, Số phòng, Yêu cầu đặc biệt)
                DatBanStore.Load();
                DatBanStore.DuLieu[DatBanInfo.MaBan] = DatBanInfo;
                DatBanStore.Save();

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đặt bàn {DatBanInfo.TenBan} cho khách {DatBanInfo.HoTen} ({DatBanInfo.NgayGioDat:dd/MM/yyyy HH:mm}) thành công!";
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi lưu đặt bàn: " + ex.Message);
                return Page();
            }
        }

        // Xử lý khi bấm nút "Hủy đặt bàn"
        public async Task<IActionResult> OnPostHuyDatBanAsync()
        {
            try
            {
                var ban = await _context.BAN_NHA_HANG.FirstOrDefaultAsync(b => b.MaBan == DatBanInfo.MaBan);
                if (ban != null)
                {
                    // Trả trạng thái bàn về "Trống"
                    ban.TrangThai = "Trống";

                    if (ban.GetType().GetProperty("TenKhachDat") != null)
                    {
                        ban.GetType().GetProperty("TenKhachDat")?.SetValue(ban, null);
                    }
                    if (ban.GetType().GetProperty("SoPhongDat") != null)
                    {
                        ban.GetType().GetProperty("SoPhongDat")?.SetValue(ban, null);
                    }

                    await _context.SaveChangesAsync();
                }

                // Xóa thông tin đặt bàn đã lưu
                DatBanStore.Load();
                if (DatBanStore.DuLieu.ContainsKey(DatBanInfo.MaBan))
                {
                    DatBanStore.DuLieu.Remove(DatBanInfo.MaBan);
                    DatBanStore.Save();
                }

                TempData["SuccessMessage"] = $"Đã hủy đặt bàn {DatBanInfo.TenBan}, bàn đã trở về trạng thái Trống!";
                return RedirectToPage("./SoDoBan");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi hủy đặt bàn: " + ex.Message);
                return Page();
            }
        }
    }
}