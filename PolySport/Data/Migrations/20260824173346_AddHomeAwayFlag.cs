using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolySport.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeAwayFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHomeGame",
                table: "Matches",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHomeGame",
                table: "Matches");
        }
    }
}
