using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("HOA_DON")]
    public class HOA_DON
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaHoaDon { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? KHACH_HANGMaKH { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? DAT_PHONGMaDatPhong { get; set; }

        public DateTime? NgayLap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienPhong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienDichVu { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienNhaHang { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienThueVAT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienCocDaTru { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienGiamGia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongTienThanhToan { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThaiThanhToan { get; set; }
    }
}