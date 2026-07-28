using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class RemoveArticleAuthorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Articles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Articles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
