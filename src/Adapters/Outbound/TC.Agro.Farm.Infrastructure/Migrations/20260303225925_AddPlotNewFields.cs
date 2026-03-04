using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.Farm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlotNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "boundary_geo_json",
                schema: "public",
                table: "plots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                schema: "public",
                table: "plots",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                schema: "public",
                table: "plots",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "boundary_geo_json",
                schema: "public",
                table: "plots");

            migrationBuilder.DropColumn(
                name: "latitude",
                schema: "public",
                table: "plots");

            migrationBuilder.DropColumn(
                name: "longitude",
                schema: "public",
                table: "plots");
        }
    }
}
