using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompassAI.Migrations
{
    /// <inheritdoc />
    public partial class addmodelsinit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocQueryLimit",
                table: "ApiKeys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MapTalkLimit",
                table: "ApiKeys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpecReviewerLimit",
                table: "ApiKeys",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocQueryLimit",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "MapTalkLimit",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "SpecReviewerLimit",
                table: "ApiKeys");
        }
    }
}
