using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockPlusPlus.Data.Migrations
{
    public partial class AddSamplePiiContact : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SampleContacts",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Identifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SampleContacts", x => x.ID));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SampleContacts");
        }
    }
}
