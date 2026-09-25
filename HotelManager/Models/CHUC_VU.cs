using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("CHUC_VU")]
    public class CHUC_VU
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaChucVu { get; set; } = default!;

        [Column(TypeName = "nvarchar(100)")]
        public string? TenChucVu { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? PHONG_BANMaPB { get; set; }
    }
}