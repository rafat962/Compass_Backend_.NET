using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompassAI.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Active", "CreatedAt", "CurrentPlan", "Email", "EmailActive", "LoginLogs", "LogoutLogs", "Name", "OTP", "PasswordHash", "Photo", "ResetPasswordExpires", "ResetPasswordToken", "Role", "UpdatedAt" },
                values: new object[] { new Guid("a0c8d8d2-9e4f-4f7c-89e6-06ea39d0d3df"), true, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Free", "rafatkamel96@gmail.com", true, "[]", "[]", "Rafat Kamel", null, "$2b$12$/EwnxUm7UQu84.BxsTLhDuuaPbRzYD.ZRwy3J88bNZ0hMTObkLUN6", "none", null, null, "admin", new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("a0c8d8d2-9e4f-4f7c-89e6-06ea39d0d3df"));
        }
    }
}
