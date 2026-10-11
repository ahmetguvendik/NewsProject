using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReferenceId_Type",
                table: "Notifications",
                columns: new[] { "ReferenceId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_MessageId",
                table: "InboxMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ReceivedAt",
                table: "InboxMessages",
                column: "ReceivedAt",
                filter: "\"IsProcessed\" = false AND \"IsDeadLettered\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_ReferenceId_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_MessageId",
                table: "InboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_InboxMessages_ReceivedAt",
                table: "InboxMessages");
        }
    }
}
