using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CountryID",
                schema: "ShiftIdentity",
                table: "Users",
                type: "bigint",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UsersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CountryID",
                schema: "ShiftIdentity",
                table: "Users",
                column: "CountryID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Countries_CountryID",
                schema: "ShiftIdentity",
                table: "Users",
                column: "CountryID",
                principalSchema: "ShiftIdentity",
                principalTable: "Countries",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Countries_CountryID",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CountryID",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CountryID",
                schema: "ShiftIdentity",
                table: "Users")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UsersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
