using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Obras.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequerimientosMasterDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mats");

            migrationBuilder.CreateTable(
                name: "Requerimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    TrabajadorId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requerimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requerimientos_Trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetallesRequerimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    RequerimientoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesRequerimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesRequerimiento_Requerimientos_RequerimientoId",
                        column: x => x.RequerimientoId,
                        principalTable: "Requerimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRequerimiento_RequerimientoId",
                table: "DetallesRequerimiento",
                column: "RequerimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Requerimientos_TrabajadorId",
                table: "Requerimientos",
                column: "TrabajadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesRequerimiento");

            migrationBuilder.DropTable(
                name: "Requerimientos");

            migrationBuilder.CreateTable(
                name: "Mats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrabajadorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mats_Trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mats_TrabajadorId",
                table: "Mats",
                column: "TrabajadorId");
        }
    }
}
