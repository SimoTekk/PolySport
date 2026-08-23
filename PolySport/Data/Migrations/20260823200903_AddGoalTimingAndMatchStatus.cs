using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolySport.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalTimingAndMatchStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Neue Spalten für Zeitangabe und Gegentore anlegen
            migrationBuilder.AddColumn<bool>(
                name: "IsOpponentGoal",
                table: "Goals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "Goals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinuteInPeriod",
                table: "Goals",
                type: "int",
                nullable: true);

            // Gegentore haben keinen Schützen aus unserem Kader
            migrationBuilder.AlterColumn<int>(
                name: "ScorerId",
                table: "Goals",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // 2. Status am Match
            migrationBuilder.AddColumn<bool>(
                name: "IsFinished",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "Matches",
                type: "datetime2",
                nullable: true);

            // 3. Bisherige Gegentore aus dem Zähler OpponentScore in einzelne
            //    Datensätze überführen – ohne Zeitangabe, die ist nicht bekannt.
            //    So bleibt der Spielstand jedes bestehenden Matches korrekt.
            migrationBuilder.Sql(@"
                INSERT INTO Goals (MatchId, IsOpponentGoal)
                SELECT m.Id, 1
                FROM Matches m
                CROSS APPLY (SELECT TOP (m.OpponentScore) 1 AS x FROM sys.all_objects) AS t
                WHERE m.OpponentScore > 0;");

            // 4. Erst jetzt darf der Zähler weg
            migrationBuilder.DropColumn(
                name: "OpponentScore",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Zähler zurückholen und aus den Gegentor-Datensätzen füllen
            migrationBuilder.AddColumn<int>(
                name: "OpponentScore",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE m
                SET OpponentScore = (
                    SELECT COUNT(*) FROM Goals g
                    WHERE g.MatchId = m.Id AND g.IsOpponentGoal = 1)
                FROM Matches m;");

            // Gegentor-Datensätze entfernen, sonst blieben Zeilen ohne
            // Torschütze übrig und ScorerId liesse sich nicht auf NOT NULL setzen.
            migrationBuilder.Sql("DELETE FROM Goals WHERE IsOpponentGoal = 1;");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsFinished",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsOpponentGoal",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "MinuteInPeriod",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "Goals");

            migrationBuilder.AlterColumn<int>(
                name: "ScorerId",
                table: "Goals",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
