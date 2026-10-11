using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CaseInsensitiveUniqueNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            // Büyük/küçük harf farkı aynı ad sayılır; silinen kaydın adı yeniden kullanılabilir.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Categories_Name\" ON \"Categories\" (lower(\"Name\")) WHERE NOT \"IsDeleted\";");
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Tags_Name\" ON \"Tags\" (lower(\"Name\")) WHERE NOT \"IsDeleted\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Categories_Name\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Tags_Name\";");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }
    }
}
