using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaVotoModelos;

public class APIVotosDbContext : DbContext
{
    public APIVotosDbContext(DbContextOptions<APIVotosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Votante> Votantes { get; set; } = default!;
    public DbSet<Candidato> Candidatos { get; set; } = default!;
    public DbSet<Eleccion> Elecciones { get; set; } = default!;
    public DbSet<HistorialParticipacion> HistorialParticipaciones { get; set; } = default!;
    public DbSet<Lista> Listas { get; set; } = default!;
    public DbSet<VotoAnonimo> VotosAnonimos { get; set; } = default!;
}
