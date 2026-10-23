using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tahdeera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsSessionsAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    owner_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_year_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<short>(type: "smallint", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    room_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    schedule_summary = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    late_grace_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    default_monthly_fee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0.00m),
                    consecutive_absence_threshold = table.Column<short>(type: "smallint", nullable: true),
                    absence_action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NotifyOnly"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    responsible_teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_groups", x => x.id);
                    table.CheckConstraint("CK_groups_absence_action", "absence_action IN ('NotifyOnly', 'MarkSuspended', 'Archive')");
                    table.CheckConstraint("CK_groups_consecutive_absence_threshold", "consecutive_absence_threshold IS NULL OR (consecutive_absence_threshold > 0 AND consecutive_absence_threshold <= 50)");
                    table.CheckConstraint("CK_groups_default_monthly_fee", "default_monthly_fee >= 0");
                    table.CheckConstraint("CK_groups_late_grace_minutes", "late_grace_minutes >= 0 AND late_grace_minutes <= 1440");
                    table.CheckConstraint("CK_groups_status", "status IN ('Active', 'Paused', 'Archived')");
                    table.ForeignKey(
                        name: "fk_groups_academic_years_academic_year_id",
                        column: x => x.academic_year_id,
                        principalTable: "academic_years",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_groups_app_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_groups_app_users_responsible_teacher_user_id",
                        column: x => x.responsible_teacher_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_groups_grades_grade_id",
                        column: x => x.grade_id,
                        principalTable: "grades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_groups_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_groups_workspaces_owner_workspace_id",
                        column: x => x.owner_workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_attendance_delegations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assistant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_by_teacher_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_attendance_delegations", x => x.id);
                    table.CheckConstraint("CK_group_attendance_delegations_revoked_at", "revoked_at IS NULL OR revoked_at >= granted_at");
                    table.ForeignKey(
                        name: "fk_group_attendance_delegations_app_users_assistant_user_id",
                        column: x => x.assistant_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_attendance_delegations_app_users_granted_by_teacher_u",
                        column: x => x.granted_by_teacher_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_attendance_delegations_app_users_revoked_by_user_id",
                        column: x => x.revoked_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_attendance_delegations_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    left_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_students", x => x.id);
                    table.CheckConstraint("CK_group_students_active_left_at", "(status = 'Active' AND left_at IS NULL) OR status <> 'Active'");
                    table.CheckConstraint("CK_group_students_left_at", "left_at IS NULL OR left_at >= joined_at");
                    table.CheckConstraint("CK_group_students_status", "status IN ('Active', 'Withdrawn', 'Suspended')");
                    table.ForeignKey(
                        name: "fk_group_students_app_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_students_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_students_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_workspace_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_view_roster = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_register_attendance = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_edit_attendance = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    granted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_workspace_access", x => x.id);
                    table.CheckConstraint("CK_group_workspace_access_revoked_at", "revoked_at IS NULL OR revoked_at >= granted_at");
                    table.ForeignKey(
                        name: "fk_group_workspace_access_app_users_granted_by_user_id",
                        column: x => x.granted_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_workspace_access_app_users_revoked_by_user_id",
                        column: x => x.revoked_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_workspace_access_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_workspace_access_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    late_grace_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    auto_absence_processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.CheckConstraint("CK_sessions_cancellation", "(status = 'Cancelled' AND cancellation_reason IS NOT NULL) OR (status <> 'Cancelled' AND cancellation_reason IS NULL)");
                    table.CheckConstraint("CK_sessions_ends_at", "ends_at > starts_at");
                    table.CheckConstraint("CK_sessions_late_grace_minutes", "late_grace_minutes >= 0 AND late_grace_minutes <= 1440");
                    table.CheckConstraint("CK_sessions_status", "status IN ('Scheduled', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "fk_sessions_app_users_cancelled_by_user_id",
                        column: x => x.cancelled_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_sessions_app_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_sessions_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_student_status_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_student_status_history", x => x.id);
                    table.CheckConstraint("CK_group_student_status_history_new_status", "new_status IN ('Active', 'Withdrawn', 'Suspended')");
                    table.CheckConstraint("CK_group_student_status_history_old_status", "old_status IS NULL OR old_status IN ('Active', 'Withdrawn', 'Suspended')");
                    table.ForeignKey(
                        name: "fk_group_student_status_history_app_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_group_student_status_history_group_students_group_student_id",
                        column: x => x.group_student_id,
                        principalTable: "group_students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    auto_evaluation_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    check_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_modified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    retroactive_reason = table.Column<string>(type: "text", nullable: true),
                    system_generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    voided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    void_reason = table.Column<string>(type: "text", nullable: true),
                    is_guest_makeup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    home_group_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_records", x => x.id);
                    table.CheckConstraint("CK_attendance_records_check_in", "(status IN ('Present', 'Late') AND check_in_at IS NOT NULL) OR status IN ('Absent', 'Excused')");
                    table.CheckConstraint("CK_attendance_records_retroactive_reason", "retroactive_reason IS NULL OR length(trim(retroactive_reason)) > 0");
                    table.CheckConstraint("CK_attendance_records_source", "source IN ('System', 'User')");
                    table.CheckConstraint("CK_attendance_records_source_user", "(source = 'System' AND recorded_by_user_id IS NULL) OR (source = 'User' AND recorded_by_user_id IS NOT NULL)");
                    table.CheckConstraint("CK_attendance_records_status", "status IN ('Present', 'Absent', 'Late', 'Excused')");
                    table.CheckConstraint("CK_attendance_records_void", "voided_at IS NULL OR void_reason IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_attendance_records_app_users_last_modified_by_user_id",
                        column: x => x.last_modified_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_attendance_records_app_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_attendance_records_groups_home_group_id",
                        column: x => x.home_group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_attendance_records_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_records_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_records_workspaces_owner_workspace_id",
                        column: x => x.owner_workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attendance_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    old_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    new_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reason = table.Column<string>(type: "text", nullable: true),
                    change_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_history", x => x.id);
                    table.CheckConstraint("CK_attendance_history_change_kind", "change_kind IN ('InitialSystemAbsence', 'InitialUserEntry', 'ManualCorrection', 'SystemReevaluation', 'VoidedOnCancellation', 'RestoredFromCancellation', 'Other')");
                    table.CheckConstraint("CK_attendance_history_new_source", "new_source IN ('System', 'User')");
                    table.CheckConstraint("CK_attendance_history_new_status", "new_status IN ('Present', 'Absent', 'Late', 'Excused')");
                    table.CheckConstraint("CK_attendance_history_old_source", "old_source IS NULL OR old_source IN ('System', 'User')");
                    table.CheckConstraint("CK_attendance_history_old_status", "old_status IS NULL OR old_status IN ('Present', 'Absent', 'Late', 'Excused')");
                    table.ForeignKey(
                        name: "fk_attendance_history_app_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_attendance_history_attendance_records_attendance_record_id",
                        column: x => x.attendance_record_id,
                        principalTable: "attendance_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attendance_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    exam_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ar"),
                    message_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    broker_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    failure_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    max_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)5),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_outbox", x => x.id);
                    table.CheckConstraint("CK_message_outbox_attempts", "attempts >= 0 AND attempts <= 10");
                    table.CheckConstraint("CK_message_outbox_failure_category", "failure_category IS NULL OR failure_category IN ('Transient', 'Permanent')");
                    table.CheckConstraint("CK_message_outbox_language_code", "language_code IN ('ar', 'en')");
                    table.CheckConstraint("CK_message_outbox_max_attempts", "max_attempts >= 1 AND max_attempts <= 10");
                    table.CheckConstraint("CK_message_outbox_message_type", "message_type IN ('AttendanceCheckIn', 'AbsenceAlert', 'ExamResult', 'WeeklyParentReport', 'PaymentReceipt')");
                    table.CheckConstraint("CK_message_outbox_status", "status IN ('Pending', 'PublishedToBroker', 'Processing', 'Sent', 'Delivered', 'FailedTransient', 'FailedPermanent', 'Cancelled')");
                    table.ForeignKey(
                        name: "fk_message_outbox_attendance_records_attendance_record_id",
                        column: x => x.attendance_record_id,
                        principalTable: "attendance_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_message_outbox_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_message_outbox_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_history_changed_by_user_id",
                table: "attendance_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_history_record_time",
                table: "attendance_history",
                columns: new[] { "attendance_record_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_auto_reevaluate",
                table: "attendance_records",
                column: "session_id",
                filter: "auto_evaluation_locked = false AND voided_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_guest_makeup",
                table: "attendance_records",
                columns: new[] { "student_id", "is_guest_makeup" },
                filter: "is_guest_makeup = true");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_owner_session_active",
                table: "attendance_records",
                columns: new[] { "owner_workspace_id", "session_id" },
                filter: "voided_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_home_group_id",
                table: "attendance_records",
                column: "home_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_last_modified_by_user_id",
                table: "attendance_records",
                column: "last_modified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_recorded_by_user_id",
                table: "attendance_records",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_session_id_student_id",
                table: "attendance_records",
                columns: new[] { "session_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_student_recorded_active",
                table: "attendance_records",
                columns: new[] { "student_id", "recorded_at" },
                descending: new[] { false, true },
                filter: "voided_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_attendance_delegations_granted_by_teacher_user_id",
                table: "group_attendance_delegations",
                column: "granted_by_teacher_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_attendance_delegations_revoked_by_user_id",
                table: "group_attendance_delegations",
                column: "revoked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_delegation_assistant",
                table: "group_attendance_delegations",
                column: "assistant_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_one_active_attendance_delegate_per_group",
                table: "group_attendance_delegations",
                column: "group_id",
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_student_history_membership_time",
                table: "group_student_status_history",
                columns: new[] { "group_student_id", "effective_at" });

            migrationBuilder.CreateIndex(
                name: "ix_group_student_status_history_changed_by_user_id",
                table: "group_student_status_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_students_group_id_student_id",
                table: "group_students",
                columns: new[] { "group_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_students_group_status",
                table: "group_students",
                columns: new[] { "group_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_group_students_student",
                table: "group_students",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_students_updated_by_user_id",
                table: "group_students",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_workspace_access_granted_by_user_id",
                table: "group_workspace_access",
                column: "granted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_workspace_access_group_id_workspace_id",
                table: "group_workspace_access",
                columns: new[] { "group_id", "workspace_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_workspace_access_revoked_by_user_id",
                table: "group_workspace_access",
                column: "revoked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_workspace_access_workspace",
                table: "group_workspace_access",
                column: "workspace_id",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_groups_academic_year_id",
                table: "groups",
                column: "academic_year_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_created_by_user_id",
                table: "groups",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_grade_id",
                table: "groups",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_owner_status",
                table: "groups",
                columns: new[] { "owner_workspace_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_groups_owner_workspace_id_academic_year_id_name",
                table: "groups",
                columns: new[] { "owner_workspace_id", "academic_year_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_groups_responsible_teacher",
                table: "groups",
                column: "responsible_teacher_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_groups_subject_id",
                table: "groups",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_attendance",
                table: "message_outbox",
                column: "attendance_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_idempotency_key",
                table: "message_outbox",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_relay",
                table: "message_outbox",
                columns: new[] { "status", "next_attempt_at" },
                filter: "status IN ('Pending', 'FailedTransient')");

            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_student_id",
                table: "message_outbox",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_workspace_created",
                table: "message_outbox",
                columns: new[] { "workspace_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_cancelled_by_user_id",
                table: "sessions",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_created_by_user_id",
                table: "sessions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_due_for_attendance",
                table: "sessions",
                column: "ends_at",
                filter: "status <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "ix_sessions_group_start",
                table: "sessions",
                columns: new[] { "group_id", "starts_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_history");

            migrationBuilder.DropTable(
                name: "group_attendance_delegations");

            migrationBuilder.DropTable(
                name: "group_student_status_history");

            migrationBuilder.DropTable(
                name: "group_workspace_access");

            migrationBuilder.DropTable(
                name: "message_outbox");

            migrationBuilder.DropTable(
                name: "group_students");

            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "groups");
        }
    }
}
