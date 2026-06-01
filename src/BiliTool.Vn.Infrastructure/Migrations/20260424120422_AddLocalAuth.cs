using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "ho_so_nguoi_dung_pkey",
                table: "ho_so_nguoi_dung");

            migrationBuilder.RenameColumn(
                name: "google_id",
                table: "ho_so_nguoi_dung",
                newName: "id");

            migrationBuilder.AddColumn<string>(
                name: "google_id",
                table: "ho_so_nguoi_dung",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
            
            // Migrate existing google_ids correctly
            migrationBuilder.Sql("UPDATE ho_so_nguoi_dung SET google_id = id");

            migrationBuilder.AddColumn<bool>(
                name: "is_email_verified",
                table: "ho_so_nguoi_dung",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "otp_code",
                table: "ho_so_nguoi_dung",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "otp_expiry_time",
                table: "ho_so_nguoi_dung",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "ho_so_nguoi_dung",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "salt",
                table: "ho_so_nguoi_dung",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ho_so_nguoi_dung",
                table: "ho_so_nguoi_dung",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ho_so_nguoi_dung",
                table: "ho_so_nguoi_dung");

            migrationBuilder.DropColumn(
                name: "id",
                table: "ho_so_nguoi_dung");

            migrationBuilder.DropColumn(
                name: "is_email_verified",
                table: "ho_so_nguoi_dung");

            migrationBuilder.DropColumn(
                name: "otp_code",
                table: "ho_so_nguoi_dung");

            migrationBuilder.DropColumn(
                name: "otp_expiry_time",
                table: "ho_so_nguoi_dung");

            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "ho_so_nguoi_dung");

            migrationBuilder.DropColumn(
                name: "salt",
                table: "ho_so_nguoi_dung");

            migrationBuilder.AlterColumn<string>(
                name: "google_id",
                table: "ho_so_nguoi_dung",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ho_so_nguoi_dung",
                table: "ho_so_nguoi_dung",
                column: "google_id");
        }
    }
}
