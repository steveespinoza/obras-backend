using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Obras.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiTenantProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProyectoId",
                table: "Trabajadores",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProyectoId",
                table: "Requerimientos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProyectoId",
                table: "MaterialesAlmacen",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Ubicacion = table.Column<string>(type: "TEXT", nullable: false),
                    AdminId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_UsuariosAcceso_AdminId",
                        column: x => x.AdminId,
                        principalTable: "UsuariosAcceso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_ProyectoId",
                table: "Trabajadores",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_Requerimientos_ProyectoId",
                table: "Requerimientos",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesAlmacen_ProyectoId",
                table: "MaterialesAlmacen",
                column: "ProyectoId");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_AdminId",
                table: "Proyectos",
                column: "AdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialesAlmacen_Proyectos_ProyectoId",
                table: "MaterialesAlmacen",
                column: "ProyectoId",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requerimientos_Proyectos_ProyectoId",
                table: "Requerimientos",
                column: "ProyectoId",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajadores_Proyectos_ProyectoId",
                table: "Trabajadores",
                column: "ProyectoId",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialesAlmacen_Proyectos_ProyectoId",
                table: "MaterialesAlmacen");

            migrationBuilder.DropForeignKey(
                name: "FK_Requerimientos_Proyectos_ProyectoId",
                table: "Requerimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajadores_Proyectos_ProyectoId",
                table: "Trabajadores");

            migrationBuilder.DropTable(
                name: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Trabajadores_ProyectoId",
                table: "Trabajadores");

            migrationBuilder.DropIndex(
                name: "IX_Requerimientos_ProyectoId",
                table: "Requerimientos");

            migrationBuilder.DropIndex(
                name: "IX_MaterialesAlmacen_ProyectoId",
                table: "MaterialesAlmacen");

            migrationBuilder.DropColumn(
                name: "ProyectoId",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "ProyectoId",
                table: "Requerimientos");

            migrationBuilder.DropColumn(
                name: "ProyectoId",
                table: "MaterialesAlmacen");
        }
    }
}
