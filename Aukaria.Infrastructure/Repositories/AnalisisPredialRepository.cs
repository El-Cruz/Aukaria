using Aukaria.Application.Interfaces;
using Aukaria.Domain.Entities;
using Aukaria.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aukaria.Infrastructure.Repositories;

public sealed class AnalisisPredialRepository : IAnalisisPredialRepository
{
    private readonly AukariaDbContext _context;

    public AnalisisPredialRepository(AukariaDbContext context)
    {
        _context = context;
    }

    public Task<AnalisisPredial?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.AnalisisPrediales
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<AnalisisPredial?> ObtenerUltimoPorFmiAsync(string matriculaFmi, Guid empresaId, CancellationToken cancellationToken = default)
    {
        return _context.AnalisisPrediales
            .AsNoTracking()
            .Where(a => a.MatriculaFMI == matriculaFmi && a.EmpresaId == empresaId && !a.Reemplazado)
            .OrderByDescending(a => a.FechaExpedicionCertificado)
            .ThenByDescending(a => a.FechaAnalisis)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<AnalisisPredial>> ListarVigentesPorFmiAsync(string matriculaFmi, Guid empresaId, CancellationToken cancellationToken = default)
    {
        return _context.AnalisisPrediales
            .AsNoTracking()
            .Where(a => a.MatriculaFMI == matriculaFmi && a.EmpresaId == empresaId && !a.Reemplazado)
            .OrderByDescending(a => a.FechaExpedicionCertificado)
            .ThenByDescending(a => a.FechaAnalisis)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AnalisisPredial>> ObtenerHistorialPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await _context.AnalisisPrediales
            .AsNoTracking()
            .Where(a => a.UsuarioId == usuarioId && !a.Reemplazado)
            .OrderByDescending(a => a.FechaAnalisis)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(AnalisisPredial analisis, CancellationToken cancellationToken = default)
    {
        await _context.AnalisisPrediales.AddAsync(analisis, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task GuardarDeduplicacionAsync(
        AnalisisPredial nuevoAnalisis,
        IReadOnlyCollection<Guid> idsAReemplazar,
        Guid reemplazadoPorId,
        CancellationToken cancellationToken = default)
    {
        if (idsAReemplazar.Count > 0)
        {
            var previos = await _context.AnalisisPrediales
                .Where(a => idsAReemplazar.Contains(a.Id))
                .ToListAsync(cancellationToken);

            foreach (var previo in previos)
            {
                previo.Reemplazado = true;
                previo.ReemplazadoPorId = reemplazadoPorId;
            }
        }

        await _context.AnalisisPrediales.AddAsync(nuevoAnalisis, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}