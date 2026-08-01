using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHisIdempotencyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "his_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    api_client_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    result_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    response_status_code = table.Column<int>(type: "integer", nullable: true),
                    response_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_his_idempotency_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_his_idempotency_expires_at",
                table: "his_idempotency_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ux_his_idempotency_client_key",
                table: "his_idempotency_records",
                columns: new[] { "tenant_id", "api_client_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "his_idempotency_records");
        }
    }
}
