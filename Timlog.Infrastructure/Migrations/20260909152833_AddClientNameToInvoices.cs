using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientNameToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "Invoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "Invoices");
        }
    }
}
