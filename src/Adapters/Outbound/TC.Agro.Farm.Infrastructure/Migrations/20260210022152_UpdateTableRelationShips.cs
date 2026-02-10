using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.Farm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableRelationShips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "owner_snapshots",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_owner_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_owner_snapshots_email",
                schema: "public",
                table: "owner_snapshots",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_plots_properties_property_id",
                schema: "public",
                table: "plots",
                column: "property_id",
                principalSchema: "public",
                principalTable: "properties",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_properties_owner_snapshots_owner_id",
                schema: "public",
                table: "properties",
                column: "owner_id",
                principalSchema: "public",
                principalTable: "owner_snapshots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sensors_plots_plot_id",
                schema: "public",
                table: "sensors",
                column: "plot_id",
                principalSchema: "public",
                principalTable: "plots",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_plots_properties_property_id",
                schema: "public",
                table: "plots");

            migrationBuilder.DropForeignKey(
                name: "fk_properties_owner_snapshots_owner_id",
                schema: "public",
                table: "properties");

            migrationBuilder.DropForeignKey(
                name: "fk_sensors_plots_plot_id",
                schema: "public",
                table: "sensors");

            migrationBuilder.DropTable(
                name: "owner_snapshots",
                schema: "public");
        }
    }
}
