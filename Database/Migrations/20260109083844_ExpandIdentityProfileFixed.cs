using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class ExpandIdentityProfileFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "IdentityProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "IdentityProfiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "IdentityProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "IdentityProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "IdentityProfiles");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "IdentityProfiles");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "IdentityProfiles");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "IdentityProfiles");
        }
    }
}
