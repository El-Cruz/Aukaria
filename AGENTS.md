# AGENTS.md — Aukaria Backend Guidelines

.NET 9 Clean Architecture solution for **Aukaria**, a B2B SaaS platform for automated real estate title audit and property record analysis (Certificado de Tradición y Libertad - CTL) in Colombia.

---

## 1. Business & Product Context

- **Core Goal:** Analyze Colombian property records (CTL PDFs from SNR) using Claude API to emit legal risk diagnoses, interactive timelines, and executable Word reports (`.docx`).
- **Core Workflow:**
  1. **Upload & Fast Pre-analysis:** Extract FMI (Matrícula Inmobiliaria) from PDF in <500ms to check for existing database records (avoid duplicate API costs).
  2. **LLM Orchestration:** Send extracted CTL text + contextual prompt to Claude API using strict JSON output mode.
  3. **Interactive Dashboard:** Render viability traffic lights (🟢 Viable, 🟡 Requires Review, 🔴 Critical Alert), alerts, and timelines.
  4. **On-demand Report:** Inject JSON data into `.docx` template when requested by user.

---

## 2. Working Directory & Commands

- **Solution Location:** The solution is in `Aukaria/` (`C:\Workspace\Personal\Aukaria\Aukaria`). Always execute `dotnet` commands from this directory.
- **Solution File:** `Aukaria.slnx` (.NET 9+ XML format).

### Useful Commands
```bash
dotnet build Aukaria.slnx
dotnet run --project Aukaria.Api          # https://localhost:7078 (https profile is default); swagger UI at /swagger in Development
dotnet watch run --project Aukaria.Api
```

---

## 3. Implementation State & Rules

Project references enforce a one-way layering; never add a reference upward:

```
Aukaria.Api            -> Application, Infrastructure
Aukaria.Infrastructure -> Application, Domain
Aukaria.Application    -> Domain
Aukaria.Domain         -> (no dependencies)
```

- `Domain`: entities (`Empresa`, `Usuario`, `AnalisisPredial`) + enums (viabilidad, propósito, rol). No ORM/DB attributes.
- `Application`: use-case DTOs (`AnalisisResultadoJsonDto` = strict Claude JSON schema), interfaces (`IPdfExtractorService`, `IClaudeApiService`, `IReportGeneratorService`, `IAnalisisPredialRepository`, `IAnalisisPredialService`) and orchestration in `Services/AnalisisPredialService.cs`. `ConsumoTokens` is `0` (Claude service doesn't expose it yet).
- `Infrastructure`: `Persistence/` (EF Core `AukariaDbContext` + `AnalisisPredialConfiguration`), `Repositories/AnalisisPredialRepository`, `Services/` (`PdfExtractorService`, `ClaudeApiService`, `ReportGeneratorService`) and `DependencyInjection.AddInfrastructure`. EF Core + Npgsql (PostgreSQL) pinned to `9.x` (latest 10.x targets net10 and will not restore for net9.0). Migrations live in `Migrations/`; `Aukaria.Api/Program.cs` applies them automatically at startup when `ConnectionStrings:DefaultConnection` is configured.
- `Api`: `Program.cs` registers `AddInfrastructure`, `IAnalisisPredialService` (Scoped), enums as JSON strings, and CORS `AllowAll`; exposes `api/AnalisisPredial` via `AnalisisPredialController` (routes `pre-analisis`, `procesar`, `descargar-word/{id:guid}`). The sample `WeatherForecastController` was removed.
- No test project or test framework exists; none is committed to rely on.

### Gotchas

- `UglyToad.PdfPig` is pinned to `1.7.0-custom-5`. NuGet has no stable version registered, so `dotnet add package UglyToad.PdfPig` fails unless you add `--prerelease`.
- String properties in entities/DTOs default to `string.Empty` (nullable enable) to keep the build warning-free.