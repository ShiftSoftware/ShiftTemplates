using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Regions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Countries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Cities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Regions_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Regions",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Countries",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBranches_DisplayOrder",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Companies",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Cities",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Regions_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Countries_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_CompanyBranches_DisplayOrder",
                schema: "ShiftIdentity",
                table: "CompanyBranches");

            migrationBuilder.DropIndex(
                name: "IX_Companies_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Cities_DisplayOrder",
                schema: "ShiftIdentity",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "CompanyBranches");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "ShiftIdentity",
                table: "Cities");
        }
    }
}
