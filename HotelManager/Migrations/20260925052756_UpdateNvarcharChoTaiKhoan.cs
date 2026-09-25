using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNvarcharChoTaiKhoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VaiTro",
                table: "TAI_KHOAN",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "TAI_KHOAN",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VaiTro",
                table: "TAI_KHOAN",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "TAI_KHOAN",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);
        }
    }
}
