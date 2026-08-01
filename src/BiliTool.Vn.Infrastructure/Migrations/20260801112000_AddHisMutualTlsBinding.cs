using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHisMutualTlsBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "certificate_fingerprint",
                table: "his_api_clients",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "previous_certificate_expires_at",
                table: "his_api_clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "previous_certificate_fingerprint",
                table: "his_api_clients",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "require_mutual_tls",
                table: "his_api_clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "certificate_fingerprint",
                table: "his_api_clients");

            migrationBuilder.DropColumn(
                name: "previous_certificate_expires_at",
                table: "his_api_clients");

            migrationBuilder.DropColumn(
                name: "previous_certificate_fingerprint",
                table: "his_api_clients");

            migrationBuilder.DropColumn(
                name: "require_mutual_tls",
                table: "his_api_clients");
        }
    }
}
