using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoHost.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTypeAndProtocolToApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Protocol",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceType",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "ApiKeys");
        }
    }
}
