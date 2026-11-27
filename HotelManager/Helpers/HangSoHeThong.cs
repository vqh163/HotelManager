using System;

namespace HotelManager.Helpers
{
    /// <summary>
    /// Lưu trữ các hằng số chuẩn hóa dùng chung toàn hệ thống (Vai trò, Chính sách phân quyền, Trạng thái nghiệp vụ)
    /// nhằm tránh lỗi gõ sai chuỗi ký tự giữa các phân hệ.
    /// </summary>
    public static class HangSoHeThong
    {
        // =========================================================================
        // 1. TÊN VAI TRÒ CHUẨN THEO BẢNG CHUC_VU (VÀ TÊN CŨ ĐỂ TƯƠNG THÍCH)
        // =========================================================================
        public const string VaiTroQuanTri = "Quản trị viên";
        public const string VaiTroQuanTriCu = "Quản trị viên hệ thống";
        public const string VaiTroGiamDoc = "Giám đốc";
        public const string VaiTroQuanLy = "Quản lý";
        public const string VaiTroLeTan = "Lễ tân";
        public const string VaiTroBuongPhong = "Nhân viên Buồng phòng";
        public const string VaiTroBuongPhongCu = "Buồng phòng";
        public const string VaiTroKyThuat = "Nhân viên Kỹ thuật";
        public const string VaiTroKyThuatCu = "Kỹ thuật";
        public const string VaiTroThuNgan = "Thu ngân Nhà hàng";
        public const string VaiTroPhucVu = "Phục vụ Nhà hàng";
        public const string VaiTroDauBep = "Đầu bếp";
        public const string VaiTroThuKho = "Thủ kho";
        public const string VaiTroSales = "Sales";
        public const string VaiTroKeToan = "Kế toán";
        public const string VaiTroKhachHang = "Khách hàng";

        // =========================================================================
        // 2. TÊN CHÍNH SÁCH PHÂN QUYỀN (AUTHORIZATION POLICIES)
        // =========================================================================
        public const string PolicyQuanTri = "QuanTriVienOnly";
        public const string PolicyQuanLyCap = "QuanLyCapPolicy";
        public const string PolicyNhanSu = "NhanSuPolicy";
        public const string PolicyNoiBo = "NoiBoPolicy";
        public const string PolicyKho = "KhoPolicy";
        public const string PolicyNhaHang = "NhaHangPolicy";
        public const string PolicyThucDon = "ThucDonPolicy";
        public const string PolicyThuNgan = "ThuNganPolicy";
        public const string PolicySoDoPhong = "SoDoPhongPolicy";
        public const string PolicyTienSanh = "TienSanhPolicy";
        public const string PolicySuCo = "SuCoPolicy";
        public const string PolicyBuongPhong = "BuongPhongPolicy";
        public const string PolicyKhachHang = "KhachHangPolicy";
        public const string PolicyXemAnhKyc = "XemAnhKycPolicy";

        // =========================================================================
        // 3. TRẠNG THÁI ĐẶT PHÒNG CHUẨN (DAT_PHONG.TrangThaiDatPhong)
        // =========================================================================
        public const string DatPhongChoXacNhan = "Chờ xác nhận";
        public const string DatPhongDaXacNhan = "Đã xác nhận";
        public const string DatPhongDangO = "DANG_O";
        public const string DatPhongDaCheckOut = "Đã check-out";
        public const string DatPhongDaHuy = "Đã hủy";

        // Tập các trạng thái đơn đặt phòng hợp lệ cho phép Check-in và hiển thị trên Sơ đồ phòng
        public static readonly string[] TrangThaiChoCheckIn =
        {
            "Chờ xác nhận",
            "Đã xác nhận",
            "DA_DAT",
            "Chờ nhận phòng"
        };

        // Tập các trạng thái đơn đặt phòng đã kết thúc (không còn giữ phòng)
        public static readonly string[] TrangThaiDaKetThuc =
        {
            "Đã hủy",
            "Đã check-out",
            "Đã hoàn tất"
        };

        // =========================================================================
        // 4. TRẠNG THÁI HÓA ĐƠN NHÀ HÀNG & TỶ LỆ THUẾ/PHÍ THỐNG NHẤT
        // =========================================================================
        public const string HoaDonNHDangPhucVu = "Đang phục vụ";
        public const string HoaDonNHChoThanhToan = "Chờ thanh toán";
        public const string HoaDonNHDaThanhToan = "Đã thanh toán";
        public const string HoaDonNHGhiNoPhong = "Ghi nợ phòng";

        public const decimal TyLePhiPhucVuNhaHang = 0.05m; // 5% phí phục vụ
        public const decimal TyLeThueVAT = 0.10m;          // 10% thuế VAT

        // =========================================================================
        // 5. HÀM ĐIỀU HƯỚNG TRANG CHỦ MẶC ĐỊNH THEO VAI TRÒ
        // =========================================================================
        public static string LayTrangMacDinhTheoVaiTro(string? vaiTro)
        {
            string role = (vaiTro ?? string.Empty).Trim();
            return role switch
            {
                VaiTroQuanTri or VaiTroQuanTriCu => "/QuanTriHeThong/QuanLyTaiKhoan",
                VaiTroGiamDoc => "/QuanLyNhanSu/QuanLyDanhSachNhanVien",
                VaiTroQuanLy => "/QuanLyLichCaNhan/QuanLyLichCaNhan",
                VaiTroLeTan => "/QuanLyTienSanh/Index",
                VaiTroSales or VaiTroBuongPhong or VaiTroBuongPhongCu => "/QuanLySoDoPhong/SoDoPhong",
                VaiTroDauBep => "/QuanLyNhaHang/QuanLyThucDon",
                VaiTroPhucVu => "/QuanLyNhaHang/SoDoBan",
                VaiTroThuNgan => "/QuanLyNhaHang/QuayThuNgan",
                VaiTroKyThuat or VaiTroKyThuatCu => "/QuanLySuCo/Index",
                VaiTroThuKho => "/QuanLyKho/QuanLyKho",
                VaiTroKeToan => "/QuanLyLichCaNhan/QuanLyLichCaNhan",
                VaiTroKhachHang => "/QuanLySoDoPhong/TimPhong",
                _ => "/Index"
            };
        }

        // =========================================================================
        // 6. TÍNH HẠNG THÀNH VIÊN & CHIẾT KHẤU THEO ĐIỂM THƯỞNG
        // =========================================================================
        public static string XacDinhHangThanhVien(int diemThuong)
        {
            if (diemThuong < 100) return "Thành viên mới";
            if (diemThuong < 500) return "Bạc";
            if (diemThuong < 1000) return "Vàng";
            return "Kim cương";
        }

        public static decimal LayTyLeGiamGiaTheoHang(string? hangThanhVien)
        {
            return (hangThanhVien ?? string.Empty).Trim() switch
            {
                "Bạc" => 0.03m,
                "Vàng" => 0.05m,
                "Kim cương" => 0.10m,
                _ => 0m
            };
        }
    }
}
