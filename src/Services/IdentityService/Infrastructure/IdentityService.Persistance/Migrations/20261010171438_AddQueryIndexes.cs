using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CreatedAt",
                table: "OutboxMessages",
                column: "CreatedAt",
                filter: "\"IsProcessed\" = false AND \"IsDeadLettered\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_CreatedAt",
                table: "OutboxMessages");
        }
    }
}
