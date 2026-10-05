using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class markAdditionalIdentityEntitiesForReplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "UserAccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "AccessTrees");
        }
    }
}
