using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("HOA_DON_NHA_HANG")]
    public class HOA_DON_NHA_HANG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaHoaDonNhaHang { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? BAN_NHA_HANGMaBan { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? DAT_PHONGMaDatPhong { get; set; }

        public DateTime? ThoiGianTao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongTienMonAn { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiamGia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ThanhTien { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? HinhThucThanhToan { get; set; }

        // Thêm vào class HOA_DON_NHA_HANG
        public string? DanhSachMonJson { get; set; }

        // Phí phục vụ nhà hàng (5%)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PhiPhucVu { get; set; }

        // Thuế giá trị gia tăng VAT (10%)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienThueVAT { get; set; }

        // Trạng thái hóa đơn tách biệt với hình thức thanh toán
        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThaiThanhToan { get; set; }
    }
}