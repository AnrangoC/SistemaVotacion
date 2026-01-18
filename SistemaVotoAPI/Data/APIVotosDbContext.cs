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
        // ENTIDADES PRINCIPALES
        public DbSet<Votante> Votantes { get; set; } = default!;
        public DbSet<Candidato> Candidatos { get; set; } = default!;
        public DbSet<Eleccion> Elecciones { get; set; } = default!;
        public DbSet<Lista> Listas { get; set; } = default!;
        public DbSet<VotoAnonimo> VotosAnonimos { get; set; } = default!;
        public DbSet<TokenAcceso> TokensAcceso { get; set; } = default!;
        public DbSet<Junta> Juntas { get; set; } = default!;
        public DbSet<Direccion> Direcciones { get; set; } = default!;
        
        // CONFIGURACIÓN DE MODELOS
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- VOTANTE ----
            modelBuilder.Entity<Votante>()
                .HasKey(v => v.Cedula);

            modelBuilder.Entity<Votante>()
                .HasOne<Junta>()
                .WithMany()
                .HasForeignKey(v => v.JuntaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- CANDIDATO (HERENCIA) ----
            modelBuilder.Entity<Candidato>()
                .HasBaseType<Votante>();

            // ---- JUNTA ----
            modelBuilder.Entity<Junta>()
                .HasKey(j => j.Id);

            // ---- DIRECCION ----
            modelBuilder.Entity<Direccion>()
                .HasKey(d => d.Id);

            // ---- TOKEN ACCESO ----
            modelBuilder.Entity<TokenAcceso>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TokenAcceso>()
                .HasOne<Votante>()
                .WithMany()
                .HasForeignKey(t => t.VotanteId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---- VOTO ANONIMO ----
            modelBuilder.Entity<VotoAnonimo>()
                .HasKey(v => v.Id);
        }
    }
}
