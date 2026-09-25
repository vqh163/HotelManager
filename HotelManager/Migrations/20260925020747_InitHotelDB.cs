using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class InitHotelDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BAN_NHA_HANG",
                columns: table => new
                {
                    MaBan = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenBan = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    SucChua = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BAN_NHA_HANG", x => x.MaBan);
                });

            migrationBuilder.CreateTable(
                name: "CHI_TIET_DAT_PHONG",
                columns: table => new
                {
                    MaChiTietDatPhong = table.Column<string>(type: "varchar(20)", nullable: false),
                    PHONGMaPhong = table.Column<string>(type: "varchar(20)", nullable: true),
                    DAT_PHONGMaDatPhong = table.Column<string>(type: "varchar(20)", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SoNgayO = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHI_TIET_DAT_PHONG", x => x.MaChiTietDatPhong);
                });

            migrationBuilder.CreateTable(
                name: "CHI_TIET_DICH_VU",
                columns: table => new
                {
                    MaChiTietDichVu = table.Column<string>(type: "varchar(20)", nullable: false),
                    DICH_VUMaDichVu = table.Column<string>(type: "varchar(20)", nullable: true),
                    KHACH_HANGMaKH = table.Column<string>(type: "varchar(20)", nullable: true),
                    PHONGMaPhong = table.Column<string>(type: "varchar(20)", nullable: true),
                    KHO_VAT_TUMaVatTu = table.Column<string>(type: "varchar(20)", nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThoiGianSuDung = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHI_TIET_DICH_VU", x => x.MaChiTietDichVu);
                });

            migrationBuilder.CreateTable(
                name: "CHUC_VU",
                columns: table => new
                {
                    MaChucVu = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenChucVu = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    PHONG_BANMaPB = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHUC_VU", x => x.MaChucVu);
                });

            migrationBuilder.CreateTable(
                name: "DAT_PHONG",
                columns: table => new
                {
                    MaDatPhong = table.Column<string>(type: "varchar(20)", nullable: false),
                    KHACH_HANGMaKH = table.Column<string>(type: "varchar(20)", nullable: true),
                    DOI_TACMaDoiTac = table.Column<string>(type: "varchar(20)", nullable: true),
                    NgayDat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTra = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoLuongKhach = table.Column<int>(type: "int", nullable: true),
                    TienCoc = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrangThaiDatPhong = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    KenhDatPhong = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DAT_PHONG", x => x.MaDatPhong);
                });

            migrationBuilder.CreateTable(
                name: "DICH_VU",
                columns: table => new
                {
                    MaDichVu = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenDichVu = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    LoaiDichVu = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    DonViTinh = table.Column<string>(type: "nvarchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DICH_VU", x => x.MaDichVu);
                });

            migrationBuilder.CreateTable(
                name: "DOI_TAC",
                columns: table => new
                {
                    MaDoiTac = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenDoiTac = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    LoaiDoiTac = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    NguoiLienHe = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    SDT = table.Column<string>(type: "varchar(10)", nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", nullable: true),
                    ChietKhau = table.Column<double>(type: "float", nullable: true),
                    HanMucCongNo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SoDuCongNo = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOI_TAC", x => x.MaDoiTac);
                });

            migrationBuilder.CreateTable(
                name: "HOA_DON",
                columns: table => new
                {
                    MaHoaDon = table.Column<string>(type: "varchar(20)", nullable: false),
                    KHACH_HANGMaKH = table.Column<string>(type: "varchar(20)", nullable: true),
                    NHAN_VIENMaNV = table.Column<string>(type: "varchar(20)", nullable: true),
                    DAT_PHONGMaDatPhong = table.Column<string>(type: "varchar(20)", nullable: true),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TienPhong = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TienDichVu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TienNhaHang = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TienThueVAT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TienCocDaTru = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TienGiamGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TongTienThanhToan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrangThaiThanhToan = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOA_DON", x => x.MaHoaDon);
                });

            migrationBuilder.CreateTable(
                name: "HOA_DON_NHA_HANG",
                columns: table => new
                {
                    MaHoaDonNhaHang = table.Column<string>(type: "varchar(20)", nullable: false),
                    BAN_NHA_HANGMaBan = table.Column<string>(type: "varchar(20)", nullable: true),
                    NHAN_VIENMaNV = table.Column<string>(type: "varchar(20)", nullable: true),
                    DAT_PHONGMaDatPhong = table.Column<string>(type: "varchar(20)", nullable: true),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TongTienMonAn = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GiamGia = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HinhThucThanhToan = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOA_DON_NHA_HANG", x => x.MaHoaDonNhaHang);
                });

            migrationBuilder.CreateTable(
                name: "KHACH_HANG",
                columns: table => new
                {
                    MaKH = table.Column<string>(type: "varchar(20)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    SoCCCD = table.Column<string>(type: "varchar(12)", nullable: true),
                    SDT = table.Column<string>(type: "varchar(10)", nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    DiemThuong = table.Column<int>(type: "int", nullable: true),
                    HangThanhVien = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    Blacklist = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    GhiChuDacBiet = table.Column<string>(type: "nvarchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KHACH_HANG", x => x.MaKH);
                });

            migrationBuilder.CreateTable(
                name: "KHO_VAT_TU",
                columns: table => new
                {
                    MaVatTu = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenVatTu = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    LoaiVatTu = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    DonViTinh = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    SoLuongTon = table.Column<int>(type: "int", nullable: true),
                    SoLuongTonToiThieu = table.Column<int>(type: "int", nullable: true),
                    DonGiaNhap = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KHO_VAT_TU", x => x.MaVatTu);
                });

            migrationBuilder.CreateTable(
                name: "LICH_LAM_VIEC",
                columns: table => new
                {
                    MaLich = table.Column<string>(type: "varchar(20)", nullable: false),
                    NHAN_VIENMaNV = table.Column<string>(type: "varchar(20)", nullable: true),
                    NgayLamViec = table.Column<DateTime>(type: "date", nullable: true),
                    CaLamViec = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    GioCheckIn = table.Column<DateTime>(type: "datetime", nullable: true),
                    GioCheckOut = table.Column<DateTime>(type: "datetime", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICH_LAM_VIEC", x => x.MaLich);
                });

            migrationBuilder.CreateTable(
                name: "NHAN_VIEN",
                columns: table => new
                {
                    MaNV = table.Column<string>(type: "varchar(20)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    SoCCCD = table.Column<string>(type: "varchar(12)", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "date", nullable: true),
                    SDT = table.Column<string>(type: "varchar(10)", nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", nullable: true),
                    NgayVaoLam = table.Column<DateTime>(type: "date", nullable: true),
                    CHUC_VUMaChucVu = table.Column<string>(type: "varchar(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NHAN_VIEN", x => x.MaNV);
                });

            migrationBuilder.CreateTable(
                name: "PHIEU_BAO_CAO_SU_CO",
                columns: table => new
                {
                    MaPhieuBaoCao = table.Column<string>(type: "varchar(20)", nullable: false),
                    NHAN_VIENMaNV = table.Column<string>(type: "varchar(20)", nullable: true),
                    KHO_VAT_TUMaVatTu = table.Column<string>(type: "varchar(20)", nullable: true),
                    MoTaSuCo = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    HinhAnhDinhKem = table.Column<string>(type: "varchar(255)", nullable: true),
                    MucDoUuTien = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHIEU_BAO_CAO_SU_CO", x => x.MaPhieuBaoCao);
                });

            migrationBuilder.CreateTable(
                name: "PHIEU_LUU_TRU",
                columns: table => new
                {
                    MaPhieuLuuTru = table.Column<string>(type: "varchar(20)", nullable: false),
                    NHAN_VIENMaNV = table.Column<string>(type: "varchar(20)", nullable: true),
                    KHACH_HANGMaKH = table.Column<string>(type: "varchar(20)", nullable: true),
                    PHONGMaPhong = table.Column<string>(type: "varchar(20)", nullable: true),
                    ThoiGianCheckIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianCheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThaiLuuTru = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHIEU_LUU_TRU", x => x.MaPhieuLuuTru);
                });

            migrationBuilder.CreateTable(
                name: "PHONG",
                columns: table => new
                {
                    MaPhong = table.Column<string>(type: "varchar(20)", nullable: false),
                    TRANG_THAI_PHONGMaTrangThai = table.Column<string>(type: "varchar(20)", nullable: true),
                    SoPhong = table.Column<string>(type: "varchar(20)", nullable: true),
                    LoaiPhong = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    SoGiuong = table.Column<int>(type: "int", nullable: true),
                    SucChuaToiDa = table.Column<int>(type: "int", nullable: true),
                    GiaTheoNgay = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GiaTheoGio = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHONG", x => x.MaPhong);
                });

            migrationBuilder.CreateTable(
                name: "PHONG_BAN",
                columns: table => new
                {
                    MaPB = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenPB = table.Column<string>(type: "nvarchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHONG_BAN", x => x.MaPB);
                });

            migrationBuilder.CreateTable(
                name: "TAI_KHOAN",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenDangNhap = table.Column<string>(type: "varchar(50)", nullable: false),
                    MatKhau = table.Column<string>(type: "varchar(50)", nullable: false),
                    VaiTro = table.Column<string>(type: "varchar(50)", nullable: true),
                    TrangThai = table.Column<string>(type: "varchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TAI_KHOAN", x => x.MaTaiKhoan);
                });

            migrationBuilder.CreateTable(
                name: "THANH_TOAN",
                columns: table => new
                {
                    MaThanhToan = table.Column<string>(type: "varchar(20)", nullable: false),
                    HOA_DONMaHoaDon = table.Column<string>(type: "varchar(20)", nullable: true),
                    HOA_DON_NHA_HANGMaHoaDonNhaHang = table.Column<string>(type: "varchar(20)", nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    MaGiaoDichNgoai = table.Column<string>(type: "varchar(50)", nullable: true),
                    ThoiGianGiaoDich = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThaiGiaoDich = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_THANH_TOAN", x => x.MaThanhToan);
                });

            migrationBuilder.CreateTable(
                name: "TRANG_THAI_PHONG",
                columns: table => new
                {
                    MaTrangThai = table.Column<string>(type: "varchar(20)", nullable: false),
                    TrangThaiPhong = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    TrangThaiVeSinh = table.Column<string>(type: "nvarchar(50)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRANG_THAI_PHONG", x => x.MaTrangThai);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BAN_NHA_HANG");

            migrationBuilder.DropTable(
                name: "CHI_TIET_DAT_PHONG");

            migrationBuilder.DropTable(
                name: "CHI_TIET_DICH_VU");

            migrationBuilder.DropTable(
                name: "CHUC_VU");

            migrationBuilder.DropTable(
                name: "DAT_PHONG");

            migrationBuilder.DropTable(
                name: "DICH_VU");

            migrationBuilder.DropTable(
                name: "DOI_TAC");

            migrationBuilder.DropTable(
                name: "HOA_DON");

            migrationBuilder.DropTable(
                name: "HOA_DON_NHA_HANG");

            migrationBuilder.DropTable(
                name: "KHACH_HANG");

            migrationBuilder.DropTable(
                name: "KHO_VAT_TU");

            migrationBuilder.DropTable(
                name: "LICH_LAM_VIEC");

            migrationBuilder.DropTable(
                name: "NHAN_VIEN");

            migrationBuilder.DropTable(
                name: "PHIEU_BAO_CAO_SU_CO");

            migrationBuilder.DropTable(
                name: "PHIEU_LUU_TRU");

            migrationBuilder.DropTable(
                name: "PHONG");

            migrationBuilder.DropTable(
                name: "PHONG_BAN");

            migrationBuilder.DropTable(
                name: "TAI_KHOAN");

            migrationBuilder.DropTable(
                name: "THANH_TOAN");

            migrationBuilder.DropTable(
                name: "TRANG_THAI_PHONG");
        }
    }
}
