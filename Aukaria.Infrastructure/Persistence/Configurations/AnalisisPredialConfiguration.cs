using Aukaria.Domain.Entities;
using Aukaria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aukaria.Infrastructure.Persistence.Configurations;

public class AnalisisPredialConfiguration : IEntityTypeConfiguration<AnalisisPredial>
{
    public void Configure(EntityTypeBuilder<AnalisisPredial> builder)
    {
        builder.ToTable("AnalisisPrediales");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.MatriculaFMI).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ORIP).HasMaxLength(100);
        builder.Property(a => a.NombrePredio).HasMaxLength(200);
        builder.Property(a => a.TipoDocumento)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(TipoDocumentoJuridico.CTL)
            .HasSentinel(TipoDocumentoJuridico.CTL);
        builder.Property(a => a.ResumenEjecutivo).HasColumnType("nvarchar(max)");
        builder.Property(a => a.ResultadoJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(a => new { a.MatriculaFMI, a.EmpresaId });
    }
}