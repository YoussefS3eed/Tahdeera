using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tahdeera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTuitionReminderMessageType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_message_outbox_message_type",
                table: "message_outbox");

            migrationBuilder.AddCheckConstraint(
                name: "CK_message_outbox_message_type",
                table: "message_outbox",
                sql: "message_type IN ('AttendanceCheckIn', 'AbsenceAlert', 'ExamResult', 'WeeklyParentReport', 'PaymentReceipt', 'AttendanceCorrection', 'TuitionReminder')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_message_outbox_message_type",
                table: "message_outbox");

            migrationBuilder.AddCheckConstraint(
                name: "CK_message_outbox_message_type",
                table: "message_outbox",
                sql: "message_type IN ('AttendanceCheckIn', 'AbsenceAlert', 'ExamResult', 'WeeklyParentReport', 'PaymentReceipt', 'AttendanceCorrection')");
        }
    }
}
