using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("PHONG_BAN")]
    public class PHONG_BAN
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaPB { get; set; } = default!;

        [Column(TypeName = "nvarchar(100)")]
        public string? TenPB { get; set; }
    }
}