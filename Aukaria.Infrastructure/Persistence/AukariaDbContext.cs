using System.Reflection;
using Aukaria.Domain.Entities;
using Aukaria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aukaria.Infrastructure.Persistence;

public class AukariaDbContext : DbContext
{
    public AukariaDbContext(DbContextOptions<AukariaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<AnalisisPredial> AnalisisPrediales => Set<AnalisisPredial>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        Guid empresaDemoId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        modelBuilder.Entity<Empresa>().HasData(new Empresa
        {
            Id = empresaDemoId,
            Nombre = "Empresa Demo S.A.S.",
            Nit = "900123456-1",
            BolsaCreditos = 100,
            FechaRegistro = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Activo = true
        });

        modelBuilder.Entity<Usuario>().HasData(new Usuario
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            EmpresaId = empresaDemoId,
            Nombre = "Analista Predial Demo",
            Email = "demo@aukaria.co",
            PasswordHash = "HashDemo123",
            Rol = RolUsuario.AnalistaPredial,
            Activo = true,
            Provider = "",
            SubjectId = "",
            EmailConfirmado = false
        });

        base.OnModelCreating(modelBuilder);
    }
}