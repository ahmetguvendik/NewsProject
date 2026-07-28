using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLettered",
                table: "OutboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeadLettered",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "OutboxMessages");
        }
    }
}
