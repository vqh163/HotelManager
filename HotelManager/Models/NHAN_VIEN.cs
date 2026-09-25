using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("NHAN_VIEN")]
    public class NHAN_VIEN
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaNV { get; set; } = default!;

        [Column(TypeName = "nvarchar(100)")]
        public string? HoTen { get; set; }

        [Column(TypeName = "varchar(12)")]
        public string? SoCCCD { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string? GioiTinh { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgaySinh { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? SDT { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? Email { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayVaoLam { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CHUC_VUMaChucVu { get; set; }
    }
}