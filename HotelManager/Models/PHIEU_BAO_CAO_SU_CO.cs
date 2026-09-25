using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("PHIEU_BAO_CAO_SU_CO")]
    public class PHIEU_BAO_CAO_SU_CO
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaPhieuBaoCao { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? KHO_VAT_TUMaVatTu { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? MoTaSuCo { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? HinhAnhDinhKem { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? MucDoUuTien { get; set; }
        public DateTime? ThoiGianTao { get; set; }
    }
}