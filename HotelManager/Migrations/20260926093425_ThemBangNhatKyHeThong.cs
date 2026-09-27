using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangNhatKyHeThong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NHAT_KY_HE_THONG",
                columns: table => new
                {
                    MaNhatKy = table.Column<string>(type: "varchar(20)", nullable: false),
                    TenDangNhap = table.Column<string>(type: "varchar(50)", nullable: true),
                    HanhDong = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChiTiet = table.Column<string>(type: "nvarchar(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NHAT_KY_HE_THONG", x => x.MaNhatKy);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NHAT_KY_HE_THONG");
        }
    }
}
