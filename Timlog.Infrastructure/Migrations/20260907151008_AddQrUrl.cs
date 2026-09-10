using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQrUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrUrl",
                table: "Invoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrUrl",
                table: "Invoices");
        }
    }
}
