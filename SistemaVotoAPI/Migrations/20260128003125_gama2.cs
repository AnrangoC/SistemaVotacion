using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVotoAPI.Migrations
{
    /// <inheritdoc />
    public partial class gama2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EleccionId",
                table: "Juntas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Juntas_EleccionId_DireccionId_NumeroMesa",
                table: "Juntas",
                columns: new[] { "EleccionId", "DireccionId", "NumeroMesa" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Juntas_Elecciones_EleccionId",
                table: "Juntas",
                column: "EleccionId",
                principalTable: "Elecciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Juntas_Elecciones_EleccionId",
                table: "Juntas");

            migrationBuilder.DropIndex(
                name: "IX_Juntas_EleccionId_DireccionId_NumeroMesa",
                table: "Juntas");

            migrationBuilder.DropColumn(
                name: "EleccionId",
                table: "Juntas");
        }
    }
}
