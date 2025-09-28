using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class addComputedIdentityFlagsForBrandAndTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Brands] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Teams] SET (SYSTEM_VERSIONING = OFF);");
            
            migrationBuilder.Sql(@"
                ALTER TABLE [ShiftIdentity].[Brands]
                ADD BrandID AS [ID] PERSISTED;

                ALTER TABLE [ShiftIdentity].[BrandsHistory]
                ADD BrandID bigint not null
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShiftIdentity].[Teams]
                ADD TeamID AS [ID] PERSISTED;

                ALTER TABLE [ShiftIdentity].[TeamsHistory]
                ADD TeamID bigint not null
            ");

            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Brands] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ShiftIdentity].[BrandsHistory]));");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Teams] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ShiftIdentity].[TeamsHistory]));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
