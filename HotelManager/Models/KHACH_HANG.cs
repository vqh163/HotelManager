
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("KHACH_HANG")]
    public class KHACH_HANG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaKH { get; set; } = default!;

        [Column(TypeName = "nvarchar(100)")]
        public string? HoTen { get; set; }

        [Column(TypeName = "varchar(12)")]
        public string? SoCCCD { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? SDT { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? Email { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string? GioiTinh { get; set; }

        [Column(TypeName = "int")]
        public int? DiemThuong { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? HangThanhVien { get; set; }

        [Column(TypeName = "nvarchar(10)")]
        public string? Blacklist { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? GhiChuDacBiet { get; set; }
    }
}