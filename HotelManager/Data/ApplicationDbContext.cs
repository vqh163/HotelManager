using HotelManager.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Nạp sẵn danh sách chức vụ vào bảng CHUC_VU theo đúng tài liệu
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
                new CHUC_VU { MaChucVu = "CV12", TenChucVu = "Giám đốc" }
            );
        }
    }
}