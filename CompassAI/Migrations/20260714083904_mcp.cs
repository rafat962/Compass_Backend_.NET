using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompassAI.Migrations
{
    /// <inheritdoc />
    public partial class mcp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArcProMCP",
                table: "ApiKeys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QGISMCP",
                table: "ApiKeys",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArcProMCP",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "QGISMCP",
                table: "ApiKeys");
        }
    }
}
