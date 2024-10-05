using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryIDToRegion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CountryID",
                schema: "ShiftIdentity",
                table: "Regions",
                type: "bigint",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RegionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_CountryID",
                schema: "ShiftIdentity",
                table: "Regions",
                column: "CountryID");

            migrationBuilder.AddForeignKey(
                name: "FK_Regions_Countries_CountryID",
                schema: "ShiftIdentity",
                table: "Regions",
                column: "CountryID",
                principalSchema: "ShiftIdentity",
                principalTable: "Countries",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Regions_Countries_CountryID",
                schema: "ShiftIdentity",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Regions_CountryID",
                schema: "ShiftIdentity",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "CountryID",
                schema: "ShiftIdentity",
                table: "Regions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RegionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
