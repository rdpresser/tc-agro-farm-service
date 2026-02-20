using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TC.Agro.Farm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyIdToSensors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add column as nullable
            migrationBuilder.AddColumn<Guid>(
                name: "property_id",
                schema: "public",
                table: "sensors",
                type: "uuid",
                nullable: true);

            // Step 2: Populate property_id from plots table
            migrationBuilder.Sql(@"
                UPDATE public.sensors s
                SET property_id = p.property_id
                FROM public.plots p
                WHERE s.plot_id = p.id;
            ");

            // Step 3: Alter column to NOT NULL (all rows should have values now)
            migrationBuilder.AlterColumn<Guid>(
                name: "property_id",
                schema: "public",
                table: "sensors",
                type: "uuid",
                nullable: false);

            // Step 4: Create index
            migrationBuilder.CreateIndex(
                name: "ix_sensors_property_id",
                schema: "public",
                table: "sensors",
                column: "property_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sensors_property_id",
                schema: "public",
                table: "sensors");

            migrationBuilder.DropColumn(
                name: "property_id",
                schema: "public",
                table: "sensors");
        }
    }
}
