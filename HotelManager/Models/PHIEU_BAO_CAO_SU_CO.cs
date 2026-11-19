using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("PHIEU_BAO_CAO_SU_CO")]
    public class PHIEU_BAO_CAO_SU_CO
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaPhieuBaoCao { get; set; } = default!;

        // Mã nhân viên tạo phiếu báo cáo sự cố
        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        // Mã vật tư chính đầu tiên sử dụng (giữ khóa ngoại theo ERD mục 2.3.2)
        [Column(TypeName = "varchar(20)")]
        public string? KHO_VAT_TUMaVatTu { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? MoTaSuCo { get; set; }

        // Ảnh hiện trường trước khi sửa chữa
        [Column(TypeName = "varchar(255)")]
        public string? HinhAnhDinhKem { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? MucDoUuTien { get; set; }

        public DateTime? ThoiGianTao { get; set; }

        // =========================================================================
        // CÁC CỘT BỔ SUNG PHỤC VỤ NGHIỆP VỤ UC_07 (QUY TRÌNH 4 BƯỚC & LIÊN KẾT PHÒNG/KHO)
        // =========================================================================

        // Mã phòng lưu trú nếu sự cố xảy ra trong phòng (Null nếu là khu vực chung)
        [Column(TypeName = "varchar(20)")]
        public string? MaPhong { get; set; }

        // Vị trí hiển thị: VD "Phòng 101" hoặc "Sảnh chính", "Hành lang tầng 2"...
        [Column(TypeName = "nvarchar(100)")]
        public string? ViTriSuCo { get; set; }

        // 4 trạng thái: "Chờ tiếp nhận", "Đang sửa chữa", "Chờ nghiệm thu", "Hoàn thành"
        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThai { get; set; } = "Chờ tiếp nhận";

        // Tên đăng nhập người tạo phiếu (dự phòng khi tài khoản chưa gắn MaNV)
        [Column(TypeName = "nvarchar(100)")]
        public string? NguoiTaoTen { get; set; }

        // Mã nhân viên Kỹ thuật tiếp nhận xử lý
        [Column(TypeName = "varchar(20)")]
        public string? MaNVKyThuat { get; set; }

        // Tên nhân viên Kỹ thuật tiếp nhận xử lý
        [Column(TypeName = "nvarchar(100)")]
        public string? TenNVKyThuat { get; set; }

        // Ảnh chứng minh sau khi sửa chữa xong
        [Column(TypeName = "varchar(255)")]
        public string? AnhSauSuaChua { get; set; }

        // Ghi chú quá trình xử lý kỹ thuật
        [Column(TypeName = "nvarchar(500)")]
        public string? GhiChuXuLy { get; set; }

        // Lý do Quản lý trả lại nếu nghiệm thu không đạt
        [Column(TypeName = "nvarchar(500)")]
        public string? LyDoTuChoi { get; set; }

        // Danh sách nhiều vật tư tiêu hao đã xuất kho (lưu JSON)
        [Column(TypeName = "nvarchar(max)")]
        public string? DanhSachVatTuJson { get; set; }

        // Chi phí phát sinh / đền bù ngoài vật tư kho (VNĐ)
        [Column(TypeName = "decimal(12, 2)")]
        public decimal? ChiPhiPhatSinh { get; set; } = 0;

        // Tổng chi phí bảo trì = Tiền vật tư + Chi phí phát sinh
        [Column(TypeName = "decimal(12, 2)")]
        public decimal? TongChiPhi { get; set; } = 0;

        // Đánh dấu nguyên nhân hư hỏng do khách hàng gây ra
        public bool DoKhachLamHong { get; set; } = false;

        // Đánh dấu phiếu này có thực hiện khóa phòng sang "Bảo trì" trên Sơ đồ phòng
        public bool CoKhoaPhong { get; set; } = false;

        public DateTime? ThoiGianTiepNhan { get; set; }

        public DateTime? ThoiGianHoanThanh { get; set; }
    }
}