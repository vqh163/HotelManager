using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangBaoCaoDoanhThu_UC12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BAO_CAO_DOANH_THU",
                columns: table => new
                {
                    MaBaoCao = table.Column<string>(type: "varchar(20)", nullable: false),
                    MaDatPhong = table.Column<string>(type: "varchar(20)", nullable: false),
                    PhanHe = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    LoaiPhong = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    TenPhong = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    TenKhach = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    NgayDen = table.Column<DateTime>(type: "datetime", nullable: true),
                    NgayDi = table.Column<DateTime>(type: "datetime", nullable: true),
                    TienPhong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DichVu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChietKhau = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongDoanhThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NoTruoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienDaThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    BaoGomThuePhi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BAO_CAO_DOANH_THU", x => x.MaBaoCao);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BAO_CAO_DOANH_THU");
        }
    }
}
