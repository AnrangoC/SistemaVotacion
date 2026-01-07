using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaVotoAPI.Migrations
{
    /// <inheritdoc />
    public partial class MigracionBeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Elecciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    TipoEleccion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elecciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Votantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCompleto = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FotoUrl = table.Column<string>(type: "text", nullable: false),
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Listas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Siglas = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: false),
                    EleccionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listas_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistorialParticipaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FechaVoto = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EleccionId = table.Column<int>(type: "integer", nullable: true),
                    VotanteId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialParticipaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialParticipaciones_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistorialParticipaciones_Votantes_VotanteId",
                        column: x => x.VotanteId,
                        principalTable: "Votantes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Candidatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cargo = table.Column<string>(type: "text", nullable: false),
                    DatosVotanteId = table.Column<int>(type: "integer", nullable: true),
                    ListaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidatos_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Candidatos_Votantes_DatosVotanteId",
                        column: x => x.DatosVotanteId,
                        principalTable: "Votantes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VotosAnonimos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FechaVoto = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EleccionId = table.Column<int>(type: "integer", nullable: true),
                    ListaId = table.Column<int>(type: "integer", nullable: true),
                    CandidatoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotosAnonimos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VotosAnonimos_Candidatos_CandidatoId",
                        column: x => x.CandidatoId,
                        principalTable: "Candidatos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VotosAnonimos_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VotosAnonimos_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_DatosVotanteId",
                table: "Candidatos",
                column: "DatosVotanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_ListaId",
                table: "Candidatos",
                column: "ListaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialParticipaciones_EleccionId",
                table: "HistorialParticipaciones",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialParticipaciones_VotanteId",
                table: "HistorialParticipaciones",
                column: "VotanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Listas_EleccionId",
                table: "Listas",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_VotosAnonimos_CandidatoId",
                table: "VotosAnonimos",
                column: "CandidatoId");

            migrationBuilder.CreateIndex(
                name: "IX_VotosAnonimos_EleccionId",
                table: "VotosAnonimos",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_VotosAnonimos_ListaId",
                table: "VotosAnonimos",
                column: "ListaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialParticipaciones");

            migrationBuilder.DropTable(
                name: "VotosAnonimos");

            migrationBuilder.DropTable(
                name: "Candidatos");

            migrationBuilder.DropTable(
                name: "Listas");

            migrationBuilder.DropTable(
                name: "Votantes");

            migrationBuilder.DropTable(
                name: "Elecciones");
        }
    }
}
