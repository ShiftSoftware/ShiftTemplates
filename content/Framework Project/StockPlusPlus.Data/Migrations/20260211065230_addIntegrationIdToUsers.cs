using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class addIntegrationIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationId",
                schema: "ShiftIdentity",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IntegrationId",
                schema: "ShiftIdentity",
                table: "Users",
                column: "IntegrationId",
                unique: true,
                filter: "IsDeleted = 0 AND IntegrationId is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IntegrationId",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IntegrationId",
                schema: "ShiftIdentity",
                table: "Users");
        }
    }
}
