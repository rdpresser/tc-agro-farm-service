using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.Farm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestedImageToCropTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "suggested_image",
                schema: "public",
                table: "crop_type_suggestions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suggested_image",
                schema: "public",
                table: "crop_type_catalog",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "suggested_image",
                schema: "public",
                table: "crop_type_suggestions");

            migrationBuilder.DropColumn(
                name: "suggested_image",
                schema: "public",
                table: "crop_type_catalog");
        }
    }
}
