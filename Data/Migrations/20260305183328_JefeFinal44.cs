using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Obras.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class JefeFinal44 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Proyectos_ProyectoId",
                table: "Trabajadores");

            migrationBuilder.DropIndex(
                name: "IX_Trabajadores_ProyectoId",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "ProyectoId",
                table: "Trabajadores");

            migrationBuilder.CreateTable(
                name: "TrabajadorProyecto",
                columns: table => new
                {
                    ProyectosId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrabajadoresId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrabajadorProyecto", x => new { x.ProyectosId, x.TrabajadoresId });
                    table.ForeignKey(
                        name: "FK_TrabajadorProyecto_Proyectos_ProyectosId",
                        column: x => x.ProyectosId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrabajadorProyecto_Trabajadores_TrabajadoresId",
                        column: x => x.TrabajadoresId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrabajadorProyecto_TrabajadoresId",
                table: "TrabajadorProyecto",
                column: "TrabajadoresId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrabajadorProyecto");

            migrationBuilder.AddColumn<int>(
                name: "ProyectoId",
                table: "Trabajadores",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_ProyectoId",
                table: "Trabajadores",
                column: "ProyectoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Proyectos_ProyectoId",
                table: "Trabajadores",
                column: "ProyectoId",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
