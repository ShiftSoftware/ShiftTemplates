using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameLastReplicationPartitionKeyToLastReplicationStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                table: "TeamBranches");

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
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyCalendars");

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

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Teams",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Services",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Departments",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "LastReplicationStamp");

            migrationBuilder.RenameColumn(
                name: "LastReplicationPartitionKey",
                schema: "ShiftIdentity",
                table: "Brands",
                newName: "LastReplicationStamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Teams",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Services",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Departments",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "LastReplicationPartitionKey");

            migrationBuilder.RenameColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Brands",
                newName: "LastReplicationPartitionKey");

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
                table: "TeamBranches",
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
    }
}
