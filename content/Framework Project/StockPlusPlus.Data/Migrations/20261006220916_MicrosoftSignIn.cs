using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class MicrosoftSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.AddColumn<int>(
                name: "SessionProvider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserProviderLinks",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    TenantID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ObjectID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    EmailLookupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProviderLinks", x => x.ID);
                    table.CheckConstraint("CK_UserProviderLink", "[Provider] = 1 AND DATALENGTH([TenantID]) > 0 AND DATALENGTH([ObjectID]) > 0 AND DATALENGTH([EmailLookupKey]) > 0");
                    table.ForeignKey(
                        name: "FK_UserProviderLinks_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "([SessionProvider] IS NULL OR ([SessionProvider] = 1 AND [Purpose] IN (1,3,10,14))) AND ([State] <> 12 OR ([Purpose] = 14 AND [SessionProvider] IS NOT NULL)) AND ([Purpose] <> 14 OR [State] IN (2,3,6,12))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "[State] IN (1,2,3,4,5,6,7,8,9,10,11,12) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12,13,14)");

            migrationBuilder.CreateIndex(
                name: "IX_UserProviderLinks_Provider_TenantID_ObjectID",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                columns: new[] { "Provider", "TenantID", "ObjectID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProviderLinks_UserID",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProviderLinks",
                schema: "ShiftIdentity");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.DropColumn(
                name: "SessionProvider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "[State] IN (1,2,3,4,5,6,7,8,9,10,11) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12,13)");
        }
    }
}
