using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeHisotryTableIDsAsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[AccessTreesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_AccessTreesHistory_ID] ON [ShiftIdentity].[AccessTreesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[AppsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_AppsHistory_ID] ON [ShiftIdentity].[AppsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[BrandsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_BrandsHistory_ID] ON [ShiftIdentity].[BrandsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CitiesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CitiesHistory_ID] ON [ShiftIdentity].[CitiesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CompaniesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CompaniesHistory_ID] ON [ShiftIdentity].[CompaniesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchBrandsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CompanyBranchBrandsHistory_ID] ON [ShiftIdentity].[CompanyBranchBrandsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchDepartmentsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CompanyBranchDepartmentsHistory_ID] ON [ShiftIdentity].[CompanyBranchDepartmentsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchServicesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CompanyBranchServicesHistory_ID] ON [ShiftIdentity].[CompanyBranchServicesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CompanyBranchesHistory_ID] ON [ShiftIdentity].[CompanyBranchesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyCalendarsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CompanyCalendarsHistory_ID] ON [ShiftIdentity].[CompanyCalendarsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[CountriesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CountriesHistory_ID] ON [ShiftIdentity].[CountriesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[DepartmentsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_DepartmentsHistory_ID] ON [ShiftIdentity].[DepartmentsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[RegionsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_RegionsHistory_ID] ON [ShiftIdentity].[RegionsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[ServicesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_ServicesHistory_ID] ON [ShiftIdentity].[ServicesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[TeamBranchesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_TeamBranchesHistory_ID] ON [ShiftIdentity].[TeamBranchesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[TeamUsersHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_TeamUsersHistory_ID] ON [ShiftIdentity].[TeamUsersHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[TeamsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_TeamsHistory_ID] ON [ShiftIdentity].[TeamsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[UserAccessTreesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_UserAccessTreesHistory_ID] ON [ShiftIdentity].[UserAccessTreesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[UsersHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_UsersHistory_ID] ON [ShiftIdentity].[UsersHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[dbo].[BrandsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_BrandsHistory_ID] ON [dbo].[BrandsHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[dbo].[CountriesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_CountriesHistory_ID] ON [dbo].[CountriesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[dbo].[InvoicesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_InvoicesHistory_ID] ON [dbo].[InvoicesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[dbo].[ProductCategoriesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_ProductCategoriesHistory_ID] ON [dbo].[ProductCategoriesHistory] ([ID]);");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[dbo].[ProductsHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_ProductsHistory_ID] ON [dbo].[ProductsHistory] ([ID]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AccessTreesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[AccessTreesHistory]')) DROP INDEX [IX_AccessTreesHistory_ID] ON [ShiftIdentity].[AccessTreesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AppsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[AppsHistory]')) DROP INDEX [IX_AppsHistory_ID] ON [ShiftIdentity].[AppsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BrandsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[BrandsHistory]')) DROP INDEX [IX_BrandsHistory_ID] ON [ShiftIdentity].[BrandsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CitiesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CitiesHistory]')) DROP INDEX [IX_CitiesHistory_ID] ON [ShiftIdentity].[CitiesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompaniesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CompaniesHistory]')) DROP INDEX [IX_CompaniesHistory_ID] ON [ShiftIdentity].[CompaniesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyBranchBrandsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchBrandsHistory]')) DROP INDEX [IX_CompanyBranchBrandsHistory_ID] ON [ShiftIdentity].[CompanyBranchBrandsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyBranchDepartmentsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchDepartmentsHistory]')) DROP INDEX [IX_CompanyBranchDepartmentsHistory_ID] ON [ShiftIdentity].[CompanyBranchDepartmentsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyBranchServicesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchServicesHistory]')) DROP INDEX [IX_CompanyBranchServicesHistory_ID] ON [ShiftIdentity].[CompanyBranchServicesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyBranchesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyBranchesHistory]')) DROP INDEX [IX_CompanyBranchesHistory_ID] ON [ShiftIdentity].[CompanyBranchesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyCalendarsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CompanyCalendarsHistory]')) DROP INDEX [IX_CompanyCalendarsHistory_ID] ON [ShiftIdentity].[CompanyCalendarsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CountriesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[CountriesHistory]')) DROP INDEX [IX_CountriesHistory_ID] ON [ShiftIdentity].[CountriesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DepartmentsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[DepartmentsHistory]')) DROP INDEX [IX_DepartmentsHistory_ID] ON [ShiftIdentity].[DepartmentsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RegionsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[RegionsHistory]')) DROP INDEX [IX_RegionsHistory_ID] ON [ShiftIdentity].[RegionsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ServicesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[ServicesHistory]')) DROP INDEX [IX_ServicesHistory_ID] ON [ShiftIdentity].[ServicesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TeamBranchesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[TeamBranchesHistory]')) DROP INDEX [IX_TeamBranchesHistory_ID] ON [ShiftIdentity].[TeamBranchesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TeamUsersHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[TeamUsersHistory]')) DROP INDEX [IX_TeamUsersHistory_ID] ON [ShiftIdentity].[TeamUsersHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TeamsHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[TeamsHistory]')) DROP INDEX [IX_TeamsHistory_ID] ON [ShiftIdentity].[TeamsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserAccessTreesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[UserAccessTreesHistory]')) DROP INDEX [IX_UserAccessTreesHistory_ID] ON [ShiftIdentity].[UserAccessTreesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UsersHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[UsersHistory]')) DROP INDEX [IX_UsersHistory_ID] ON [ShiftIdentity].[UsersHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BrandsHistory_ID' AND object_id = OBJECT_ID(N'[dbo].[BrandsHistory]')) DROP INDEX [IX_BrandsHistory_ID] ON [dbo].[BrandsHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CountriesHistory_ID' AND object_id = OBJECT_ID(N'[dbo].[CountriesHistory]')) DROP INDEX [IX_CountriesHistory_ID] ON [dbo].[CountriesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoicesHistory_ID' AND object_id = OBJECT_ID(N'[dbo].[InvoicesHistory]')) DROP INDEX [IX_InvoicesHistory_ID] ON [dbo].[InvoicesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductCategoriesHistory_ID' AND object_id = OBJECT_ID(N'[dbo].[ProductCategoriesHistory]')) DROP INDEX [IX_ProductCategoriesHistory_ID] ON [dbo].[ProductCategoriesHistory];");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductsHistory_ID' AND object_id = OBJECT_ID(N'[dbo].[ProductsHistory]')) DROP INDEX [IX_ProductsHistory_ID] ON [dbo].[ProductsHistory];");
        }
    }
}
