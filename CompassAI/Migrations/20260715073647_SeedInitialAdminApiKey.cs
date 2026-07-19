using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompassAI.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialAdminApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ApiKeys",
                columns: new[] { "Id", "ArcProMCP", "CreatedAt", "DocQueryLimit", "ExpiresAt", "IsActive", "Key", "MapTalkLimit", "PackageType", "QGISMCP", "RequestsLimit", "RequestsUsed", "SpecReviewerLimit", "UserId" },
                values: new object[] { new Guid("6e26344e-971b-420c-8fe7-0fbc9d0fe520"), 50000, new DateTime(2026, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc), 50000, null, true, "cmp_eb4fbf10989d40e5a9b3c16d7e2f503a", 50000, "Premium", 50000, 50000, 0, 50000, new Guid("a0c8d8d2-9e4f-4f7c-89e6-06ea39d0d3df") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: new Guid("6e26344e-971b-420c-8fe7-0fbc9d0fe520"));
        }
    }
}
