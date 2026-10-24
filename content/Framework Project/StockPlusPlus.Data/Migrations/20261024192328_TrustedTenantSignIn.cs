using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrustedTenantSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.AddColumn<string>(
                name: "SessionTenant",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "([SessionProvider] IS NULL OR ([SessionProvider] IN (1, 2) AND [Purpose] IN (1,3,10,13,14))) AND ([State] <> 12 OR ([Purpose] = 14 AND [SessionProvider] IS NOT NULL) OR [Purpose] = 13) AND ([Purpose] <> 14 OR [State] IN (2,3,6,12)) AND ([SessionTenant] IS NULL OR [SessionProvider] = 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.DropColumn(
                name: "SessionTenant",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "([SessionProvider] IS NULL OR ([SessionProvider] IN (1, 2) AND [Purpose] IN (1,3,10,14))) AND ([State] <> 12 OR ([Purpose] = 14 AND [SessionProvider] IS NOT NULL)) AND ([Purpose] <> 14 OR [State] IN (2,3,6,12))");
        }
    }
}
