using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHisCredentialRotationOverlap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "previous_api_key_hash",
                table: "his_api_clients",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "previous_key_expires_at",
                table: "his_api_clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "previous_key_fingerprint",
                table: "his_api_clients",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_his_api_clients_previous_key_fingerprint",
                table: "his_api_clients",
                column: "previous_key_fingerprint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_his_api_clients_previous_key_fingerprint",
                table: "his_api_clients");

            migrationBuilder.DropColumn(
                name: "previous_api_key_hash",
                table: "his_api_clients");

            migrationBuilder.DropColumn(
                name: "previous_key_expires_at",
                table: "his_api_clients");

            migrationBuilder.DropColumn(
                name: "previous_key_fingerprint",
                table: "his_api_clients");
        }
    }
}
