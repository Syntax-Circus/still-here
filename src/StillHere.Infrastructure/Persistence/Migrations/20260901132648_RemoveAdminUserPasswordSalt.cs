using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StillHere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdminUserPasswordSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "AdminUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordSalt",
                table: "AdminUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
