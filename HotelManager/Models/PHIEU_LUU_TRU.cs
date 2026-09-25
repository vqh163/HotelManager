using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("PHIEU_LUU_TRU")]
    public class PHIEU_LUU_TRU
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaPhieuLuuTru { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? KHACH_HANGMaKH { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? PHONGMaPhong { get; set; }

        public DateTime? ThoiGianCheckIn { get; set; }
        public DateTime? ThoiGianCheckOut { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThaiLuuTru { get; set; }
    }
}