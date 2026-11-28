using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    [Table("BAO_CAO_DOANH_THU")]
    public class BAO_CAO_DOANH_THU
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaBaoCao { get; set; } = default!;

        [Column(TypeName = "varchar(20)")]
        public string MaDatPhong { get; set; } = default!; // VD: #821

        [Column(TypeName = "nvarchar(50)")]
        public string? PhanHe { get; set; } = "Lễ tân"; // Dùng để nhóm (Nhóm theo Phân hệ)

        [Column(TypeName = "nvarchar(50)")]
        public string? LoaiPhong { get; set; } // VD: SP, VR, VIP, Penthouse

        [Column(TypeName = "nvarchar(50)")]
        public string? TenPhong { get; set; } // VD: 204, 402, vip2

        [Column(TypeName = "nvarchar(100)")]
        public string? TenKhach { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? NgayDen { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? NgayDi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienPhong { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DichVu { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChietKhau { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongDoanhThu { get; set; } = 0; // = TienPhong + DichVu - ChietKhau

        [Column(TypeName = "decimal(18,2)")]
        public decimal NoTruoc { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienDaThu { get; set; } = 0; // Cột cuối cùng bên phải

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThai { get; set; } // Dùng cho bộ lọc Trạng thái

        public bool BaoGomThuePhi { get; set; } = true; // Dùng cho checkbox "Bao gồm thuế phí"
    }
}
