using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManager.Models
{
    // Ánh xạ lớp này thành bảng TAI_KHOAN trong SQL Server theo đúng chuẩn thiết kế
    [Table("TAI_KHOAN")]
    public class TAI_KHOAN
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        public string MaTaiKhoan { get; set; } = default!;

        // Bổ sung 2 khóa ngoại theo đúng ERD mục 2.3.2
        [Column(TypeName = "varchar(20)")]
        public string? KHACH_HANGMaKH { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? NHAN_VIENMaNV { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [Column(TypeName = "varchar(50)")]
        public string TenDangNhap { get; set; } = default!;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [Column(TypeName = "varchar(50)")] // (Theo mô tả 2.3.1 là varchar 50)
        [MaxLength(255)]
        public string? MatKhau { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? VaiTro { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? TrangThai { get; set; }
    }
}