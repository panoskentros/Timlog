using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    IssuerAfm = table.Column<string>(type: "text", nullable: false),
                    EncryptedAadeUserId = table.Column<string>(type: "text", nullable: false),
                    EncryptedSubscriptionKey = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantCredentials_IssuerAfm",
                table: "TenantCredentials",
                column: "IssuerAfm");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCredentials_TelegramChatId",
                table: "TenantCredentials",
                column: "TelegramChatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantCredentials");
        }
    }
}
