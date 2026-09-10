using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuerAfmToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IssuerAfm",
                table: "Invoices",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IssuerAfm",
                table: "Invoices",
                column: "IssuerAfm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_IssuerAfm",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuerAfm",
                table: "Invoices");
        }
    }
}
