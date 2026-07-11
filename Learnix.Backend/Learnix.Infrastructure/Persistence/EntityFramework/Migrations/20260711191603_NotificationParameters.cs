using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.EntityFramework.Migrations;

/// <inheritdoc />
public partial class NotificationParameters : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Body",
            table: "Notifications");

        migrationBuilder.DropColumn(
            name: "Title",
            table: "Notifications");

        migrationBuilder.AddColumn<string>(
            name: "Parameters",
            table: "Notifications",
            type: "jsonb",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Parameters",
            table: "Notifications");

        migrationBuilder.AddColumn<string>(
            name: "Body",
            table: "Notifications",
            type: "character varying(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Title",
            table: "Notifications",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");
    }
}
