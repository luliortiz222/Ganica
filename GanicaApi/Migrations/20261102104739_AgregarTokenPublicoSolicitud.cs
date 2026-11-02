using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanicaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTokenPublicoSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenPublico",
                table: "Solicitudes",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenPublico",
                table: "Solicitudes");
        }
    }
}
