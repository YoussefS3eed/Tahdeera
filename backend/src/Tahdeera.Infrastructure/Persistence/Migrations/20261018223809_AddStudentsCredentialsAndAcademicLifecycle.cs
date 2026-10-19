using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tahdeera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentsCredentialsAndAcademicLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_app_users_preferred_theme",
                table: "app_users");

            migrationBuilder.CreateTable(
                name: "academic_years",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_academic_years", x => x.id);
                    table.CheckConstraint("ck_academic_years_dates", "ends_on > starts_on");
                });

            migrationBuilder.CreateTable(
                name: "grades",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    verification_pin_hash = table.Column<byte[]>(type: "bytea", nullable: true),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_students", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    name_en = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subjects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "student_academic_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<short>(type: "smallint", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_academic_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_student_academic_history_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_academic_history_app_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_academic_history_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_academic_history_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    card_uid_display = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true),
                    issued_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_credentials", x => x.id);
                    table.CheckConstraint("ck_student_credentials_expires_at", "expires_at IS NULL OR expires_at > issued_at");
                    table.CheckConstraint("ck_student_credentials_revoked_at", "revoked_at IS NULL OR revoked_at >= issued_at");
                    table.CheckConstraint("ck_student_credentials_type", "credential_type IN ('QR', 'NFC', 'FallbackCode')");
                    table.ForeignKey(
                        name: "fk_student_credentials_app_users_issued_by_user_id",
                        column: x => x.issued_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_credentials_app_users_revoked_by_user_id",
                        column: x => x.revoked_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_credentials_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_credentials_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workspace_students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    student_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    guardian_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    guardian_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    whatsapp_opt_in = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    whatsapp_opt_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    seat_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    local_notes = table.Column<string>(type: "text", nullable: true),
                    enrolled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_students", x => x.id);
                    table.CheckConstraint("ck_workspace_students_status", "status IN ('Active', 'Inactive', 'Archived', 'Blocked')");
                    table.ForeignKey(
                        name: "fk_workspace_students_app_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_workspace_students_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workspace_students_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_app_users_preferred_theme",
                table: "app_users",
                sql: "preferred_theme IN ('light', 'dark')");

            migrationBuilder.CreateIndex(
                name: "ix_academic_years_name",
                table: "academic_years",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_one_current_academic_year",
                table: "academic_years",
                column: "is_current",
                unique: true,
                filter: "is_current = true");

            migrationBuilder.CreateIndex(
                name: "ix_grades_code",
                table: "grades",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grades_name_ar",
                table: "grades",
                column: "name_ar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_grades_name_en",
                table: "grades",
                column: "name_en",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_academic_history_academic_year_id",
                table: "student_academic_history",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_academic_history_grade_id",
                table: "student_academic_history",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_academic_history_lookup",
                table: "student_academic_history",
                columns: new[] { "student_id", "academic_year_id", "grade_id" });

            migrationBuilder.CreateIndex(
                name: "ix_student_academic_history_recorded_by_user_id",
                table: "student_academic_history",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_academic_history_student_id_academic_year_id",
                table: "student_academic_history",
                columns: new[] { "student_id", "academic_year_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_credentials_issued_by_user_id",
                table: "student_credentials",
                column: "issued_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_credentials_revoked_by_user_id",
                table: "student_credentials",
                column: "revoked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_credentials_student_active",
                table: "student_credentials",
                column: "student_id",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_one_active_nfc_per_student_workspace",
                table: "student_credentials",
                columns: new[] { "workspace_id", "student_id", "credential_type" },
                unique: true,
                filter: "revoked_at IS NULL AND credential_type = 'NFC'");

            migrationBuilder.CreateIndex(
                name: "ux_student_credentials_token_hash",
                table: "student_credentials",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_students_student_code",
                table: "students",
                column: "student_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subjects_code",
                table: "subjects",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subjects_name_ar",
                table: "subjects",
                column: "name_ar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subjects_name_en",
                table: "subjects",
                column: "name_en",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workspace_students_created_by_user_id",
                table: "workspace_students",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_students_guardian_phone",
                table: "workspace_students",
                column: "guardian_phone");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_students_student",
                table: "workspace_students",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_workspace_students_workspace_id_student_id",
                table: "workspace_students",
                columns: new[] { "workspace_id", "student_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_academic_history");

            migrationBuilder.DropTable(
                name: "student_credentials");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "workspace_students");

            migrationBuilder.DropTable(
                name: "academic_years");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropCheckConstraint(
                name: "ck_app_users_preferred_theme",
                table: "app_users");

            migrationBuilder.AddCheckConstraint(
                name: "ck_app_users_preferred_theme",
                table: "app_users",
                sql: "preferred_theme IN ('light', 'dark', 'system')");
        }
    }
}
