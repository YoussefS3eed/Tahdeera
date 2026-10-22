using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tahdeera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFinanceLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EGP"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_student_accounts_app_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_accounts_workspace_students_workspace_student_id",
                        column: x => x.workspace_student_id,
                        principalTable: "workspace_students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workspace_receipt_sequences",
                columns: table => new
                {
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year_month = table.Column<int>(type: "integer", nullable: false),
                    next_sequence = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_receipt_sequences", x => new { x.workspace_id, x.year_month });
                    table.ForeignKey(
                        name: "fk_workspace_receipt_sequences_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    billing_period_label = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_charges", x => x.id);
                    table.CheckConstraint("CK_student_charges_amount_positive", "amount > 0");
                    table.CheckConstraint("CK_student_charges_cancellation_reason", "status = 'Open' OR cancellation_reason IS NOT NULL");
                    table.CheckConstraint("CK_student_charges_status", "status IN ('Open', 'Cancelled', 'WrittenOff')");
                    table.ForeignKey(
                        name: "fk_student_charges_app_users_cancelled_by_user_id",
                        column: x => x.cancelled_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_charges_app_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_charges_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_charges_student_accounts_student_account_id",
                        column: x => x.student_account_id,
                        principalTable: "student_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Received"),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_reason = table.Column<string>(type: "text", nullable: true),
                    reverses_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_payments", x => x.id);
                    table.CheckConstraint("CK_student_payments_amount_positive", "amount > 0");
                    table.CheckConstraint("CK_student_payments_method", "method IN ('Cash', 'BankTransfer', 'Wallet', 'Card', 'Other')");
                    table.CheckConstraint("CK_student_payments_reversal_validity", "(status = 'Received' AND reversed_at IS NULL) OR (status = 'Reversed' AND reversed_at IS NOT NULL AND reversal_reason IS NOT NULL)");
                    table.CheckConstraint("CK_student_payments_status", "status IN ('Received', 'Reversed')");
                    table.ForeignKey(
                        name: "fk_student_payments_app_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_payments_app_users_reversed_by_user_id",
                        column: x => x.reversed_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_payments_student_accounts_student_account_id",
                        column: x => x.student_account_id,
                        principalTable: "student_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_payments_student_payments_reverses_payment_id",
                        column: x => x.reverses_payment_id,
                        principalTable: "student_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_reversed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                    table.CheckConstraint("CK_payment_allocations_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_allocations_student_charges_charge_id",
                        column: x => x.charge_id,
                        principalTable: "student_charges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_student_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "student_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_charge",
                table: "payment_allocations",
                column: "charge_id",
                filter: "is_reversed = false");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_payment_id_charge_id",
                table: "payment_allocations",
                columns: new[] { "payment_id", "charge_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_accounts_created_by_user_id",
                table: "student_accounts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_accounts_workspace_student_id",
                table: "student_accounts",
                column: "workspace_student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_charges_account_status",
                table: "student_charges",
                columns: new[] { "student_account_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_student_charges_cancelled_by_user_id",
                table: "student_charges",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_charges_created_by_user_id",
                table: "student_charges",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_charges_group_id",
                table: "student_charges",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_payments_account_date",
                table: "student_payments",
                columns: new[] { "student_account_id", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "ix_student_payments_recorded_by_user_id",
                table: "student_payments",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_payments_reversed_by_user_id",
                table: "student_payments",
                column: "reversed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_payments_reverses_payment_id",
                table: "student_payments",
                column: "reverses_payment_id");

            migrationBuilder.CreateIndex(
                name: "ux_student_payments_receipt_number",
                table: "student_payments",
                column: "receipt_number",
                unique: true);

            // Task T028: Idempotent backfill for existing workspace students
            migrationBuilder.Sql(@"
                INSERT INTO student_accounts (id, workspace_student_id, currency, created_at)
                SELECT gen_random_uuid(), id, 'EGP', now()
                FROM workspace_students
                ON CONFLICT (workspace_student_id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "workspace_receipt_sequences");

            migrationBuilder.DropTable(
                name: "student_charges");

            migrationBuilder.DropTable(
                name: "student_payments");

            migrationBuilder.DropTable(
                name: "student_accounts");
        }
    }
}
