using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanicaApi.Migrations
{
    /// <inheritdoc />
    public partial class ModificarEstructuraPuntosRetiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "PuntoRetiro");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "PuntoRetiro");

            migrationBuilder.AddColumn<string>(
                name: "Barrio",
                table: "PuntoRetiro",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barrio",
                table: "PuntoRetiro");

            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "PuntoRetiro",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "PuntoRetiro",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
