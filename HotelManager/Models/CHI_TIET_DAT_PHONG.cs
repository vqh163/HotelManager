using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("CHI_TIET_DAT_PHONG")]
    public class CHI_TIET_DAT_PHONG
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaChiTietDatPhong { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? PHONGMaPhong { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? DAT_PHONGMaDatPhong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DonGia { get; set; }
        public int? SoNgayO { get; set; }
    }
}