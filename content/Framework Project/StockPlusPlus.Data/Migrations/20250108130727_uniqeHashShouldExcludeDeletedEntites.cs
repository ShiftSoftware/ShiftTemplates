using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class uniqeHashShouldExcludeDeletedEntites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_UniqueHash",
                table: "Brands");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_UniqueHash",
                table: "Brands",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL and IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_UniqueHash",
                table: "Brands");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_UniqueHash",
                table: "Brands",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL");
        }
    }
}
