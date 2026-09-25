using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("DICH_VU")]
    public class DICH_VU
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaDichVu { get; set; } = default!;

        [Column(TypeName = "nvarchar(100)")]
        public string? TenDichVu { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? LoaiDichVu { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? DonGia { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public string? DonViTinh { get; set; }
    }
}