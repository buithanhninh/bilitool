using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHisTenantClientIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "engine_version",
                table: "clinical_audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "result_id",
                table: "clinical_audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "clinical_audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "his_tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_his_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "his_api_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    key_fingerprint = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    api_key_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    scopes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_his_api_clients", x => x.id);
                    table.ForeignKey(
                        name: "FK_his_api_clients_his_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "his_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clinical_audit_logs_tenant_result",
                table: "clinical_audit_logs",
                columns: new[] { "tenant_id", "result_id" });

            migrationBuilder.CreateIndex(
                name: "ix_his_api_clients_key_fingerprint",
                table: "his_api_clients",
                column: "key_fingerprint");

            migrationBuilder.CreateIndex(
                name: "ux_his_api_clients_tenant_client_code",
                table: "his_api_clients",
                columns: new[] { "tenant_id", "client_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_his_tenants_code",
                table: "his_tenants",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "his_api_clients");

            migrationBuilder.DropTable(
                name: "his_tenants");

            migrationBuilder.DropIndex(
                name: "ix_clinical_audit_logs_tenant_result",
                table: "clinical_audit_logs");

            migrationBuilder.DropColumn(
                name: "engine_version",
                table: "clinical_audit_logs");

            migrationBuilder.DropColumn(
                name: "result_id",
                table: "clinical_audit_logs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "clinical_audit_logs");
        }
    }
}
