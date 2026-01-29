using Microsoft.EntityFrameworkCore;
using SistemaVotoModelos;

namespace SistemaVotoAPI.Data
{
    public class APIVotosDbContext : DbContext
    {
        public APIVotosDbContext(DbContextOptions<APIVotosDbContext> options)
            : base(options)
        {
        }
        public DbSet<Votante> Votantes { get; set; } = default!;
        public DbSet<Candidato> Candidatos { get; set; } = default!;
        public DbSet<Eleccion> Elecciones { get; set; } = default!;
        public DbSet<Lista> Listas { get; set; } = default!;
        public DbSet<VotoAnonimo> VotosAnonimos { get; set; } = default!;
        public DbSet<TokenAcceso> TokensAcceso { get; set; } = default!;
        public DbSet<Junta> Juntas { get; set; } = default!;
        public DbSet<Direccion> Direcciones { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // VOTANTE
            modelBuilder.Entity<Votante>()
                .HasKey(v => v.Cedula);

            modelBuilder.Entity<Votante>()
                .HasOne(v => v.Junta)
                .WithMany(j => j.Votantes)
                .HasForeignKey(v => v.JuntaId)
                .OnDelete(DeleteBehavior.Restrict);

            // DIRECCION
            modelBuilder.Entity<Direccion>()
                .HasKey(d => d.Id);

            // ELECCION
            modelBuilder.Entity<Eleccion>()
                .HasKey(e => e.Id);

            // LISTA
            modelBuilder.Entity<Lista>()
                .HasKey(l => l.Id);

            modelBuilder.Entity<Lista>()
                .HasOne<Eleccion>()
                .WithMany()
                .HasForeignKey(l => l.EleccionId)
                .OnDelete(DeleteBehavior.Cascade);

            // CANDIDATO
            modelBuilder.Entity<Candidato>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.Votante)
                .WithMany()
                .HasForeignKey(c => c.Cedula)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.Eleccion)
                .WithMany()
                .HasForeignKey(c => c.EleccionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.Lista)
                .WithMany()
                .HasForeignKey(c => c.ListaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Candidato>()
                .HasIndex(c => new { c.Cedula, c.EleccionId })
                .IsUnique();

            // JUNTA
            modelBuilder.Entity<Junta>()
                .HasKey(j => j.Id);

            modelBuilder.Entity<Junta>()
                .Property(j => j.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<Junta>()
                .HasOne(j => j.Direccion)
                .WithMany()
                .HasForeignKey(j => j.DireccionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Junta>()
                .HasOne(j => j.JefeDeJunta)
                .WithMany()
                .HasForeignKey(j => j.JefeDeJuntaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Junta>()
                .HasOne(j => j.Eleccion)
                .WithMany()
                .HasForeignKey(j => j.EleccionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Permite repetir la misma mesa en otra elección sin chocar
            modelBuilder.Entity<Junta>()
                .HasIndex(j => new { j.EleccionId, j.DireccionId, j.NumeroMesa })
                .IsUnique();

            // TOKEN ACCESO
            modelBuilder.Entity<TokenAcceso>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TokenAcceso>()
                .HasOne(t => t.Votante)
                .WithMany()
                .HasForeignKey(t => t.VotanteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Para el voto anonimo
            modelBuilder.Entity<VotoAnonimo>()
                .HasKey(v => v.Id);

            modelBuilder.Entity<VotoAnonimo>()
                .HasOne<Eleccion>()
                .WithMany()
                .HasForeignKey(v => v.EleccionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
