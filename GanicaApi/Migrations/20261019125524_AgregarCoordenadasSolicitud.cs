using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanicaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCoordenadasSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "Solicitudes",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "Solicitudes",
                type: "double",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "Solicitudes");
        }
    }
}
