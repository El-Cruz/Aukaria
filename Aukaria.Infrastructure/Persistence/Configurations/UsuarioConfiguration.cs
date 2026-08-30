using Aukaria.Domain.Entities;
using Aukaria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aukaria.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500);
        builder.Property(u => u.Rol)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(RolUsuario.AnalistaPredial)
            .HasSentinel(RolUsuario.AnalistaPredial);
        builder.Property(u => u.Provider).HasMaxLength(40);
        builder.Property(u => u.SubjectId).HasMaxLength(500);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => new { u.Provider, u.SubjectId });
    }
}
