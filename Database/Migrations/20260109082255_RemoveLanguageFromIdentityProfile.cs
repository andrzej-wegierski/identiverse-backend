using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLanguageFromIdentityProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentityProfiles_PersonId_Context_Language_IsDefaultForCont~",
                table: "IdentityProfiles");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "IdentityProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProfiles_PersonId_Context_IsDefaultForContext",
                table: "IdentityProfiles",
                columns: new[] { "PersonId", "Context", "IsDefaultForContext" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentityProfiles_PersonId_Context_IsDefaultForContext",
                table: "IdentityProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "IdentityProfiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProfiles_PersonId_Context_Language_IsDefaultForCont~",
                table: "IdentityProfiles",
                columns: new[] { "PersonId", "Context", "Language", "IsDefaultForContext" });
        }
    }
}
