using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tahdeera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoAbsenceBufferMinutesToWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "auto_absence_buffer_minutes",
                table: "workspaces",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddCheckConstraint(
                name: "ck_workspaces_auto_absence_buffer",
                table: "workspaces",
                sql: "auto_absence_buffer_minutes >= 0 AND auto_absence_buffer_minutes <= 120");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_workspaces_auto_absence_buffer",
                table: "workspaces");

            migrationBuilder.DropColumn(
                name: "auto_absence_buffer_minutes",
                table: "workspaces");
        }
    }
}
