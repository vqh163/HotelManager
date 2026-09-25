using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("THANH_TOAN")]
    public class THANH_TOAN
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaThanhToan { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? HOA_DONMaHoaDon { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? HOA_DON_NHA_HANGMaHoaDonNhaHang { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SoTien { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? PhuongThucThanhToan { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? MaGiaoDichNgoai { get; set; }

        public DateTime? ThoiGianGiaoDich { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThaiGiaoDich { get; set; }
    }
}