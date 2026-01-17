using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWebsiteURLInCompanyBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebsiteURL",
                schema: "ShiftIdentity",
                table: "CompanyBranches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WebsiteURL",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
