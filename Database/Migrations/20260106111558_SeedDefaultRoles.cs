using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { 1, "Admin", "ADMIN", Guid.NewGuid().ToString() },
                    { 2, "User", "USER", Guid.NewGuid().ToString() }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: 1);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: 2);
        }
    }
}
