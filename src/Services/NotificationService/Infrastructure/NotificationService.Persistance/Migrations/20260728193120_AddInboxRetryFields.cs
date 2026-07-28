using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLettered",
                table: "InboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "InboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeadLettered",
                table: "InboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "InboxMessages");
        }
    }
}
