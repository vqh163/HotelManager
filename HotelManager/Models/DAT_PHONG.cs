using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("DAT_PHONG")]
    public class DAT_PHONG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaDatPhong { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? KHACH_HANGMaKH { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? DOI_TACMaDoiTac { get; set; }

        public DateTime? NgayDat { get; set; }
        public DateTime? NgayNhan { get; set; }
        public DateTime? NgayTra { get; set; }
        public int? SoLuongKhach { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TienCoc { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThaiDatPhong { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? KenhDatPhong { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? GhiChu { get; set; }
    }
}