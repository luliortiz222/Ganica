using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GanicaApi.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHistorialSolicitudes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SolicitudId",
                table: "Notificaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistorialSolicitudes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SolicitudId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<long>(type: "bigint", nullable: false),
                    Accion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstadoAnterior = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstadoNuevo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fecha = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Observacion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialSolicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialSolicitudes_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialSolicitudes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_SolicitudId",
                table: "Notificaciones",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialSolicitudes_SolicitudId",
                table: "HistorialSolicitudes",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialSolicitudes_UsuarioId",
                table: "HistorialSolicitudes",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notificaciones_Solicitudes_SolicitudId",
                table: "Notificaciones",
                column: "SolicitudId",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notificaciones_Solicitudes_SolicitudId",
                table: "Notificaciones");

            migrationBuilder.DropTable(
                name: "HistorialSolicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_SolicitudId",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "SolicitudId",
                table: "Notificaciones");
        }
    }
}
