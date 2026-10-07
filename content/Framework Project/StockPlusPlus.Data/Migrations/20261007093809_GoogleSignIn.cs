using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class GoogleSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProviderLink",
                schema: "ShiftIdentity",
                table: "UserProviderLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.AlterColumn<string>(
                name: "ObjectID",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AddColumn<bool>(
                name: "PersonalAccount",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Links made before this release are Microsoft's: the personal-account tenant marks the personal ones.
            // EXEC defers compiling the statement, so a single-batch script can name the column it just added.
            migrationBuilder.Sql("EXEC(N'UPDATE [ShiftIdentity].[UserProviderLinks] SET [PersonalAccount] = 1 WHERE [Provider] = 1 AND [TenantID] = ''9188040d-6c67-4c5b-b112-36a304b66dad''')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProviderLink",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                sql: "[Provider] IN (1, 2) AND DATALENGTH([TenantID]) > 0 AND DATALENGTH([ObjectID]) > 0 AND DATALENGTH([EmailLookupKey]) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "([SessionProvider] IS NULL OR ([SessionProvider] IN (1, 2) AND [Purpose] IN (1,3,10,14))) AND ([State] <> 12 OR ([Purpose] = 14 AND [SessionProvider] IS NOT NULL)) AND ([Purpose] <> 14 OR [State] IN (2,3,6,12))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserProviderLink",
                schema: "ShiftIdentity",
                table: "UserProviderLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations");

            migrationBuilder.DropColumn(
                name: "PersonalAccount",
                schema: "ShiftIdentity",
                table: "UserProviderLinks");

            migrationBuilder.AlterColumn<string>(
                name: "ObjectID",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserProviderLink",
                schema: "ShiftIdentity",
                table: "UserProviderLinks",
                sql: "[Provider] = 1 AND DATALENGTH([TenantID]) > 0 AND DATALENGTH([ObjectID]) > 0 AND DATALENGTH([EmailLookupKey]) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuthenticationOperation_Provider",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                sql: "([SessionProvider] IS NULL OR ([SessionProvider] = 1 AND [Purpose] IN (1,3,10,14))) AND ([State] <> 12 OR ([Purpose] = 14 AND [SessionProvider] IS NOT NULL)) AND ([Purpose] <> 14 OR [State] IN (2,3,6,12))");
        }
    }
}
