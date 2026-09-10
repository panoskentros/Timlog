using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDetailsToCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "TenantCredentials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "TenantCredentials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "TenantCredentials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "TenantCredentials",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "TenantCredentials",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "TenantCredentials");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "TenantCredentials");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "TenantCredentials");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "TenantCredentials");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "TenantCredentials");
        }
    }
}
