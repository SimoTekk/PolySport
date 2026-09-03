using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolySport.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalkeeperFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGoalkeeper",
                table: "MatchPlayers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGoalkeeper",
                table: "MatchPlayers");
        }
    }
}
