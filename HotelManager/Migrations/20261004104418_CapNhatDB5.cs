using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class CapNhatDB5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Checklist",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CongCu",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DoUuTien",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HinhAnh",
                table: "LICH_LAM_VIEC",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Checklist",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "CongCu",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "DoUuTien",
                table: "LICH_LAM_VIEC");

            migrationBuilder.DropColumn(
                name: "HinhAnh",
                table: "LICH_LAM_VIEC");
        }
    }
}
