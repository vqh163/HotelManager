using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("PHONG")]
    public class PHONG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaPhong { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? TRANG_THAI_PHONGMaTrangThai { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? SoPhong { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? LoaiPhong { get; set; }

        [Column(TypeName = "int")]
        public int? SoGiuong { get; set; }

        [Column(TypeName = "int")]
        public int? SucChuaToiDa { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiaTheoNgay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiaTheoGio { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? MoTa { get; set; }
    }
}