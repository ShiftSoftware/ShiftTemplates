using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoice2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManualReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InvoiceNo = table.Column<long>(type: "bigint", nullable: false),
                    ReleaseDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RegionID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyID = table.Column<long>(type: "bigint", nullable: true),
                    CompanyBranchID = table.Column<long>(type: "bigint", nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CityID = table.Column<long>(type: "bigint", nullable: true),
                    CountryID = table.Column<long>(type: "bigint", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    UniqueHash = table.Column<byte[]>(type: "BINARY(32)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.ID);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InvoicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductID = table.Column<long>(type: "bigint", nullable: true),
                    InvoiceID = table.Column<long>(type: "bigint", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.ID);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceID",
                        column: x => x.InvoiceID,
                        principalTable: "Invoices",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_IdempotencyKey",
                table: "InvoiceLines",
                column: "IdempotencyKey",
                unique: true,
                filter: "IdempotencyKey IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceID",
                table: "InvoiceLines",
                column: "InvoiceID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ProductID",
                table: "InvoiceLines",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IdempotencyKey",
                table: "Invoices",
                column: "IdempotencyKey",
                unique: true,
                filter: "IdempotencyKey IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UniqueHash",
                table: "Invoices",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL and IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "Invoices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "InvoicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
