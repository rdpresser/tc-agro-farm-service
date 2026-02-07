#nullable disable

namespace TC.Agro.Farm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "farm");

            migrationBuilder.CreateTable(
                name: "plots",
                schema: "farm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    crop_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    area_hectares = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "farm",
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
                });

            migrationBuilder.CreateTable(
                name: "sensors",
                schema: "farm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    installed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensors", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plots_property_id",
                schema: "farm",
                table: "plots",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_properties_owner_id",
                schema: "farm",
                table: "properties",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_plot_id",
                schema: "farm",
                table: "sensors",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_status",
                schema: "farm",
                table: "sensors",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sensors_type",
                schema: "farm",
                table: "sensors",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plots",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "sensors",
                schema: "farm");
        }
    }
}
