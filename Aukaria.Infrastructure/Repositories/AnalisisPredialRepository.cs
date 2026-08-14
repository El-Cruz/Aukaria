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
            .Where(a => a.MatriculaFMI == matriculaFmi && a.EmpresaId == empresaId)
            .OrderByDescending(a => a.FechaAnalisis)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AgregarAsync(AnalisisPredial analisis, CancellationToken cancellationToken = default)
    {
        await _context.AnalisisPrediales.AddAsync(analisis, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}