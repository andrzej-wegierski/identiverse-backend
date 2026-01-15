using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueFilteredIndexToIdentityProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentityProfiles_PersonId_Context_IsDefaultForContext",
                table: "IdentityProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProfiles_PersonId_Context",
                table: "IdentityProfiles",
                columns: new[] { "PersonId", "Context" },
                unique: true,
                filter: "\"IsDefaultForContext\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentityProfiles_PersonId_Context",
                table: "IdentityProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProfiles_PersonId_Context_IsDefaultForContext",
                table: "IdentityProfiles",
                columns: new[] { "PersonId", "Context", "IsDefaultForContext" });
        }
    }
}
