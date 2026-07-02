using BarberShopApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BarberShopApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Barbeiro> Barbeiros { get; set; }
    public DbSet<Servico> Servicos { get; set; }
    public DbSet<Disponibilidade> Disponibilidades { get; set; }
    public DbSet<Agendamento> Agendamentos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Email único por usuário
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Proteção contra agendamento duplicado (mesmo barbeiro, mesmo horário)
        modelBuilder.Entity<Agendamento>()
            .HasIndex(a => new { a.BarbeiroId, a.DataHoraInicio })
            .IsUnique();

        // Precisão do campo Preco no banco
        modelBuilder.Entity<Servico>()
            .Property(s => s.Preco)
            .HasColumnType("numeric(10,2)");

        // Relacionamentos
        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Usuario)
            .WithMany(u => u.Agendamentos)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Barbeiro)
            .WithMany(b => b.Agendamentos)
            .HasForeignKey(a => a.BarbeiroId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Servico)
            .WithMany(s => s.Agendamentos)
            .HasForeignKey(a => a.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Disponibilidade>()
            .HasOne(d => d.Barbeiro)
            .WithMany(b => b.Disponibilidades)
            .HasForeignKey(d => d.BarbeiroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}