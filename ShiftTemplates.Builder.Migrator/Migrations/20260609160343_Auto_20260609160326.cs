using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftTemplates.Builder.Migrator.Migrations
{
    /// <inheritdoc />
    public partial class Auto_20260609160326 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Apps",
                schema: "ShiftIdentity")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AppsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityStamp",
                schema: "ShiftIdentity",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ActiveSignalCount",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AttentionSignalsJson",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasActiveAttention",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HighestSeverity",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActiveSignalCount",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueDate",
                table: "Invoices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasActiveAttention",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HighestSeverity",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AttentionSignals",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClearedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClearedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttentionSignals", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttentionSignals_EntityType_EntityId_ClearedAt",
                table: "AttentionSignals",
                columns: new[] { "EntityType", "EntityId", "ClearedAt" });

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'[ShiftIdentity].[AccessTreesHistory]') AND ic.key_ordinal = 1 AND c.name = N'ID') CREATE NONCLUSTERED INDEX [IX_AccessTreesHistory_ID] ON [ShiftIdentity].[AccessTreesHistory] ([ID]);");

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
            migrationBuilder.DropTable(
                name: "AttentionSignals");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActiveSignalCount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AttentionSignalsJson",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasActiveAttention",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HighestSeverity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ActiveSignalCount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "HasActiveAttention",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "HighestSeverity",
                table: "Invoices");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Apps",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AppSecret = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    RedirectUri = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.ID);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AppsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_Apps_AppId",
                schema: "ShiftIdentity",
                table: "Apps",
                column: "AppId",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AccessTreesHistory_ID' AND object_id = OBJECT_ID(N'[ShiftIdentity].[AccessTreesHistory]')) DROP INDEX [IX_AccessTreesHistory_ID] ON [ShiftIdentity].[AccessTreesHistory];");

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
