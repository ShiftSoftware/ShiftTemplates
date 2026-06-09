using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class addLastReplicatedPartitionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "UserLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "TeamBranches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Regions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                table: "ProductCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                table: "InvoiceLines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Apps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "UserLogs");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "UserAccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "TeamBranches");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyCalendars");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranches");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "AccessTrees");
        }
    }
}
