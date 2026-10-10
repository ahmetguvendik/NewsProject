using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsService.Persistance.Migrations
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

            // Akış sorgusu COALESCE ile sıralıyor; ifade indeksi EF modeliyle tanımlanamıyor.
            migrationBuilder.Sql(
                "CREATE INDEX \"IX_Articles_Feed\" ON \"Articles\" ((COALESCE(\"PublishedAt\", \"CreatedAt\")) DESC) WHERE NOT \"IsDeleted\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Articles_Feed\";");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_CreatedAt",
                table: "OutboxMessages");
        }
    }
}
