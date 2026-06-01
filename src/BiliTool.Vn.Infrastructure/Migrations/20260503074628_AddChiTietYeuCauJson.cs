using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChiTietYeuCauJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChiTietYeuCauJson",
                table: "lich_su_tinh_toan",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AnhChiBiVangDaCanChieuDen",
                table: "ho_so_benh_nhan",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BenhTanHuyetABO",
                table: "ho_so_benh_nhan",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BenhTanHuyetRh",
                table: "ho_so_benh_nhan",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MeBuMeHoanToan",
                table: "ho_so_benh_nhan",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VangDaTrong24hDau",
                table: "ho_so_benh_nhan",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChiTietYeuCauJson",
                table: "lich_su_tinh_toan");

            migrationBuilder.DropColumn(
                name: "AnhChiBiVangDaCanChieuDen",
                table: "ho_so_benh_nhan");

            migrationBuilder.DropColumn(
                name: "BenhTanHuyetABO",
                table: "ho_so_benh_nhan");

            migrationBuilder.DropColumn(
                name: "BenhTanHuyetRh",
                table: "ho_so_benh_nhan");

            migrationBuilder.DropColumn(
                name: "MeBuMeHoanToan",
                table: "ho_so_benh_nhan");

            migrationBuilder.DropColumn(
                name: "VangDaTrong24hDau",
                table: "ho_so_benh_nhan");
        }
    }
}
