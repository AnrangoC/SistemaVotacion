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

    public DbSet<Provincia> Provincias { get; set; }
    public DbSet<Canton> Cantones { get; set; }
    public DbSet<Parroquia> Parroquias { get; set; }
    public DbSet<Barrio> Barrios { get; set; }

   
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Eleccion> Elecciones { get; set; }
    public DbSet<Lista> Listas { get; set; }
    public DbSet<Candidato> Candidatos { get; set; }
    public DbSet<Voto> Votos { get; set; }
}
