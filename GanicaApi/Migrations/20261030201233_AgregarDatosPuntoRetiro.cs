using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanicaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosPuntoRetiro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PuntosRetiro",
                table: "PuntosRetiro");

            migrationBuilder.RenameTable(
                name: "PuntosRetiro",
                newName: "PuntoRetiro");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "PuntoRetiro",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Materiales",
                table: "PuntoRetiro",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PuntoRetiro",
                table: "PuntoRetiro",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PuntoRetiro",
                table: "PuntoRetiro");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "PuntoRetiro");

            migrationBuilder.DropColumn(
                name: "Materiales",
                table: "PuntoRetiro");

            migrationBuilder.RenameTable(
                name: "PuntoRetiro",
                newName: "PuntosRetiro");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PuntosRetiro",
                table: "PuntosRetiro",
                column: "Id");
        }
    }
}
