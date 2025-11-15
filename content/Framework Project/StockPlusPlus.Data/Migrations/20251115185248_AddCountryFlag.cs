using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_Products_ProductID",
                table: "InvoiceLines");

            migrationBuilder.AlterColumn<long>(
                name: "ProductID",
                table: "InvoiceLines",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Flag",
                schema: "ShiftIdentity",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_Products_ProductID",
                table: "InvoiceLines",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_Products_ProductID",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "Flag",
                schema: "ShiftIdentity",
                table: "Countries");

            migrationBuilder.AlterColumn<long>(
                name: "ProductID",
                table: "InvoiceLines",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_Products_ProductID",
                table: "InvoiceLines",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ID");
        }
    }
}
