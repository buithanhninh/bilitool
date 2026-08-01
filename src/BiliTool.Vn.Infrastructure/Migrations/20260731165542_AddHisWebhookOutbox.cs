using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiliTool.Vn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHisWebhookOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "his_webhook_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    api_client_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    endpoint_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    secret_protected = table.Column<string>(type: "text", nullable: false),
                    event_types = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_his_webhook_subscriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "his_outbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    api_client_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    result_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    lock_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_his_outbox_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_his_outbox_events_his_webhook_subscriptions_webhook_subscri~",
                        column: x => x.webhook_subscription_id,
                        principalTable: "his_webhook_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_his_outbox_delivery_queue",
                table: "his_outbox_events",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_his_outbox_events_webhook_subscription_id",
                table: "his_outbox_events",
                column: "webhook_subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_his_outbox_result_id",
                table: "his_outbox_events",
                column: "result_id");

            migrationBuilder.CreateIndex(
                name: "ux_his_webhook_subscription_endpoint",
                table: "his_webhook_subscriptions",
                columns: new[] { "tenant_id", "api_client_id", "endpoint_url" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "his_outbox_events");

            migrationBuilder.DropTable(
                name: "his_webhook_subscriptions");
        }
    }
}
