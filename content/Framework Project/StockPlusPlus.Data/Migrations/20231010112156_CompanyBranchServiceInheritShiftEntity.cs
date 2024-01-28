using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    public partial class CompanyBranchServiceInheritShiftEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CompanyID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSaveDate",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "LastSavedByUserID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RegionID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "CreatedByUserID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "LastSaveDate",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "LastSavedByUserID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "RegionID",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CompanyBranchServicesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity");
        }
    }
}
