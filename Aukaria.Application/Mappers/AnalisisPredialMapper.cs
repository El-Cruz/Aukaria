using Aukaria.Application.DTOs.JsonSchema;
using Aukaria.Domain.Enums;
using Aukaria.Domain.Models;

namespace Aukaria.Application.Mappers;

public static class AnalisisPredialMapper
{
    public static DiagnosticoPredialModel ToDomainModel(this AnalisisResultadoJsonDto dto)
    {
        if (dto is null) return new DiagnosticoPredialModel();

        return new DiagnosticoPredialModel
        {
            MatriculaFMI = dto.MatriculaFMI,
            ORIP = dto.ORIP,
            Departamento = dto.Departamento,
            Municipio = dto.Municipio,
            Vereda = dto.Vereda,
            NombrePredio = dto.NombrePredio,
            AreaRegistrada = dto.AreaRegistrada,
            PropietarioActual = dto.PropietarioActual,
            EstadoFolio = dto.EstadoFolio,
            Viabilidad = MapearViabilidad(dto.Viabilidad),
            ResumenEjecutivo = dto.ResumenEjecutivo,
            Anotaciones = dto.Anotaciones?.Select(a => new AnotacionModel
            {
                Numero = a.NumeroAnotacion,
                Fecha = a.Fecha,
                Tipo = a.Especificacion,
                Descripcion = a.NaturalezaJuridica
            }).ToList() ?? new List<AnotacionModel>(),
            AlertasJuridicas = dto.AlertasJuridicas?.Select(a => new AlertaJuridicaModel
            {
                NivelRiesgo = a.NivelRiesgo,
                Descripcion = a.Descripcion,
                Recomendacion = a.Recomendacion
            }).ToList() ?? new List<AlertaJuridicaModel>(),
            Observaciones = dto.Observaciones?.ToList() ?? new List<string>(),
            ObservacionAmbiental = dto.ObservacionAmbiental,
            ExclusionResponsabilidad = dto.ExclusionResponsabilidad,
            NumeroEscritura = dto.NumeroEscritura,
            Notaria = dto.Notaria,
            CiudadNotaria = dto.CiudadNotaria,
            FechaEscritura = dto.FechaEscritura,
            Otorgantes = dto.Otorgantes?.ToList() ?? new List<string>(),
            Cuantia = dto.Cuantia,
            Linderos = dto.Linderos,
            CedulaCatastral = dto.CedulaCatastral,
            Nupre = dto.Nupre,
            FolioMatriz = dto.FolioMatriz,
            FoliosDerivados = dto.FoliosDerivados,
            FechaExpedicionCertificado = dto.FechaExpedicionCertificado,
            OrigenCabidaActual = dto.OrigenCabidaActual,
            ConclusionPredial = dto.ConclusionPredial,
            AreaSegunFmi = dto.AreaSegunFmi,
            AreaAdjudicacionInicial = dto.AreaAdjudicacionInicial,
            DesenglobesVentasParciales = dto.DesenglobesVentasParciales,
            AreaRemanenteReal = dto.AreaRemanenteReal,
            LinderosDescripcion = dto.LinderosDescripcion,
            SoporteDocumentalLinderos = dto.SoporteDocumentalLinderos,
            Titulares = dto.Titulares?.Select(t => new TitularModel
            {
                Nombre = t.Nombre,
                TipoDocumento = t.CondicionDominio,
                NumeroDocumento = t.Identificacion,
                Porcentaje = t.ParticipacionCuota
            }).ToList() ?? new List<TitularModel>(),
            RegimenPropiedadAnalisis = dto.RegimenPropiedadAnalisis,
            TradicionActos = dto.TradicionActos?.Select(t => new TradicionActoModel
            {
                NumeroEscritura = t.NumeroAnotacion,
                Fecha = t.Fecha,
                Notaria = t.CodigoSnr,
                Otorgante = t.ActoJuridico,
                Adquirente = t.CadenaDeDominio,
                Cuantia = t.AnalisisJuridico
            }).ToList() ?? new List<TradicionActoModel>(),
            CertificacionTracto = dto.CertificacionTracto,
            DiagnosticoEjecutivo = dto.DiagnosticoEjecutivo,
            DocumentosAnalizados = dto.DocumentosAnalizados?.ToList() ?? new List<string>()
        };
    }

    public static AnalisisResultadoJsonDto ToDto(this DiagnosticoPredialModel model)
    {
        if (model is null) return new AnalisisResultadoJsonDto();

        return new AnalisisResultadoJsonDto
        {
            MatriculaFMI = model.MatriculaFMI,
            ORIP = model.ORIP,
            Departamento = model.Departamento,
            Municipio = model.Municipio,
            Vereda = model.Vereda,
            NombrePredio = model.NombrePredio,
            AreaRegistrada = model.AreaRegistrada,
            PropietarioActual = model.PropietarioActual,
            EstadoFolio = model.EstadoFolio,
            Viabilidad = model.Viabilidad.ToString(),
            ResumenEjecutivo = model.ResumenEjecutivo,
            Anotaciones = model.Anotaciones?.Select(a => new AnotacionDto
            {
                NumeroAnotacion = a.Numero,
                Fecha = a.Fecha,
                Especificacion = a.Tipo,
                NaturalezaJuridica = a.Descripcion
            }).ToList() ?? new List<AnotacionDto>(),
            AlertasJuridicas = model.AlertasJuridicas?.Select(a => new AlertaJuridicaDto
            {
                NivelRiesgo = a.NivelRiesgo,
                Descripcion = a.Descripcion,
                Recomendacion = a.Recomendacion
            }).ToList() ?? new List<AlertaJuridicaDto>(),
            Observaciones = model.Observaciones?.ToList() ?? new List<string>(),
            ObservacionAmbiental = model.ObservacionAmbiental,
            ExclusionResponsabilidad = model.ExclusionResponsabilidad,
            NumeroEscritura = model.NumeroEscritura,
            Notaria = model.Notaria,
            CiudadNotaria = model.CiudadNotaria,
            FechaEscritura = model.FechaEscritura,
            Otorgantes = model.Otorgantes?.ToList() ?? new List<string>(),
            Cuantia = model.Cuantia,
            Linderos = model.Linderos,
            CedulaCatastral = model.CedulaCatastral,
            Nupre = model.Nupre,
            FolioMatriz = model.FolioMatriz,
            FoliosDerivados = model.FoliosDerivados,
            FechaExpedicionCertificado = model.FechaExpedicionCertificado,
            OrigenCabidaActual = model.OrigenCabidaActual,
            ConclusionPredial = model.ConclusionPredial,
            AreaSegunFmi = model.AreaSegunFmi,
            AreaAdjudicacionInicial = model.AreaAdjudicacionInicial,
            DesenglobesVentasParciales = model.DesenglobesVentasParciales,
            AreaRemanenteReal = model.AreaRemanenteReal,
            LinderosDescripcion = model.LinderosDescripcion,
            SoporteDocumentalLinderos = model.SoporteDocumentalLinderos,
            Titulares = model.Titulares?.Select(t => new TitularDto
            {
                Nombre = t.Nombre,
                CondicionDominio = t.TipoDocumento,
                Identificacion = t.NumeroDocumento,
                ParticipacionCuota = t.Porcentaje
            }).ToList() ?? new List<TitularDto>(),
            RegimenPropiedadAnalisis = model.RegimenPropiedadAnalisis,
            TradicionActos = model.TradicionActos?.Select(t => new TradicionActoDto
            {
                NumeroAnotacion = t.NumeroEscritura,
                Fecha = t.Fecha,
                CodigoSnr = t.Notaria,
                ActoJuridico = t.Otorgante,
                CadenaDeDominio = t.Adquirente,
                AnalisisJuridico = t.Cuantia
            }).ToList() ?? new List<TradicionActoDto>(),
            CertificacionTracto = model.CertificacionTracto,
            DiagnosticoEjecutivo = model.DiagnosticoEjecutivo,
            DocumentosAnalizados = model.DocumentosAnalizados?.ToList() ?? new List<string>()
        };
    }

    private static EstadoViabilidad MapearViabilidad(string viabilidad)
    {
        if (string.IsNullOrWhiteSpace(viabilidad))
            return EstadoViabilidad.RequiereRevision;

        var normalized = viabilidad.Trim().ToLowerInvariant();

        return normalized switch
        {
            "viable" => EstadoViabilidad.Viable,
            "no viable" or "alerta critica" or "alertacrítica" => EstadoViabilidad.AlertaCritica,
            _ => EstadoViabilidad.RequiereRevision
        };
    }
}
