using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HotelManager.Data;
using HotelManager.Helpers;
using HotelManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelManager.Pages.QuanLySoDoPhong
{
    [Authorize]
    public class LichSuDatPhongModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LichSuDatPhongModel> _logger;

        public LichSuDatPhongModel(ApplicationDbContext context, ILogger<LichSuDatPhongModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class ThongTinLichSu
        {
            public DAT_PHONG DatPhong { get; set; } = default!;
            public CHI_TIET_DAT_PHONG ChiTiet { get; set; } = default!;
            public PHONG Phong { get; set; } = default!;
        }

        public List<ThongTinLichSu> DanhSachSapToi { get; set; } = new List<ThongTinLichSu>();
        public List<ThongTinLichSu> DanhSachDaHoanTat { get; set; } = new List<ThongTinLichSu>();

        [TempData]
        public string? ThongBao { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToPage("/Auth/DangNhap");

            try
            {
                var tenDangNhap = User.Identity.Name;
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking().FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

                if (taiKhoan != null && !string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH))
                {
                    var query = from dp in _context.DAT_PHONG
                                join ct in _context.CHI_TIET_DAT_PHONG on dp.MaDatPhong equals ct.DAT_PHONGMaDatPhong
                                join p in _context.PHONG on ct.PHONGMaPhong equals p.MaPhong
                                where dp.KHACH_HANGMaKH == taiKhoan.KHACH_HANGMaKH
                                orderby dp.NgayNhan descending
                                select new ThongTinLichSu { DatPhong = dp, ChiTiet = ct, Phong = p };

                    var tatCaLichSu = await query.AsNoTracking().ToListAsync();

                    // Bao gồm cả đơn đang lưu trú (DANG_O) trong danh sách hiện tại/sắp tới
                    DanhSachSapToi = tatCaLichSu.Where(x =>
                        HangSoHeThong.TrangThaiChoCheckIn.Contains(x.DatPhong.TrangThaiDatPhong ?? "")
                        || x.DatPhong.TrangThaiDatPhong == HangSoHeThong.DatPhongDangO).ToList();

                    DanhSachDaHoanTat = tatCaLichSu.Where(x =>
                        HangSoHeThong.TrangThaiDaKetThuc.Contains(x.DatPhong.TrangThaiDatPhong ?? "")).ToList();
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải lịch sử đặt phòng.");
                ThongBao = "Đã xảy ra lỗi hệ thống khi tải lịch sử đặt phòng.";
                return Page();
            }
        }

        // Hủy đơn đặt phòng: Bắt buộc kiểm tra đăng nhập, kiểm tra quyền sở hữu (chống IDOR) và trạng thái hợp lệ
        public async Task<IActionResult> OnPostHuyPhongAsync(string maDatPhong)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToPage("/Auth/DangNhap");

            try
            {
                var taiKhoan = await _context.TAI_KHOAN.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TenDangNhap == User.Identity.Name);

                var datPhong = await _context.DAT_PHONG.FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);
                if (datPhong == null || !datPhong.NgayNhan.HasValue)
                {
                    ThongBao = "Không tìm thấy dữ liệu đơn đặt phòng hợp lệ!";
                    return RedirectToPage();
                }

                // 1. Chống IDOR: Chỉ chủ sở hữu đơn đặt phòng mới được phép tự hủy
                if (taiKhoan == null || string.IsNullOrEmpty(taiKhoan.KHACH_HANGMaKH) || datPhong.KHACH_HANGMaKH != taiKhoan.KHACH_HANGMaKH)
                {
                    return Forbid();
                }

                // 2. Chỉ cho phép hủy các đơn ở trạng thái chờ nhận phòng (chặn hủy đơn đang ở hoặc đã check-out)
                if (!HangSoHeThong.TrangThaiChoCheckIn.Contains(datPhong.TrangThaiDatPhong ?? ""))
                {
                    ThongBao = $"Không thể hủy đơn đặt phòng đang ở trạng thái '{datPhong.TrangThaiDatPhong}'!";
                    return RedirectToPage();
                }

                // 3. Tính số ngày còn lại và chính sách hoàn cọc
                int soNgayConLai = (datPhong.NgayNhan.Value.Date - DateTime.Now.Date).Days;
                decimal tienCoc = datPhong.TienCoc.GetValueOrDefault(0m);
                decimal tienHoan = 0m;
                string ghiChuHoanTien;

                if (soNgayConLai >= 7)
                {
                    tienHoan = tienCoc;
                    ghiChuHoanTien = $"Hoàn 100% cọc: {tienHoan:N0} đ (Hủy trước {soNgayConLai} ngày).";
                }
                else if (soNgayConLai >= 5)
                {
                    tienHoan = tienCoc * 0.5m;
                    ghiChuHoanTien = $"Hoàn 50% cọc: {tienHoan:N0} đ (Hủy trước {soNgayConLai} ngày).";
                }
                else
                {
                    ghiChuHoanTien = "Không hoàn cọc (Hủy dưới 5 ngày).";
                }

                datPhong.TrangThaiDatPhong = HangSoHeThong.DatPhongDaHuy;
                string ghiChuCapNhat = string.IsNullOrEmpty(datPhong.GhiChu) ? ghiChuHoanTien : $"{datPhong.GhiChu} | {ghiChuHoanTien}";
                datPhong.GhiChu = ghiChuCapNhat.Length > 255 ? ghiChuCapNhat.Substring(ghiChuCapNhat.Length - 255) : ghiChuCapNhat;
                _context.DAT_PHONG.Update(datPhong);

                await _context.SaveChangesAsync();
                ThongBao = "Hủy phòng thành công! " + ghiChuHoanTien;
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hủy đơn đặt phòng {MaDP}", maDatPhong);
                ThongBao = "Đã xảy ra lỗi hệ thống khi hủy phòng.";
                return RedirectToPage();
            }
        }
    }
}
