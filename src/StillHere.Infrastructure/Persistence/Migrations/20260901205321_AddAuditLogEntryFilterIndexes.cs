using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StillHere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogEntryFilterIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_ManagedDomainId",
                table: "AuditLogEntries");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ManagedDomainId_TimestampUtc",
                table: "AuditLogEntries",
                columns: new[] { "ManagedDomainId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_TimestampUtc",
                table: "AuditLogEntries",
                column: "TimestampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_ManagedDomainId_TimestampUtc",
                table: "AuditLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_TimestampUtc",
                table: "AuditLogEntries");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ManagedDomainId",
                table: "AuditLogEntries",
                column: "ManagedDomainId");
        }
    }
}
