using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeviceSignIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDeviceSignIn",
                schema: "ShiftIdentity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DeviceAuthorizations",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    UserCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    ClientID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: true),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: true),
                    PolicyRevision = table.Column<long>(type: "bigint", nullable: true),
                    FactorGeneration = table.Column<long>(type: "bigint", nullable: true),
                    MfaSatisfied = table.Column<bool>(type: "bit", nullable: true),
                    AuthenticatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SessionProvider = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPolledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAuthorizations", x => x.ID);
                    table.CheckConstraint("CK_DeviceAuthorization_Account", "([State] = 1 AND [UserID] IS NULL AND [ApprovedAt] IS NULL) OR ([State] IN (2,4) AND [UserID] IS NOT NULL AND [SecurityVersion] >= 1 AND [PolicyRevision] >= 1 AND [FactorGeneration] >= 1 AND [MfaSatisfied] IS NOT NULL AND [AuthenticatedAt] IS NOT NULL AND [ApprovedAt] IS NOT NULL) OR [State] IN (3,5)");
                    table.CheckConstraint("CK_DeviceAuthorization_Codes", "([State] IN (1,2) AND DATALENGTH([DeviceCodeDigest]) = 32 AND DATALENGTH([UserCodeDigest]) = 32) OR ([State] IN (4,5) AND [DeviceCodeDigest] IS NULL AND [UserCodeDigest] IS NULL) OR ([State] = 3 AND ((DATALENGTH([DeviceCodeDigest]) = 32 AND DATALENGTH([UserCodeDigest]) = 32) OR ([DeviceCodeDigest] IS NULL AND [UserCodeDigest] IS NULL)))");
                    table.CheckConstraint("CK_DeviceAuthorization_State", "[State] IN (1,2,3,4,5) AND [ExpiresAt] > [CreatedAt] AND [Interval] BETWEEN 1 AND 3600 AND ([SessionProvider] IS NULL OR [SessionProvider] IN (1, 2))");
                    table.ForeignKey(
                        name: "FK_DeviceAuthorizations_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthorizations_ExpiresAt_State",
                schema: "ShiftIdentity",
                table: "DeviceAuthorizations",
                columns: new[] { "ExpiresAt", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthorizations_UserCodeDigest",
                schema: "ShiftIdentity",
                table: "DeviceAuthorizations",
                column: "UserCodeDigest",
                unique: true,
                filter: "[UserCodeDigest] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthorizations_UserID",
                schema: "ShiftIdentity",
                table: "DeviceAuthorizations",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceAuthorizations",
                schema: "ShiftIdentity");

            migrationBuilder.DropColumn(
                name: "AllowDeviceSignIn",
                schema: "ShiftIdentity",
                table: "Users");
        }
    }
}
