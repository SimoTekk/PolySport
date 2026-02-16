using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolySport.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserPlayerEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Users_AssistId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Users_ScorerId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayers_Users_UserId",
                table: "MatchPlayers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Players");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Players",
                table: "Players",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Players_AssistId",
                table: "Goals",
                column: "AssistId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Players_ScorerId",
                table: "Goals",
                column: "ScorerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayers_Players_UserId",
                table: "MatchPlayers",
                column: "UserId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Players_AssistId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Players_ScorerId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchPlayers_Players_UserId",
                table: "MatchPlayers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Players",
                table: "Players");

            migrationBuilder.RenameTable(
                name: "Players",
                newName: "Users");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Users_AssistId",
                table: "Goals",
                column: "AssistId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Users_ScorerId",
                table: "Goals",
                column: "ScorerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchPlayers_Users_UserId",
                table: "MatchPlayers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
