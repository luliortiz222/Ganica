using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanicaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoResiduoSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoResiduo",
                table: "Solicitudes",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId",
                table: "Notificaciones",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notificaciones_Usuarios_UsuarioId",
                table: "Notificaciones",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notificaciones_Usuarios_UsuarioId",
                table: "Notificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_UsuarioId",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "TipoResiduo",
                table: "Solicitudes");
        }
    }
}
