using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyTienSanh
{
    // Bắt buộc phân quyền: Chỉ Lễ tân và Quản lý mới được truy cập phân hệ UC_04
    [Authorize(Roles = "Lễ tân,Quản lý")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Biến lưu trữ thông báo lỗi/thành công trên giao diện
        [TempData]
        public string? AlertMessage { get; set; }

        // === BIẾN LƯU TRỮ SỐ LƯỢNG KHÁCH VÀ PHÒNG TRONG NGÀY (UC_04) ===
        // Số lượng khách dự kiến đến/đi hoặc khách ở cùng chưa trình diện CCCD
        public int SoKhachDangChoCheckIn { get; set; }
        public int SoKhachDangChoCheckOut { get; set; }

        // Số lượng khách THỰC TẾ Lễ tân đã bấm Check-in / Check-out trong ngày hôm nay (Hiển thị số lớn chính diện)
        // Đầu ngày mới khi chưa có thao tác sẽ luôn bằng 0
        public int SoKhachDaCheckIn { get; set; }
        public int SoKhachDaCheckOut { get; set; }

        // Thống kê số phòng đang có khách nhưng chưa khai báo đủ CCCD/Hộ chiếu của người ở cùng
        public int SoPhongChuaDuKhach { get; set; }

        // Tổng số phòng vật lý đang có người ở thực tế (Trạng thái DANG_O)
        public int TongPhongDangLuuTru { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                DateTime today = DateTime.Now.Date;

                // =========================================================================
                // 1. THỐNG KÊ KHÁCH ĐẾN HÔM NAY (ARRIVALS - HỖ TRỢ CHECK-IN TỪNG PHẦN)
                // =========================================================================
                var arrivalsToday = await _context.DAT_PHONG.AsNoTracking()
                    .Where(dp => dp.NgayNhan.HasValue && dp.NgayNhan.Value.Date == today)
                    .ToListAsync();

                // 1.1. Số khách của các đơn đặt phòng hẹn hôm nay nhưng CHƯA mở phòng
                int khachChuaMoPhong = arrivalsToday
                    .Where(dp => dp.TrangThaiDatPhong == "DA_DAT" || dp.TrangThaiDatPhong == "Chờ xác nhận")
                    .Sum(dp => dp.SoLuongKhach ?? 0);

                // 1.2. Đếm số lượng hành khách THỰC TẾ đã trình diện CCCD/Hộ chiếu và Check-in hôm nay trong PHIEU_LUU_TRU
                int soPhieuCheckInHomNay = await _context.PHIEU_LUU_TRU.AsNoTracking()
                    .CountAsync(p => p.ThoiGianCheckIn.HasValue && p.ThoiGianCheckIn.Value.Date == today);

                if (soPhieuCheckInHomNay > 0)
                {
                    // Nếu đã có phiếu lưu trú trong ngày -> Lấy chính xác theo từng người đã làm thủ tục
                    SoKhachDaCheckIn = soPhieuCheckInHomNay;
                }
                else
                {
                    // Fallback tương thích dữ liệu cũ (nếu có đơn DANG_O trong ngày trước khi áp dụng PHIEU_LUU_TRU)
                    SoKhachDaCheckIn = arrivalsToday
                        .Where(dp => dp.TrangThaiDatPhong == "DANG_O" || dp.TrangThaiDatPhong == "Đã check-out")
                        .Sum(dp => dp.SoLuongKhach ?? 0);
                }

                // 1.3. Tính số khách ở cùng (Companion) còn thiếu của các phòng đang mở (Check-in chưa đủ người)
                var activeRoomBookings = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                                join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                where dp.TrangThaiDatPhong == "DANG_O"
                                                select new
                                                {
                                                    MaPhong = ct.PHONGMaPhong,
                                                    SoKhachDangKy = dp.SoLuongKhach ?? 1
                                                }).ToListAsync();

                var activeStaysByRoom = await _context.PHIEU_LUU_TRU.AsNoTracking()
                    .Where(p => p.TrangThaiLuuTru == "DANG_LUU_TRU")
                    .GroupBy(p => p.PHONGMaPhong)
                    .Select(g => new { MaPhong = g.Key, SoKhachDaKhaiBao = g.Count() })
                    .ToListAsync();

                int khachOChungConThieu = 0;
                int demPhongChuaDu = 0;

                foreach (var rb in activeRoomBookings)
                {
                    var stayInfo = activeStaysByRoom.FirstOrDefault(s => s.MaPhong == rb.MaPhong);
                    // Nếu phòng đã mở mà chưa có bản ghi PHIEU_LUU_TRU (dữ liệu cũ) thì mặc định đã có 1 người đại diện
                    int daKhaiBao = stayInfo != null ? stayInfo.SoKhachDaKhaiBao : 1;

                    if (daKhaiBao < rb.SoKhachDangKy)
                    {
                        khachOChungConThieu += (rb.SoKhachDangKy - daKhaiBao);
                        demPhongChuaDu++;
                    }
                }

                SoPhongChuaDuKhach = demPhongChuaDu;
                // Tổng khách chờ Check-in = Khách của phòng chưa nhận + Khách ở cùng chưa đến trình diện CCCD
                SoKhachDangChoCheckIn = khachChuaMoPhong + khachOChungConThieu;


                // =========================================================================
                // 2. THỐNG KÊ KHÁCH ĐI HÔM NAY (DEPARTURES)
                // =========================================================================
                var departuresToday = await _context.DAT_PHONG.AsNoTracking()
                    .Where(dp => dp.NgayTra.HasValue && dp.NgayTra.Value.Date == today)
                    .ToListAsync();

                // Số khách đang ở có lịch trả phòng hôm nay nhưng Lễ tân CHƯA bấm Check-out
                SoKhachDangChoCheckOut = departuresToday
                    .Where(dp => dp.TrangThaiDatPhong == "DANG_O")
                    .Sum(dp => dp.SoLuongKhach ?? 0);

                // Số khách Lễ tân ĐÃ BẤM Check-out hoàn tất trong ngày hôm nay
                int soPhieuCheckOutHomNay = await _context.PHIEU_LUU_TRU.AsNoTracking()
                    .CountAsync(p => p.ThoiGianCheckOut.HasValue
                                  && p.ThoiGianCheckOut.Value.Date == today
                                  && p.TrangThaiLuuTru == "DA_CHECK_OUT");

                int soKhachCheckOutTheoDon = departuresToday
                    .Where(dp => dp.TrangThaiDatPhong == "Đã check-out")
                    .Sum(dp => dp.SoLuongKhach ?? 0);

                // Lấy giá trị lớn hơn để đảm bảo dù phòng Check-out khi mới khai báo 1/3 phiếu hay đủ 3/3 phiếu đều phản ánh đúng tổng khách rời đi
                SoKhachDaCheckOut = Math.Max(soPhieuCheckOutHomNay, soKhachCheckOutTheoDon);


                // =========================================================================
                // 3. THỐNG KÊ PHÒNG ĐANG LƯU TRÚ (OCCUPANCY)
                // =========================================================================
                // Đếm số lượng phòng vật lý phân biệt (Distinct) đang có người ở thực tế
                TongPhongDangLuuTru = activeRoomBookings
                    .Select(x => x.MaPhong)
                    .Distinct()
                    .Count();

                return Page();
            }
            catch (Exception ex)
            {
                // Bọc try...catch chống sập ứng dụng và hiển thị thông báo lỗi rõ ràng
                AlertMessage = $"Lỗi hệ thống khi tải dữ liệu tiền sảnh: {ex.Message}";
                return Page();
            }
        }
    }
}