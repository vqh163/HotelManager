using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("LICH_LAM_VIEC")]
    public class LICH_LAM_VIEC
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaLich { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayLamViec { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? CaLamViec { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? GioCheckIn { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? GioCheckOut { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThai { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? GhiChu { get; set; }
    }
}