using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLastReplicationDateFromTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedRowLogs");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserLogs");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamBranches");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees");

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyCalendars");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamBranches",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "Tags",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "ProductCategories",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "Invoices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "InvoiceLines",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "Countries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "Brands",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "Apps",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeletedRowLogs",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContainerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastReplicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PartitionKeyLevelOneType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelOneValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartitionKeyLevelThreeType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelThreeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartitionKeyLevelTwoType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelTwoValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedRowLogs", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletedRowLogs_ContainerName_LastReplicationDate",
                table: "DeletedRowLogs",
                columns: new[] { "ContainerName", "LastReplicationDate" });
        }
    }
}
