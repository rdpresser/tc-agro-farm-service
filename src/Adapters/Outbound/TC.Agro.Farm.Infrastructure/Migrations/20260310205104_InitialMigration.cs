using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.Farm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "crop_type_catalog",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_system_defined = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    scientific_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    typical_planting_start_month = table.Column<int>(type: "integer", nullable: true),
                    typical_planting_end_month = table.Column<int>(type: "integer", nullable: true),
                    recommended_irrigation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    typical_harvest_cycle_months = table.Column<int>(type: "integer", nullable: true),
                    min_temperature = table.Column<double>(type: "double precision", nullable: true),
                    max_temperature = table.Column<double>(type: "double precision", nullable: true),
                    min_humidity = table.Column<double>(type: "double precision", nullable: true),
                    min_soil_moisture = table.Column<double>(type: "double precision", nullable: true),
                    max_soil_moisture = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crop_type_catalog", x => x.id);
                });

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

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    location_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    location_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    location_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    location_latitude = table.Column<double>(type: "double precision", nullable: true),
                    location_longitude = table.Column<double>(type: "double precision", nullable: true),
                    area_hectares = table.Column<double>(type: "double precision", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_properties_owner_snapshots_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "owner_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crop_type_suggestions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_stale = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    confidence_score = table.Column<double>(type: "double precision", nullable: true),
                    planting_window = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    harvest_cycle_months = table.Column<int>(type: "integer", nullable: true),
                    suggested_irrigation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    min_soil_moisture = table.Column<double>(type: "double precision", nullable: true),
                    max_temperature = table.Column<double>(type: "double precision", nullable: true),
                    min_humidity = table.Column<double>(type: "double precision", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_crop_type_suggestions", x => x.id);
                    table.ForeignKey(
                        name: "fk_crop_type_suggestions_owner_snapshots_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "owner_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_crop_type_suggestions_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "public",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plots",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    area_hectares = table.Column<double>(type: "double precision", nullable: false),
                    crop_type_catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_crop_type_suggestion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    boundary_geo_json = table.Column<string>(type: "text", nullable: true),
                    planting_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expected_harvest_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    irrigation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    additional_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plots", x => x.id);
                    table.ForeignKey(
                        name: "fk_plots_crop_type_catalogs_crop_type_catalog_id",
                        column: x => x.crop_type_catalog_id,
                        principalSchema: "public",
                        principalTable: "crop_type_catalog",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plots_crop_type_suggestions_selected_crop_type_suggestion_id",
                        column: x => x.selected_crop_type_suggestion_id,
                        principalSchema: "public",
                        principalTable: "crop_type_suggestions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_plots_owner_snapshots_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "owner_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_plots_properties_property_id",
                        column: x => x.property_id,
                        principalSchema: "public",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sensors",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_change_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    installed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensors", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensors_owner_snapshots_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "public",
                        principalTable: "owner_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sensors_plots_plot_id",
                        column: x => x.plot_id,
                        principalSchema: "public",
                        principalTable: "plots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crop_type_suggestions_owner_id",
                schema: "public",
                table: "crop_type_suggestions",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_type_suggestions_property_id",
                schema: "public",
                table: "crop_type_suggestions",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_crop_type_suggestions_property_id_owner_id",
                schema: "public",
                table: "crop_type_suggestions",
                columns: new[] { "property_id", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_crop_type_suggestions_property_id_source_is_active_is_stale",
                schema: "public",
                table: "crop_type_suggestions",
                columns: new[] { "property_id", "source", "is_active", "is_stale" });

            migrationBuilder.CreateIndex(
                name: "ix_owner_snapshots_email",
                schema: "public",
                table: "owner_snapshots",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plots_crop_type_catalog_id",
                schema: "public",
                table: "plots",
                column: "crop_type_catalog_id");

            migrationBuilder.CreateIndex(
                name: "ix_plots_owner_id",
                schema: "public",
                table: "plots",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_plots_property_id",
                schema: "public",
                table: "plots",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_plots_selected_crop_type_suggestion_id",
                schema: "public",
                table: "plots",
                column: "selected_crop_type_suggestion_id");

            migrationBuilder.CreateIndex(
                name: "ix_properties_owner_id",
                schema: "public",
                table: "properties",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_owner_id",
                schema: "public",
                table: "sensors",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_plot_id",
                schema: "public",
                table: "sensors",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_status",
                schema: "public",
                table: "sensors",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_type",
                schema: "public",
                table: "sensors",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sensors",
                schema: "public");

            migrationBuilder.DropTable(
                name: "plots",
                schema: "public");

            migrationBuilder.DropTable(
                name: "crop_type_catalog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "crop_type_suggestions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "public");

            migrationBuilder.DropTable(
                name: "owner_snapshots",
                schema: "public");
        }
    }
}
