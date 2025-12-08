using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayNameAndPublishTargetToCompanyBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishTargets",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE [ShiftIdentity].[CompanyBranches]
                SET DisplayName = Name
                WHERE DisplayName IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                schema: "ShiftIdentity",
                table: "CompanyBranches");

            migrationBuilder.DropColumn(
                name: "PublishTargets",
                schema: "ShiftIdentity",
                table: "CompanyBranches");
        }
    }
}
