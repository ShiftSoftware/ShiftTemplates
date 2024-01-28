using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    public partial class UpdateDeletedRowLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PartitionKeyValue",
                table: "DeletedRowLogs",
                newName: "PartitionKeyLevelTwoValue");

            migrationBuilder.RenameColumn(
                name: "PartitionKeyType",
                table: "DeletedRowLogs",
                newName: "PartitionKeyLevelTwoType");

            migrationBuilder.AddColumn<int>(
                name: "PartitionKeyLevelOneType",
                table: "DeletedRowLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PartitionKeyLevelOneValue",
                table: "DeletedRowLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartitionKeyLevelThreeType",
                table: "DeletedRowLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PartitionKeyLevelThreeValue",
                table: "DeletedRowLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartitionKeyLevelOneType",
                table: "DeletedRowLogs");

            migrationBuilder.DropColumn(
                name: "PartitionKeyLevelOneValue",
                table: "DeletedRowLogs");

            migrationBuilder.DropColumn(
                name: "PartitionKeyLevelThreeType",
                table: "DeletedRowLogs");

            migrationBuilder.DropColumn(
                name: "PartitionKeyLevelThreeValue",
                table: "DeletedRowLogs");

            migrationBuilder.RenameColumn(
                name: "PartitionKeyLevelTwoValue",
                table: "DeletedRowLogs",
                newName: "PartitionKeyValue");

            migrationBuilder.RenameColumn(
                name: "PartitionKeyLevelTwoType",
                table: "DeletedRowLogs",
                newName: "PartitionKeyType");
        }
    }
}
