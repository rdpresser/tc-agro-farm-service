using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.Farm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCropCatalogTenantScopeAndCropCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                schema: "public",
                table: "crop_type_catalog",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "crop_cycles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop_type_catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_crop_type_suggestion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expected_harvest_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crop_cycles", x => x.id);
                    table.ForeignKey(
                        name: "fk_crop_cycles_crop_type_catalogs_crop_type_catalog_id",
                        column: x => x.crop_type_catalog_id,
                        principalSchema: "public",
                        principalTable: "crop_type_catalog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_crop_cycles_crop_type_suggestions_selected_crop_type_sugges",
                        column: x => x.selected_crop_type_suggestion_id,
                        principalSchema: "public",
                        principalTable: "crop_type_suggestions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_crop_cycles_owner_snapshots_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "owner_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_crop_cycles_plots_plot_id",
                        column: x => x.plot_id,
                        principalSchema: "public",
                        principalTable: "plots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_crop_cycles_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "public",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crop_cycle_events",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crop_cycle_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_crop_cycle_events_crop_cycles_crop_cycle_id",
                        column: x => x.crop_cycle_id,
                        principalSchema: "public",
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crop_type_catalog_owner_id",
                schema: "public",
                table: "crop_type_catalog",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_events_crop_cycle_id",
                schema: "public",
                table: "crop_cycle_events",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_events_event_type",
                schema: "public",
                table: "crop_cycle_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_events_owner_id",
                schema: "public",
                table: "crop_cycle_events",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_events_plot_id",
                schema: "public",
                table: "crop_cycle_events",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycle_events_property_id",
                schema: "public",
                table: "crop_cycle_events",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_crop_type_catalog_id",
                schema: "public",
                table: "crop_cycles",
                column: "crop_type_catalog_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_owner_id",
                schema: "public",
                table: "crop_cycles",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_plot_id",
                schema: "public",
                table: "crop_cycles",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_property_id",
                schema: "public",
                table: "crop_cycles",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_selected_crop_type_suggestion_id",
                schema: "public",
                table: "crop_cycles",
                column: "selected_crop_type_suggestion_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_cycles_status",
                schema: "public",
                table: "crop_cycles",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "fk_crop_type_catalog_owner_snapshots_owner_id",
                schema: "public",
                table: "crop_type_catalog",
                column: "owner_id",
                principalSchema: "public",
                principalTable: "owner_snapshots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_crop_type_catalog_owner_snapshots_owner_id",
                schema: "public",
                table: "crop_type_catalog");

            migrationBuilder.DropTable(
                name: "crop_cycle_events",
                schema: "public");

            migrationBuilder.DropTable(
                name: "crop_cycles",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "ix_crop_type_catalog_owner_id",
                schema: "public",
                table: "crop_type_catalog");

            migrationBuilder.DropColumn(
                name: "owner_id",
                schema: "public",
                table: "crop_type_catalog");
        }
    }
}
