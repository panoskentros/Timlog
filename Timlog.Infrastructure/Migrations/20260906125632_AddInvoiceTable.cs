using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientAfm = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    WithholdingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPayable = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AadeMark = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AadeMark",
                table: "Invoices",
                column: "AadeMark",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ClientAfm",
                table: "Invoices",
                column: "ClientAfm");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IssueDate",
                table: "Invoices",
                column: "IssueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
