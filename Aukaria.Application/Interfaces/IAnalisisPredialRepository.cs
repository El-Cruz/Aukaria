using Aukaria.Domain.Entities;

namespace Aukaria.Application.Interfaces;

public interface IAnalisisPredialRepository
{
    Task<AnalisisPredial?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AnalisisPredial?> ObtenerUltimoPorFmiAsync(string matriculaFmi, Guid empresaId, CancellationToken cancellationToken = default);
    Task AgregarAsync(AnalisisPredial analisis, CancellationToken cancellationToken = default);
}