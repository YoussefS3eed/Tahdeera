using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tahdeera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppOutboxAndHybridSenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_message_outbox_failure_category",
                table: "message_outbox");

            migrationBuilder.AlterColumn<string>(
                name: "failure_category",
                table: "message_outbox",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "broker_message_id",
                table: "message_outbox",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "message_outbox",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<decimal>(
                name: "cost_egp",
                table: "message_outbox",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "lease_recovery_attempts",
                table: "message_outbox",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<Guid>(
                name: "sender_connection_id",
                table: "message_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sender_mode",
                table: "message_outbox",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PlatformManaged");

            migrationBuilder.AddColumn<Guid>(
                name: "session_id",
                table: "message_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                table: "message_outbox",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "whatsapp_sender_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownership_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    waba_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    verified_business_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    encrypted_access_token = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    connected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    disconnected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_sender_connections", x => x.id);
                    table.CheckConstraint("chk_whatsapp_sender_ownership", "ownership_type IN ('Platform', 'Workspace')");
                    table.CheckConstraint("chk_whatsapp_sender_phone", "display_phone_number ~ '^\\+?[0-9\\s\\-]+$'");
                    table.CheckConstraint("chk_whatsapp_sender_status", "status IN ('PendingSetup', 'Connected', 'Disconnected', 'Restricted', 'Failed')");
                    table.ForeignKey(
                        name: "fk_whatsapp_sender_connections_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_whatsapp_usage_ledgers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year_month = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    messages_dispatched_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    messages_delivered_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    messages_failed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    estimated_cost_usd = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false, defaultValue: 0m),
                    estimated_cost_egp = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false, defaultValue: 0m),
                    rate_card_snapshot_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_whatsapp_usage_ledgers", x => x.id);
                    table.CheckConstraint("chk_whatsapp_usage_year_month", "year_month ~ '^[0-9]{4}-(0[1-9]|1[0-2])$'");
                    table.ForeignKey(
                        name: "fk_workspace_whatsapp_usage_ledgers_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_whatsapp_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_mode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    monthly_message_quota = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    messages_used_this_month = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    active_sender_connection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_whatsapp_configs", x => x.id);
                    table.CheckConstraint("chk_whatsapp_monthly_quota", "monthly_message_quota >= 0");
                    table.CheckConstraint("chk_whatsapp_sender_mode", "sender_mode IN ('PlatformManaged', 'WorkspaceOwned', 'Disabled')");
                    table.CheckConstraint("chk_whatsapp_used_messages", "messages_used_this_month >= 0");
                    table.ForeignKey(
                        name: "fk_workspace_whatsapp_configs_whatsapp_sender_connections_acti",
                        column: x => x.active_sender_connection_id,
                        principalTable: "whatsapp_sender_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_workspace_whatsapp_configs_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_sender_connection_id",
                table: "message_outbox",
                column: "sender_connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_session",
                table: "message_outbox",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_sender_active",
                table: "whatsapp_sender_connections",
                column: "workspace_id",
                filter: "status = 'Connected'");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_whatsapp_configs_active_sender_connection_id",
                table: "workspace_whatsapp_configs",
                column: "active_sender_connection_id");

            migrationBuilder.CreateIndex(
                name: "ux_workspace_whatsapp_configs_workspace_id",
                table: "workspace_whatsapp_configs",
                column: "workspace_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_usage_workspace_month",
                table: "workspace_whatsapp_usage_ledgers",
                columns: new[] { "workspace_id", "year_month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_message_outbox_sessions_session_id",
                table: "message_outbox",
                column: "session_id",
                principalTable: "sessions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_message_outbox_whats_app_sender_connections_sender_connecti",
                table: "message_outbox",
                column: "sender_connection_id",
                principalTable: "whatsapp_sender_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_outbox_sessions_session_id",
                table: "message_outbox");

            migrationBuilder.DropForeignKey(
                name: "fk_message_outbox_whats_app_sender_connections_sender_connecti",
                table: "message_outbox");

            migrationBuilder.DropTable(
                name: "workspace_whatsapp_configs");

            migrationBuilder.DropTable(
                name: "workspace_whatsapp_usage_ledgers");

            migrationBuilder.DropTable(
                name: "whatsapp_sender_connections");

            migrationBuilder.DropIndex(
                name: "ix_message_outbox_sender_connection_id",
                table: "message_outbox");

            migrationBuilder.DropIndex(
                name: "ix_message_outbox_session",
                table: "message_outbox");

            migrationBuilder.DropColumn(
                name: "cost_egp",
                table: "message_outbox");

            migrationBuilder.DropColumn(
                name: "lease_recovery_attempts",
                table: "message_outbox");

            migrationBuilder.DropColumn(
                name: "sender_connection_id",
                table: "message_outbox");

            migrationBuilder.DropColumn(
                name: "sender_mode",
                table: "message_outbox");

            migrationBuilder.DropColumn(
                name: "session_id",
                table: "message_outbox");

            migrationBuilder.DropColumn(
                name: "template_name",
                table: "message_outbox");

            migrationBuilder.AlterColumn<string>(
                name: "failure_category",
                table: "message_outbox",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "broker_message_id",
                table: "message_outbox",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "message_outbox",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddCheckConstraint(
                name: "CK_message_outbox_failure_category",
                table: "message_outbox",
                sql: "failure_category IS NULL OR failure_category IN ('Transient', 'Permanent')");
        }
    }
}
