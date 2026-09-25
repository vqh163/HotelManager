using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("KHO_VAT_TU")]
    public class KHO_VAT_TU
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaVatTu { get; set; } = default!;

        [Column(TypeName = "nvarchar(100)")]
        public string? TenVatTu { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? LoaiVatTu { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public string? DonViTinh { get; set; }

        public int? SoLuongTon { get; set; }
        public int? SoLuongTonToiThieu { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DonGiaNhap { get; set; }
    }
}