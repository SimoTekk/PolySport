using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolySport.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchClock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinuteInPeriod",
                table: "Goals",
                newName: "SecondsInPeriod");

            // Die Spalte führt jetzt Sekunden statt Minuten – bestehende
            // Zeitangaben entsprechend umrechnen.
            migrationBuilder.Sql(
                "UPDATE Goals SET SecondsInPeriod = SecondsInPeriod * 60 WHERE SecondsInPeriod IS NOT NULL;");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPeriod",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStartedAt",
                table: "Matches",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPeriod",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PeriodStartedAt",
                table: "Matches");

            // Sekunden zurück auf Minuten runden (Sekundengenauigkeit geht dabei verloren)
            migrationBuilder.Sql(
                "UPDATE Goals SET SecondsInPeriod = SecondsInPeriod / 60 WHERE SecondsInPeriod IS NOT NULL;");

            migrationBuilder.RenameColumn(
                name: "SecondsInPeriod",
                table: "Goals",
                newName: "MinuteInPeriod");
        }
    }
}
