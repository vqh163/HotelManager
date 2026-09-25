using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelManager.Migrations
{
    /// <inheritdoc />
    public partial class NapDuLieuChucVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CHUC_VU",
                columns: new[] { "MaChucVu", "PHONG_BANMaPB", "TenChucVu" },
                values: new object[,]
                {
                    { "CV01", null, "Quản trị viên" },
                    { "CV02", null, "Quản lý" },
                    { "CV03", null, "Lễ tân" },
                    { "CV04", null, "Nhân viên Buồng phòng" },
                    { "CV05", null, "Nhân viên Kỹ thuật" },
                    { "CV06", null, "Thu ngân Nhà hàng" },
                    { "CV07", null, "Phục vụ Nhà hàng" },
                    { "CV08", null, "Đầu bếp" },
                    { "CV09", null, "Thủ kho" },
                    { "CV10", null, "Sales" },
                    { "CV11", null, "Kế toán" },
                    { "CV12", null, "Giám đốc" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV01");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV02");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV03");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV04");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV05");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV06");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV07");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV08");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV09");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV10");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV11");

            migrationBuilder.DeleteData(
                table: "CHUC_VU",
                keyColumn: "MaChucVu",
                keyValue: "CV12");
        }
    }
}
