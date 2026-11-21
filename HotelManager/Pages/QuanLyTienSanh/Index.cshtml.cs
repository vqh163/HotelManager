using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        // ViewModel biểu thị thông tin chi tiết của từng phòng đang có khách lưu trú
        public class OccupiedRoomDetailViewModel
        {
            public string MaPhong { get; set; } = string.Empty;
            public string SoPhong { get; set; } = string.Empty;
            public string HangPhong { get; set; } = string.Empty;
            public int SoKhachDangLuuTru { get; set; }
            public int TongSoKhachDangKy { get; set; }
            public string TenKhachDaiDien { get; set; } = string.Empty;
            public string SDTKhachDaiDien { get; set; } = string.Empty;
            public decimal ViCoc { get; set; }
            public DateTime? ThoiGianCheckInDuKien { get; set; }
            public DateTime? ThoiGianTraDuKien { get; set; }
            public bool DaDuCCCD => SoKhachDangLuuTru >= TongSoKhachDangKy;
        }

        // Biến lưu trữ thông báo lỗi/thành công trên giao diện
        [TempData]
        public string? AlertMessage { get; set; }

        // === BIẾN LƯU TRỮ SỐ LƯỢNG KHÁCH VÀ PHÒNG TRONG NGÀY (UC_04) ===
        public int SoKhachDangChoCheckIn { get; set; }
        public int SoKhachDangChoCheckOut { get; set; }

        public int SoKhachDaCheckIn { get; set; }
        public int SoKhachDaCheckOut { get; set; }

        public int SoPhongChuaDuKhach { get; set; }
        public int TongPhongDangLuuTru { get; set; }

        // Danh sách chi tiết các phòng đang lưu trú (Hiển thị khi Lễ tân click vào ô "Phòng đang lưu trú")
        public List<OccupiedRoomDetailViewModel> DanhSachPhongDangLuuTru { get; set; } = new List<OccupiedRoomDetailViewModel>();

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

                int khachChuaMoPhong = arrivalsToday
                    .Where(dp => dp.TrangThaiDatPhong == "DA_DAT" || dp.TrangThaiDatPhong == "Chờ xác nhận")
                    .Sum(dp => dp.SoLuongKhach ?? 0);

                int soPhieuCheckInHomNay = await _context.PHIEU_LUU_TRU.AsNoTracking()
                    .CountAsync(p => p.ThoiGianCheckIn.HasValue && p.ThoiGianCheckIn.Value.Date == today);

                if (soPhieuCheckInHomNay > 0)
                {
                    SoKhachDaCheckIn = soPhieuCheckInHomNay;
                }
                else
                {
                    SoKhachDaCheckIn = arrivalsToday
                        .Where(dp => dp.TrangThaiDatPhong == "DANG_O" || dp.TrangThaiDatPhong == "Đã check-out")
                        .Sum(dp => dp.SoLuongKhach ?? 0);
                }

                // =========================================================================
                // 2. TRUY VẤN CHI TIẾT CÁC PHÒNG ĐANG LƯU TRÚ (KẾT NỐI PHONG - DAT_PHONG - KHACH_HANG)
                // =========================================================================
                var activeRoomBookings = await (from ct in _context.CHI_TIET_DAT_PHONG.AsNoTracking()
                                                join dp in _context.DAT_PHONG.AsNoTracking() on ct.DAT_PHONGMaDatPhong equals dp.MaDatPhong
                                                join p in _context.PHONG.AsNoTracking() on ct.PHONGMaPhong equals p.MaPhong
                                                join kh in _context.KHACH_HANG.AsNoTracking() on dp.KHACH_HANGMaKH equals kh.MaKH
                                                where dp.TrangThaiDatPhong == "DANG_O"
                                                select new
                                                {
                                                    MaPhong = p.MaPhong,
                                                    SoPhong = p.SoPhong ?? p.MaPhong,
                                                    HangPhong = p.LoaiPhong ?? "Standard",
                                                    SoKhachDangKy = dp.SoLuongKhach ?? 1,
                                                    TenKhachDaiDien = kh.HoTen ?? "Khách lưu trú",
                                                    SDTKhachDaiDien = kh.SDT ?? "Chưa cập nhật",
                                                    TienCoc = dp.TienCoc ?? 0m,
                                                    NgayNhan = dp.NgayNhan,
                                                    NgayTra = dp.NgayTra
                                                }).ToListAsync();

                // Đếm số lượng khách đang lưu trú thực tế tại từng phòng từ bảng PHIEU_LUU_TRU
                var activeStaysByRoom = await _context.PHIEU_LUU_TRU.AsNoTracking()
                    .Where(p => p.TrangThaiLuuTru == "DANG_LUU_TRU")
                    .GroupBy(p => p.PHONGMaPhong)
                    .Select(g => new { MaPhong = g.Key, SoKhachDangO = g.Count() })
                    .ToListAsync();

                // Đếm tổng số khách đã từng trình diện CCCD (cả đang ở và đã rời đi trước) để kiểm tra thiếu CCCD
                var totalEverCheckedInByRoom = await _context.PHIEU_LUU_TRU.AsNoTracking()
                    .Where(p => p.TrangThaiLuuTru == "DANG_LUU_TRU" || p.TrangThaiLuuTru == "DA_ROI_DI")
                    .GroupBy(p => p.PHONGMaPhong)
                    .Select(g => new { MaPhong = g.Key, TongDaKhaiBao = g.Count() })
                    .ToListAsync();

                int khachOChungConThieu = 0;
                int demPhongChuaDu = 0;

                // Gộp theo MaPhong (tránh trùng nếu có nhiều bản ghi chi tiết) và đổ vào DanhSachPhongDangLuuTru
                var uniqueActiveRooms = activeRoomBookings
                    .GroupBy(x => x.MaPhong)
                    .Select(g => g.First())
                    .OrderBy(x => x.SoPhong)
                    .ToList();

                foreach (var rb in uniqueActiveRooms)
                {
                    var stayNow = activeStaysByRoom.FirstOrDefault(s => s.MaPhong == rb.MaPhong);
                    var everStay = totalEverCheckedInByRoom.FirstOrDefault(s => s.MaPhong == rb.MaPhong);

                    // Số lượng khách hiện đang lưu trú tại phòng (nếu chưa có bản ghi PHIEU_LUU_TRU thì tính mặc định 1 người đại diện)
                    int soDangOThucTe = stayNow != null ? stayNow.SoKhachDangO : 1;
                    int daKhaiBaoCCCD = everStay != null ? everStay.TongDaKhaiBao : 1;

                    if (daKhaiBaoCCCD < rb.SoKhachDangKy)
                    {
                        khachOChungConThieu += (rb.SoKhachDangKy - daKhaiBaoCCCD);
                        demPhongChuaDu++;
                    }

                    DanhSachPhongDangLuuTru.Add(new OccupiedRoomDetailViewModel
                    {
                        MaPhong = rb.MaPhong,
                        SoPhong = rb.SoPhong,
                        HangPhong = rb.HangPhong,
                        SoKhachDangLuuTru = soDangOThucTe,
                        TongSoKhachDangKy = rb.SoKhachDangKy,
                        TenKhachDaiDien = rb.TenKhachDaiDien,
                        SDTKhachDaiDien = rb.SDTKhachDaiDien,
                        ViCoc = rb.TienCoc,
                        ThoiGianCheckInDuKien = rb.NgayNhan,
                        ThoiGianTraDuKien = rb.NgayTra
                    });
                }

                SoPhongChuaDuKhach = demPhongChuaDu;
                SoKhachDangChoCheckIn = khachChuaMoPhong + khachOChungConThieu;
                TongPhongDangLuuTru = DanhSachPhongDangLuuTru.Count;

                // =========================================================================
                // 3. THỐNG KÊ KHÁCH ĐI HÔM NAY (DEPARTURES)
                // =========================================================================
                var departuresToday = await _context.DAT_PHONG.AsNoTracking()
                    .Where(dp => dp.NgayTra.HasValue && dp.NgayTra.Value.Date == today)
                    .ToListAsync();

                SoKhachDangChoCheckOut = departuresToday
                    .Where(dp => dp.TrangThaiDatPhong == "DANG_O")
                    .Sum(dp => dp.SoLuongKhach ?? 0);

                int soPhieuCheckOutHomNay = await _context.PHIEU_LUU_TRU.AsNoTracking()
                    .CountAsync(p => p.ThoiGianCheckOut.HasValue
                                  && p.ThoiGianCheckOut.Value.Date == today
                                  && (p.TrangThaiLuuTru == "DA_CHECK_OUT" || p.TrangThaiLuuTru == "DA_ROI_DI"));

                int soKhachCheckOutTheoDon = departuresToday
                    .Where(dp => dp.TrangThaiDatPhong == "Đã check-out")
                    .Sum(dp => dp.SoLuongKhach ?? 0);

                SoKhachDaCheckOut = Math.Max(soPhieuCheckOutHomNay, soKhachCheckOutTheoDon);

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