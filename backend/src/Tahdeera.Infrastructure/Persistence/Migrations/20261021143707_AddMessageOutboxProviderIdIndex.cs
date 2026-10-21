using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tahdeera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageOutboxProviderIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_message_outbox_provider_message_id",
                table: "message_outbox",
                column: "provider_message_id",
                filter: "provider_message_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_message_outbox_provider_message_id",
                table: "message_outbox");
        }
    }
}
