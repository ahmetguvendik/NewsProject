using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceParentToInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                table: "InboxMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceParent",
                table: "InboxMessages");
        }
    }
}
