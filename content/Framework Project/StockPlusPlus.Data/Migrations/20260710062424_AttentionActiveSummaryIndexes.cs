using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AttentionActiveSummaryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_HasActiveAttention",
                table: "Products",
                column: "HasActiveAttention",
                filter: "[HasActiveAttention] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_HasActiveAttention",
                table: "Invoices",
                column: "HasActiveAttention",
                filter: "[HasActiveAttention] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_HasActiveAttention",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_HasActiveAttention",
                table: "Invoices");
        }
    }
}
