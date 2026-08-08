using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewsService.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleNotifySubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifySubscribers",
                table: "Articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifySubscribers",
                table: "Articles");
        }
    }
}
