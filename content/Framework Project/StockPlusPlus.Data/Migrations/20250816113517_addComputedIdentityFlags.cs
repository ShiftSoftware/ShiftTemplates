using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class addComputedIdentityFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Cities] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Companies] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[CompanyBranches] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Countries] SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Regions] SET (SYSTEM_VERSIONING = OFF);");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShiftIdentity].[Cities]
                ADD CityID AS [ID] PERSISTED;

                ALTER TABLE [ShiftIdentity].[CitiesHistory]
                ADD CityID bigint not null
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShiftIdentity].[Companies]
                ADD CompanyID AS [ID] PERSISTED;

                ALTER TABLE [ShiftIdentity].[CompaniesHistory]
                ADD CompanyID bigint not null
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShiftIdentity].[CompanyBranches]
                ADD CompanyBranchID AS [ID] PERSISTED;

                ALTER TABLE [ShiftIdentity].[CompanyBranchesHistory]
                ADD CompanyBranchID bigint not null
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShiftIdentity].[Countries]
                ADD CountryID AS [ID] PERSISTED;

                ALTER TABLE [ShiftIdentity].[CountriesHistory]
                ADD CountryID bigint not null
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [ShiftIdentity].[Regions]
                ADD RegionID AS [ID] PERSISTED;

                ALTER TABLE [ShiftIdentity].[RegionsHistory]
                ADD RegionID bigint not null
            ");

            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Cities] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ShiftIdentity].[CitiesHistory]));");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Companies] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ShiftIdentity].[CompaniesHistory]));");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[CompanyBranches] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ShiftIdentity].[CompanyBranchesHistory]));");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Countries] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ShiftIdentity].[CountriesHistory]));");
            migrationBuilder.Sql("ALTER TABLE [ShiftIdentity].[Regions] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ShiftIdentity].[RegionsHistory]));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
