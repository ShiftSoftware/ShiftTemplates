using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class addIdempotencyKeyToCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "Countries",
                type: "uniqueidentifier",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CountriesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CompanyID",
                schema: "ShiftIdentity",
                table: "Teams",
                column: "CompanyID");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Companies_CompanyID",
                schema: "ShiftIdentity",
                table: "Teams",
                column: "CompanyID",
                principalSchema: "ShiftIdentity",
                principalTable: "Companies",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Companies_CompanyID",
                schema: "ShiftIdentity",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CompanyID",
                schema: "ShiftIdentity",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Countries")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CountriesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
