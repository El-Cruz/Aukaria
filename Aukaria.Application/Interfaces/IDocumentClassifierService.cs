using Aukaria.Application.DTOs;

namespace Aukaria.Application.Interfaces;

public interface IDocumentClassifierService
{
    ClasificacionDocumentoDto Clasificar(string textoExtraido);
}