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
    }
}