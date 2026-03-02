using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Obras.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Mats",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Mats");
        }
    }
}
