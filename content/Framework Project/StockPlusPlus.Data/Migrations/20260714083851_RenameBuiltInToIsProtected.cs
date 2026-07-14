using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameBuiltInToIsProtected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "IsProtected");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "BuiltIn");
        }
    }
}
