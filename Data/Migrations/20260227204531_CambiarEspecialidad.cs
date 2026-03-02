using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Obras.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CambiarEspecialidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Area",
                table: "UsuariosAcceso",
                newName: "Especialidad");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Especialidad",
                table: "UsuariosAcceso",
                newName: "Area");
        }
    }
}
