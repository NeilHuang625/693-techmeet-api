using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace techmeet_api.Migrations
{
    /// <inheritdoc />
    public partial class offlineMessage1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "OfflineMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "OfflineMessages");
        }
    }
}
