using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace water.Migrations
{
    public partial class MeterReadingIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_MeterId",
                table: "MeterReadings");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId",
                table: "MeterReadings",
                column: "MeterId")
                .Annotation("SqlServer:Include", new[] { "Date" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeterReadings_MeterId",
                table: "MeterReadings");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_MeterId",
                table: "MeterReadings",
                column: "MeterId");
        }
    }
}
