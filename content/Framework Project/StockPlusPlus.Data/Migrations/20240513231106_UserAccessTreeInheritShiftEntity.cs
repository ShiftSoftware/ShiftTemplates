using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserAccessTreeInheritShiftEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CompanyBranchID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "bigint",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<long>(
                name: "CompanyID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "bigint",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreateDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)))
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "bigint",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "bit",
                nullable: false,
                defaultValue: false)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "datetimeoffset",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSaveDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)))
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<long>(
                name: "LastSavedByUserID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "bigint",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<long>(
                name: "RegionID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "bigint",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyBranchID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "CompanyID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "CreatedByUserID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "LastSaveDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "LastSavedByUserID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "RegionID",
                schema: "ShiftIdentity",
                table: "UserAccessTrees")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserAccessTreesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ShiftIdentity")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
