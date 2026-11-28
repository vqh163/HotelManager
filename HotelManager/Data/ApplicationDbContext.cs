using Microsoft.EntityFrameworkCore;
using HotelManager.Models;
using HotelManager.Helpers;

namespace HotelManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Khai báo các DbSet (Bảng CSDL) toàn hệ thống theo chuẩn VIẾT_HOA_CÓ_GẠCH_DƯỚI
        public DbSet<TAI_KHOAN> TAI_KHOAN { get; set; } = default!;
        public DbSet<NHAN_VIEN> NHAN_VIEN { get; set; } = default!;
        public DbSet<KHACH_HANG> KHACH_HANG { get; set; } = default!;
        public DbSet<PHONG> PHONG { get; set; } = default!;
        public DbSet<LICH_LAM_VIEC> LICH_LAM_VIEC { get; set; } = default!;
        public DbSet<PHONG_BAN> PHONG_BAN { get; set; } = default!;
        public DbSet<TRANG_THAI_PHONG> TRANG_THAI_PHONG { get; set; } = default!;
        public DbSet<KHO_VAT_TU> KHO_VAT_TU { get; set; } = default!;
        public DbSet<DICH_VU> DICH_VU { get; set; } = default!;
        public DbSet<DOI_TAC> DOI_TAC { get; set; } = default!;
        public DbSet<BAN_NHA_HANG> BAN_NHA_HANG { get; set; } = default!;
        public DbSet<CHUC_VU> CHUC_VU { get; set; } = default!;
        public DbSet<DAT_PHONG> DAT_PHONG { get; set; } = default!;
        public DbSet<CHI_TIET_DAT_PHONG> CHI_TIET_DAT_PHONG { get; set; } = default!;
        public DbSet<PHIEU_LUU_TRU> PHIEU_LUU_TRU { get; set; } = default!;
        public DbSet<CHI_TIET_DICH_VU> CHI_TIET_DICH_VU { get; set; } = default!;
        public DbSet<PHIEU_BAO_CAO_SU_CO> PHIEU_BAO_CAO_SU_CO { get; set; } = default!;
        public DbSet<HOA_DON_NHA_HANG> HOA_DON_NHA_HANG { get; set; } = default!;
        public DbSet<HOA_DON> HOA_DON { get; set; } = default!;
        public DbSet<THANH_TOAN> THANH_TOAN { get; set; } = default!;
        public DbSet<NHAT_KY_HE_THONG> NHAT_KY_HE_THONG { get; set; } = default!;
        // Các bảng mở rộng nghiệp vụ & Báo cáo thống kê
        public DbSet<DON_XIN_NGHI> DON_XIN_NGHI { get; set; } = default!;
        public DbSet<PHIEU_NHAP_XUAT_KHO> PHIEU_NHAP_XUAT_KHO { get; set; } = default!;
        public DbSet<BIEN_BAN_KIEM_KE> BIEN_BAN_KIEM_KE { get; set; } = default!;
        public DbSet<BAO_CAO_DOANH_THU> BAO_CAO_DOANH_THU { get; set; } = default!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================================
            // 1. CẤU HÌNH UNIQUE INDEX & INDEX TỐI ƯU HIỆU NĂNG TRUY VẤN
            // =========================================================================
            modelBuilder.Entity<TAI_KHOAN>()
                .HasIndex(t => t.TenDangNhap)
                .IsUnique();

            modelBuilder.Entity<PHONG>()
                .HasIndex(p => p.SoPhong)
                .IsUnique()
                .HasFilter("[SoPhong] IS NOT NULL");

            modelBuilder.Entity<KHACH_HANG>()
                .HasIndex(k => k.SoCCCD)
                .IsUnique()
                .HasFilter("[SoCCCD] IS NOT NULL");

            modelBuilder.Entity<NHAN_VIEN>()
                .HasIndex(n => n.SoCCCD)
                .IsUnique()
                .HasFilter("[SoCCCD] IS NOT NULL");

            modelBuilder.Entity<DAT_PHONG>()
                .HasIndex(d => d.TrangThaiDatPhong);

            modelBuilder.Entity<CHI_TIET_DAT_PHONG>()
                .HasIndex(c => c.PHONGMaPhong);

            modelBuilder.Entity<PHIEU_LUU_TRU>()
                .HasIndex(p => new { p.PHONGMaPhong, p.TrangThaiLuuTru });

            modelBuilder.Entity<LICH_LAM_VIEC>()
                .HasIndex(l => l.NHAN_VIENMaNV);

            // =========================================================================
            // 2. NẠP SẴN DANH SÁCH CHỨC VỤ CHUẨN VÀO BẢNG CHUC_VU
            // =========================================================================
            modelBuilder.Entity<CHUC_VU>().HasData(
                new CHUC_VU { MaChucVu = "CV01", TenChucVu = "Quản trị viên" },
                new CHUC_VU { MaChucVu = "CV02", TenChucVu = "Quản lý" },
                new CHUC_VU { MaChucVu = "CV03", TenChucVu = "Lễ tân" },
                new CHUC_VU { MaChucVu = "CV04", TenChucVu = "Nhân viên Buồng phòng" },
                new CHUC_VU { MaChucVu = "CV05", TenChucVu = "Nhân viên Kỹ thuật" },
                new CHUC_VU { MaChucVu = "CV06", TenChucVu = "Thu ngân Nhà hàng" },
                new CHUC_VU { MaChucVu = "CV07", TenChucVu = "Phục vụ Nhà hàng" },
                new CHUC_VU { MaChucVu = "CV08", TenChucVu = "Đầu bếp" },
                new CHUC_VU { MaChucVu = "CV09", TenChucVu = "Thủ kho" },
                new CHUC_VU { MaChucVu = "CV10", TenChucVu = "Sales" },
                new CHUC_VU { MaChucVu = "CV11", TenChucVu = "Kế toán" },
                new CHUC_VU { MaChucVu = "CV12", TenChucVu = "Giám đốc" },
                new CHUC_VU { MaChucVu = "CV13", TenChucVu = "Khách hàng" }
            );

            // =========================================================================
            // 3. SEED DATA: TÀI KHOẢN QUẢN TRỊ VIÊN MẶC ĐỊNH (admin / 123456 ĐÃ BĂM SHA-256 BASE64)
            // =========================================================================
            modelBuilder.Entity<TAI_KHOAN>().HasData(
                new TAI_KHOAN
                {
                    MaTaiKhoan = "TK_ADMIN_001",
                    TenDangNhap = "admin",
                    MatKhau = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=", // SHA-256 Base64 của "123456"
                    VaiTro = "Quản trị viên",
                    TrangThai = "Hoạt động"
                }
            );
        }
    }
}
