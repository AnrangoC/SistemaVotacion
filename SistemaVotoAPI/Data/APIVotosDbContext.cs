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

        // DbSets
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

            // ---------------- VOTANTE ----------------
            modelBuilder.Entity<Votante>()
                .HasKey(v => v.Cedula);

            modelBuilder.Entity<Votante>()
                .HasOne(v => v.Junta)
                .WithMany(j => j.Votantes)
                .HasForeignKey(v => v.JuntaId)
                .OnDelete(DeleteBehavior.Restrict);

            // HERENCIA VOTANTE -> CANDIDATO 
            modelBuilder.Entity<Votante>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Votante>("VOTANTE")
                .HasValue<Candidato>("CANDIDATO");

            // JUNTA
            modelBuilder.Entity<Junta>()
                .HasKey(j => j.Id);

            modelBuilder.Entity<Junta>()
                .HasOne(j => j.JefeDeJunta)
                .WithMany()
                .HasForeignKey(j => j.JefeDeJuntaId)
                .OnDelete(DeleteBehavior.Restrict);

            // DIRECCION
            modelBuilder.Entity<Direccion>()
                .HasKey(d => d.Id);

            // ELECCION
            modelBuilder.Entity<Eleccion>()
                .HasKey(e => e.Id);

            //LISTA
            modelBuilder.Entity<Lista>()
                .HasKey(l => l.Id);

            modelBuilder.Entity<Lista>()
                .HasOne<Eleccion>()
                .WithMany()
                .HasForeignKey(l => l.EleccionId)
                .OnDelete(DeleteBehavior.Cascade);

            //TOKEN ACCESO
            modelBuilder.Entity<TokenAcceso>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TokenAcceso>()
                .HasOne(t => t.Votante)
                .WithMany()
                .HasForeignKey(t => t.VotanteId)
                .OnDelete(DeleteBehavior.Cascade);

            // VOTO ANONIMO
            // NO tiene relación con Votante ni Candidato
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
