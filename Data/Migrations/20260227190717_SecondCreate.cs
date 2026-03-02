using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Obras.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecondCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mats_Users_UserId",
                table: "Mats");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Mats",
                newName: "TrabajadorId");

            migrationBuilder.RenameIndex(
                name: "IX_Mats_UserId",
                table: "Mats",
                newName: "IX_Mats_TrabajadorId");

            migrationBuilder.CreateTable(
                name: "UsuariosAcceso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Apellido = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Area = table.Column<string>(type: "TEXT", nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosAcceso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trabajadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreCompleto = table.Column<string>(type: "TEXT", nullable: false),
                    UsuarioAccesoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trabajadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trabajadores_UsuariosAcceso_UsuarioAccesoId",
                        column: x => x.UsuarioAccesoId,
                        principalTable: "UsuariosAcceso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_UsuarioAccesoId",
                table: "Trabajadores",
                column: "UsuarioAccesoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Mats_Trabajadores_TrabajadorId",
                table: "Mats",
                column: "TrabajadorId",
                principalTable: "Trabajadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mats_Trabajadores_TrabajadorId",
                table: "Mats");

            migrationBuilder.DropTable(
                name: "Trabajadores");

            migrationBuilder.DropTable(
                name: "UsuariosAcceso");

            migrationBuilder.RenameColumn(
                name: "TrabajadorId",
                table: "Mats",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Mats_TrabajadorId",
                table: "Mats",
                newName: "IX_Mats_UserId");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Mats_Users_UserId",
                table: "Mats",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
