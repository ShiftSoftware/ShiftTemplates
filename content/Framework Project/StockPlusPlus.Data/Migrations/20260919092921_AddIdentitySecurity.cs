using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentitySecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthenticationAuditEvents",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    OperationID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ActorUserID = table.Column<long>(type: "bigint", nullable: true),
                    VerificationReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationAuditEvents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationOperations",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    ContactRevision = table.Column<long>(type: "bigint", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    OutstandingLinkSlot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FactorGeneration = table.Column<long>(type: "bigint", nullable: false),
                    PolicyRevision = table.Column<long>(type: "bigint", nullable: false),
                    ClientID = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    External = table.Column<bool>(type: "bit", nullable: false),
                    HandleDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AppBinding = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SessionAuthenticatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SessionMfaSatisfied = table.Column<bool>(type: "bit", nullable: true),
                    SessionLegacyCompatibilityExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PasswordChangeOrigin = table.Column<int>(type: "int", nullable: true),
                    PasswordProvenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MfaProvenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ParentID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProtectedPendingTotpSecret = table.Column<byte[]>(type: "varbinary(1024)", maxLength: 1024, nullable: true),
                    RecoveryCodeDigest = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    OutstandingRecoveryUserID = table.Column<long>(type: "bigint", nullable: true),
                    PendingPasswordHash = table.Column<byte[]>(type: "varbinary(256)", maxLength: 256, nullable: true),
                    PendingPasswordSalt = table.Column<byte[]>(type: "varbinary(128)", maxLength: 128, nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationOperations", x => x.ID);
                    table.CheckConstraint("CK_AuthenticationOperation_App", "([State] <> 11 OR ([Purpose] = 10 AND [External] = 1 AND [AppBinding] IS NOT NULL AND [SessionAuthenticatedAt] IS NOT NULL AND [SessionMfaSatisfied] IS NOT NULL)) AND ([Purpose] <> 10 OR [State] IN (2,3,6,9,11))");
                    table.CheckConstraint("CK_AuthenticationOperation_Factor", "[ProtectedPendingTotpSecret] IS NULL OR ([Purpose] IN (3,4,5,6) AND [State] = 7)");
                    table.CheckConstraint("CK_AuthenticationOperation_LegacyMfa", "[Purpose] <> 12 OR ([State] IN (1,2,3,6) AND [External] = 0 AND DATALENGTH([HandleDigest]) = 32 AND [CodeChallenge] = '' AND ([State] = 1 OR [CompletedAt] IS NOT NULL))");
                    table.CheckConstraint("CK_AuthenticationOperation_LegacyRefresh", "[Purpose] <> 11 OR ([State] = 2 AND [External] = 0 AND DATALENGTH([HandleDigest]) = 32 AND [CompletedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_AuthenticationOperation_Link", "([State] <> 10 OR [Purpose] IN (7,8,9)) AND ([OutstandingLinkSlot] IS NULL OR ([Purpose] IN (7,8,9) AND [State] = 10))");
                    table.CheckConstraint("CK_AuthenticationOperation_Password", "([PasswordChangeOrigin] IS NULL OR [PasswordChangeOrigin] IN (1,2)) AND (([PendingPasswordHash] IS NULL AND [PendingPasswordSalt] IS NULL) OR ([Purpose] = 4 AND [State] IN (1,7) AND [PendingPasswordHash] IS NOT NULL AND [PendingPasswordSalt] IS NOT NULL))");
                    table.CheckConstraint("CK_AuthenticationOperation_Recovery", "([RecoveryCodeDigest] IS NULL OR ([Purpose] = 6 AND [State] = 8 AND [ParentID] IS NULL)) AND ([OutstandingRecoveryUserID] IS NULL OR ([Purpose] = 6 AND [ParentID] IS NULL AND [OutstandingRecoveryUserID] = [UserID]))");
                    table.CheckConstraint("CK_AuthenticationOperation_SessionCompatibility", "[SessionLegacyCompatibilityExpiresAt] IS NULL OR ([Purpose] = 10 AND [State] = 11)");
                    table.CheckConstraint("CK_AuthenticationOperation_State", "[State] IN (1,2,3,4,5,6,7,8,9,10,11) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12)");
                    table.CheckConstraint("CK_AuthenticationOperation_Version", "[SecurityVersion] >= 1 AND [FactorGeneration] >= 1 AND [PolicyRevision] >= 1 AND [FailedAttempts] BETWEEN 0 AND 5 AND [ExpiresAt] > [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_AuthenticationOperations_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationPolicyStates",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    MfaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MfaMandatory = table.Column<bool>(type: "bit", nullable: false),
                    RequireVerifiedEmail = table.Column<bool>(type: "bit", nullable: false),
                    TotpDigits = table.Column<int>(type: "int", nullable: false),
                    TotpPeriodSeconds = table.Column<int>(type: "int", nullable: false),
                    TotpWindowPast = table.Column<int>(type: "int", nullable: false),
                    TotpWindowFuture = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationPolicyStates", x => x.ID);
                    table.CheckConstraint("CK_AuthenticationPolicyState", "[ID] = 1 AND [Revision] >= 1");
                    table.CheckConstraint("CK_AuthenticationPolicyState_Totp", "[TotpDigits] BETWEEN 6 AND 8 AND [TotpPeriodSeconds] BETWEEN 1 AND 300 AND [TotpWindowPast] BETWEEN 0 AND 2 AND [TotpWindowFuture] BETWEEN 0 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "AuthThrottleBuckets",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthThrottleBuckets", x => x.Key);
                    table.CheckConstraint("CK_AuthThrottleBucket", "[Count] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "UserSecurityStates",
                schema: "ShiftIdentity",
                columns: table => new
                {
                    UserID = table.Column<long>(type: "bigint", nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    ContactRevision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    UsernameLookupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, collation: "Latin1_General_100_BIN2"),
                    EmailLookupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, collation: "Latin1_General_100_BIN2"),
                    RecoveryEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RecoveryEmailRevision = table.Column<long>(type: "bigint", nullable: true),
                    RecoveryEmailProvenance = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DeliveryWindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastDeliveryAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeliveryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FactorGeneration = table.Column<long>(type: "bigint", nullable: false),
                    LocalMfaRecoveryRequired = table.Column<bool>(type: "bit", nullable: false),
                    ProtectedTotpSecret = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    TotpProtectionVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MfaRecoveryOperationID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastAcceptedTotpStep = table.Column<long>(type: "bigint", nullable: true),
                    FailedProofs = table.Column<int>(type: "int", nullable: false),
                    FailureWindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityStates", x => x.UserID);
                    table.CheckConstraint("CK_UserSecurityState_ContactDelivery", "[ContactRevision] >= 1 AND [DeliveryCount] >= 0 AND [RecoveryEmailProvenance] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_UserSecurityState_Lookup", "([UsernameLookupKey] IS NULL AND [EmailLookupKey] IS NULL) OR ([UsernameLookupKey] IS NOT NULL AND DATALENGTH([UsernameLookupKey]) > 0 AND ([EmailLookupKey] IS NULL OR DATALENGTH([EmailLookupKey]) > 0))");
                    table.CheckConstraint("CK_UserSecurityState_Version", "[SecurityVersion] >= 1 AND [FactorGeneration] >= 1 AND [FailedProofs] >= 0 AND [TotpProtectionVersion] IN (0,1)");
                    table.ForeignKey(
                        name: "FK_UserSecurityStates_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "ShiftIdentity",
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAuditEvents_CreatedAt",
                schema: "ShiftIdentity",
                table: "AuthenticationAuditEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_ExpiresAt_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                columns: new[] { "ExpiresAt", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_HandleDigest",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "HandleDigest",
                unique: true,
                filter: "[Purpose] IN (11,12)");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_OutstandingLinkSlot",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "OutstandingLinkSlot",
                unique: true,
                filter: "[OutstandingLinkSlot] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_OutstandingRecoveryUserID",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "OutstandingRecoveryUserID",
                unique: true,
                filter: "[OutstandingRecoveryUserID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_ParentID",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationOperations_UserID_Purpose_State",
                schema: "ShiftIdentity",
                table: "AuthenticationOperations",
                columns: new[] { "UserID", "Purpose", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthThrottleBuckets_WindowStart",
                schema: "ShiftIdentity",
                table: "AuthThrottleBuckets",
                column: "WindowStart");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityStates_EmailLookupKey",
                schema: "ShiftIdentity",
                table: "UserSecurityStates",
                column: "EmailLookupKey",
                unique: true,
                filter: "[EmailLookupKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityStates_UsernameLookupKey",
                schema: "ShiftIdentity",
                table: "UserSecurityStates",
                column: "UsernameLookupKey",
                unique: true,
                filter: "[UsernameLookupKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthenticationAuditEvents",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AuthenticationOperations",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AuthenticationPolicyStates",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "AuthThrottleBuckets",
                schema: "ShiftIdentity");

            migrationBuilder.DropTable(
                name: "UserSecurityStates",
                schema: "ShiftIdentity");
        }
    }
}
