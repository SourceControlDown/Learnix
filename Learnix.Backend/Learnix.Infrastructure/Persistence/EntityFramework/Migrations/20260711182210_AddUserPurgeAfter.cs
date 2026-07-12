using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.EntityFramework.Migrations;

/// <inheritdoc />
public partial class AddUserPurgeAfter : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "PurgeAfter",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_PurgeAfter",
            table: "AspNetUsers",
            column: "PurgeAfter",
            filter: "\"PurgeAfter\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_PurgeAfter",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PurgeAfter",
            table: "AspNetUsers");
    }
}
