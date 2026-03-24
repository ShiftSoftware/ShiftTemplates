using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokenChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationId",
                schema: "ShiftIdentity",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyCalendars",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    LastReplicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShiftGroups = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeekendGroups = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCalendars", x => x.ID);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyCalendarsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "CompanyCalendarBranches",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCalendarID = table.Column<long>(type: "bigint", nullable: false),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyCalendarBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyCalendarBranches_CompanyCalendars_CompanyCalendarID",
                        column: x => x.CompanyCalendarID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "CompanyCalendars",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IntegrationId",
                schema: "ShiftIdentity",
                table: "Users",
                column: "IntegrationId",
                unique: true,
                filter: "IsDeleted = 0 AND IntegrationId is not null");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendarBranches_CompanyCalendarID",
                schema: "ShiftIdentity",
                table: "CompanyCalendarBranches",
                column: "CompanyCalendarID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCalendars_StartDate_EndDate_CompanyID_IsDeleted",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                columns: new[] { "StartDate", "EndDate", "CompanyID", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyCalendarBranches",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "CompanyCalendars",
                schema: "ShiftIdentity")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyCalendarsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropIndex(
                name: "IX_Users_IntegrationId",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IntegrationId",
                schema: "ShiftIdentity",
                table: "Users");
        }
    }
}
