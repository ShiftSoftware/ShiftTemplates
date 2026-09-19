using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations;

public partial class AddAdministratorConfirmation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_AuthenticationOperation_State", "AuthenticationOperations", "ShiftIdentity");
        migrationBuilder.AddColumn<byte[]>("SourceSessionDigest", "AuthenticationOperations", schema: "ShiftIdentity",
            type: "varbinary(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddCheckConstraint("CK_AuthenticationOperation_State", "AuthenticationOperations",
            "[State] IN (1,2,3,4,5,6,7,8,9,10,11) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12,13)", "ShiftIdentity");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [ShiftIdentity].[AuthenticationOperations] WHERE [Purpose] = 13) THROW 51000, 'Administrator confirmation operations must be retired before downgrading.', 1;");
        migrationBuilder.DropCheckConstraint("CK_AuthenticationOperation_State", "AuthenticationOperations", "ShiftIdentity");
        migrationBuilder.DropColumn("SourceSessionDigest", "AuthenticationOperations", "ShiftIdentity");
        migrationBuilder.AddCheckConstraint("CK_AuthenticationOperation_State", "AuthenticationOperations",
            "[State] IN (1,2,3,4,5,6,7,8,9,10,11) AND [Purpose] IN (1,2,3,4,5,6,7,8,9,10,11,12)", "ShiftIdentity");
    }
}
