# Graph Report - Aukaria  (2026-08-19)

## Corpus Check
- Corpus is ~44,656 words - fits in a single context window. You may not need a graph.

## Summary
- 602 nodes · 985 edges · 35 communities (32 shown, 3 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 37 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- API DTOs & Contracts
- Animation Recipes
- Claude API Integration
- Word Report Generation
- Web Frontend Dependencies
- Repository & Domain Entities
- Solution Project Layout
- Controller Endpoints
- PDF Extraction Services
- Debugging Skill (.agents)
- Analysis Dashboard UI
- Debugging Skill (.claude)
- Document Classification Service
- PDF Text Extraction Internals
- API Launch Configuration
- Database Migrations
- App Shell & Login Flow
- Google Morph Loader
- Aukaria Web Docs & Platform
- Landing Page
- AI Assistant Sidebar
- PDF Upload Zone
- Variant Preview
- Web API Service Layer
- Linter Configuration
- Brand & Social Icons
- Processing Overlay
- Railway Deploy Config
- Navbar & Language Selector
- Main App View
- Dependency Injection Wiring
- find-polluter Script
- Vercel Deploy Config

## God Nodes (most connected - your core abstractions)
1. `ReportGeneratorService` - 30 edges
2. `AnalisisResultadoJsonDto` - 23 edges
3. `Aukaria.Infrastructure` - 17 edges
4. `Aukaria.Domain.Enums` - 17 edges
5. `Animation Recipes` - 16 edges
6. `Systematic Debugging Skill (SKILL.md)` - 16 edges
7. `Aukaria.Application.Interfaces` - 15 edges
8. `AnalisisPredial` - 15 edges
9. `DocumentClassifierService` - 15 edges
10. `ClaudeApiService` - 14 edges

## Surprising Connections (you probably didn't know these)
- `AGENTS.md — Aukaria Backend Guidelines` --conceptually_related_to--> `Aukaria.Web index.html`  [INFERRED]
  AGENTS.md → Aukaria/Aukaria.Web/index.html
- `AGENTS.md — Aukaria Backend Guidelines` --conceptually_related_to--> `Aukaria.Web README (React + Vite)`  [INFERRED]
  AGENTS.md → Aukaria/Aukaria.Web/README.md
- `Aukaria Web SPA Entry (CTL Legal Audit Page)` --conceptually_related_to--> `Aukaria Platform`  [INFERRED]
  Aukaria/Aukaria.Web/index.html → AGENTS.md
- `Aukaria Web SPA Entry (CTL Legal Audit Page)` --conceptually_related_to--> `Core Workflow (Upload → LLM → Dashboard → Report)`  [INFERRED]
  Aukaria/Aukaria.Web/index.html → AGENTS.md
- `AnalisisResultadoJsonDto` --references--> `AlertaJuridicaDto`  [EXTRACTED]
  Aukaria/Aukaria.Application/DTOs/JsonSchema/AnalisisResultadoJsonDto.cs → Aukaria/Aukaria.Application/DTOs/JsonSchema/AlertaJuridicaDto.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Animation Recipe Collection** — _agents_skills_animate_recipes_button_press, _agents_skills_animate_recipes_popover_menu_select, _agents_skills_animate_recipes_tooltip, _agents_skills_animate_recipes_modal, _agents_skills_animate_recipes_drawer_sheet, _agents_skills_animate_recipes_toast, _agents_skills_animate_recipes_accordion, _agents_skills_animate_recipes_stagger_entrance, _agents_skills_animate_recipes_hold_to_confirm, _agents_skills_animate_recipes_tab_indicator, _agents_skills_animate_recipes_scroll_reveal, _agents_skills_animate_recipes_drag_to_dismiss, _agents_skills_animate_recipes_masking_crossfade, _agents_skills_animate_recipes_waapi [EXTRACTED 1.00]
- **Systematic Debugging Supporting Techniques** — _agents_skills_systematic_debugging_root_cause_tracing, _agents_skills_systematic_debugging_defense_in_depth, _agents_skills_systematic_debugging_condition_based_waiting [EXTRACTED 1.00]
- **Systematic Debugging Validation Tests** — _agents_skills_systematic_debugging_test_academic, _agents_skills_systematic_debugging_test_pressure_1, _agents_skills_systematic_debugging_test_pressure_2, _agents_skills_systematic_debugging_test_pressure_3 [EXTRACTED 1.00]
- **Systematic Debugging Supporting Techniques** — _claude_skills_systematic_debugging_skill, _claude_skills_systematic_debugging_root_cause_tracing, _claude_skills_systematic_debugging_defense_in_depth, _claude_skills_systematic_debugging_condition_based_waiting [EXTRACTED 1.00]
- **Four-Phase Debugging Process** — _claude_skills_systematic_debugging_skill_four_phases, _claude_skills_systematic_debugging_skill_iron_law, _claude_skills_systematic_debugging_skill_single_hypothesis_rule, _claude_skills_systematic_debugging_skill_question_architecture, _claude_skills_systematic_debugging_skill_red_flags [EXTRACTED 1.00]
- **Systematic Debugging Validation Test Suite** — _claude_skills_systematic_debugging_creation_log, _claude_skills_systematic_debugging_test_academic, _claude_skills_systematic_debugging_test_pressure_1, _claude_skills_systematic_debugging_test_pressure_2, _claude_skills_systematic_debugging_test_pressure_3 [EXTRACTED 1.00]
- **Social Media Footer Icon Set** — aukaria_aukaria_web_public_icons_bluesky_icon, aukaria_aukaria_web_public_icons_discord_icon, aukaria_aukaria_web_public_icons_github_icon, aukaria_aukaria_web_public_icons_x_icon [EXTRACTED 0.95]
- **Purple Stroke Line Icon Family** — aukaria_aukaria_web_public_icons_documentation_icon, aukaria_aukaria_web_public_icons_social_icon [EXTRACTED 0.95]

## Communities (35 total, 3 thin omitted)

### Community 0 - "API DTOs & Contracts"
Cohesion: 0.06
Nodes (27): AlertaJuridicaDto, AnotacionDto, TitularDto, TradicionActoDto, PreAnalisisFmiRequestDto, Guid, PdfInvalidoException, IClaudeApiService (+19 more)

### Community 1 - "Animation Recipes"
Cohesion: 0.06
Nodes (47): Animation Recipes, Accordion/Collapse Recipe, Button Press Recipe, Drag to Dismiss Recipe, Drawer/Sheet Recipe, Hold to Confirm Recipe, Masking Imperfect Crossfades Recipe, Modal Recipe (+39 more)

### Community 2 - "Claude API Integration"
Cohesion: 0.06
Nodes (29): AnthropicContentBlock, AnthropicMessage, SolicitudAnalisisRequestDto, Guid, AnalisisPredialResponseDto, DateTime, Guid, CancellationToken (+21 more)

### Community 3 - "Word Report Generation"
Cohesion: 0.19
Nodes (12): AnalisisResultadoJsonDto, List, ReportGeneratorService, CancellationToken, string, Task, Body, IReadOnlyList (+4 more)

### Community 4 - "Web Frontend Dependencies"
Cohesion: 0.06
Nodes (31): dependencies, framer-motion, react, react-dom, tailwindcss, @tailwindcss/vite, devDependencies, oxlint (+23 more)

### Community 5 - "Repository & Domain Entities"
Cohesion: 0.10
Nodes (21): IAnalisisPredialRepository, CancellationToken, Guid, Task, AnalisisPredial, DateTime, Guid, Empresa (+13 more)

### Community 6 - "Solution Project Layout"
Cohesion: 0.09
Nodes (25): Aukaria.Api, net9.0, Microsoft.EntityFrameworkCore.Design (9.0.2), Aukaria.Application, net9.0, Microsoft.NET.Sdk, Aukaria.Domain, net9.0 (+17 more)

### Community 7 - "Controller Endpoints"
Cohesion: 0.14
Nodes (19): AnalisisPredialController, CancellationToken, Guid, Task, PreAnalisisFmiResponseDto, ClasificacionDocumentoDto, DateTime, Guid (+11 more)

### Community 8 - "PDF Extraction Services"
Cohesion: 0.15
Nodes (13): ReporteWordDto, IPdfExtractorService, CancellationToken, Stream, Task, IReportGeneratorService, CancellationToken, Task (+5 more)

### Community 9 - "Debugging Skill (.agents)"
Cohesion: 0.13
Nodes (22): Condition-Based Waiting, condition-based-waiting-example.ts, waitFor Polling Function, Creation Log: Systematic Debugging Skill, Defense-in-Depth Validation, Four Validation Layers, Root Cause Tracing, Backward Call-Stack Tracing (+14 more)

### Community 10 - "Analysis Dashboard UI"
Cohesion: 0.12
Nodes (17): AnalysisDashboard(), DEFAULT_ALERTS, DEFAULT_FICHA, mapAlerta(), mapAnotacion(), nivelRiesgo(), RISK, spring (+9 more)

### Community 11 - "Debugging Skill (.claude)"
Cohesion: 0.13
Nodes (19): Condition-Based Waiting, waitFor Polling Helper, Systematic Debugging Creation Log, Four Validation Tests, Defense-in-Depth Validation, Four Validation Layers, Root Cause Tracing, Backward Call-Stack Tracing (+11 more)

### Community 12 - "Document Classification Service"
Cohesion: 0.22
Nodes (6): ClasificacionDocumentoDto, IDocumentClassifierService, DocumentClassifierService, int, List, Regex

### Community 13 - "PDF Text Extraction Internals"
Cohesion: 0.23
Nodes (8): ExtraccionPreAnalisisDto, PdfExtractorService, CancellationToken, int, Regex, Stream, Task, PdfDocument

### Community 14 - "API Launch Configuration"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 15 - "Database Migrations"
Cohesion: 0.15
Nodes (8): InicialPostgres, ModelBuilder, AukariaDbContextModelSnapshot, ModelBuilder, Aukaria.Infrastructure.Migrations, Migration, MigrationBuilder, ModelSnapshot

### Community 16 - "App Shell & Login Flow"
Cohesion: 0.18
Nodes (9): App(), extraerInfoPredio(), fade, spring, LandingPage(), LoginModal(), spring, springBtn (+1 more)

### Community 17 - "Google Morph Loader"
Cohesion: 0.14
Nodes (9): balanceSwing, iconEnter, iconExit, ICONOS, morphLoop, needleSpin, scanSweep, sealDraw (+1 more)

### Community 18 - "Aukaria Web Docs & Platform"
Cohesion: 0.19
Nodes (13): AGENTS.md — Aukaria Backend Guidelines, Aukaria Platform, Claude API, Clean Architecture One-Way Layering, Core Workflow (Upload → LLM → Dashboard → Report), Certificado de Tradición y Libertad (CTL), Aukaria.Web index.html, Aukaria Web SPA Entry (CTL Legal Audit Page) (+5 more)

### Community 19 - "Landing Page"
Cohesion: 0.18
Nodes (6): fadeUp, FEATURES, METRICAS, NAV_LINKS, PASOS, spring

### Community 20 - "AI Assistant Sidebar"
Cohesion: 0.22
Nodes (6): PREGUNTAS_WORKSPACE, respuestaIA(), RightSidebar(), spring, viabilidadKey(), VIABILITY

### Community 21 - "PDF Upload Zone"
Cohesion: 0.29
Nodes (7): fade, PdfUploadZone(), spring, identificadorPreAnalisis(), PREGUNTAS_POR_TIPO, resolvedorTipo(), TIPOS_DOCUMENTO

### Community 23 - "Web API Service Layer"
Cohesion: 0.31
Nodes (8): CLEAN_URL, descargarReporteWord(), EMPRESA_ID, preAnalizarCtl(), procesarAnalisisCtl(), request(), ROOT_URL, USUARIO_ID

### Community 24 - "Linter Configuration"
Cohesion: 0.25
Nodes (7): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, warn

### Community 25 - "Brand & Social Icons"
Cohesion: 0.25
Nodes (8): Aukaria Brand Favicon, Bluesky Social Icon, Discord Social Icon, Documentation Link Icon, GitHub Social Icon, Settings/Social Profile Icon, App Icon SVG Sprite, X (Twitter) Social Icon

### Community 26 - "Processing Overlay"
Cohesion: 0.25
Nodes (5): GoogleMorphLoader(), fade, ProcessingOverlay(), spring, STEPS

### Community 27 - "Railway Deploy Config"
Cohesion: 0.25
Nodes (7): build, builder, dockerfilePath, deploy, restartPolicyMaxRetries, restartPolicyType, $schema

### Community 28 - "Navbar & Language Selector"
Cohesion: 0.33
Nodes (5): LANGUAGES, LanguageSelector(), spring, Navbar(), spring

### Community 29 - "Main App View"
Cohesion: 0.38
Nodes (5): fechaTexto(), MainAppView(), spring, viabilidadKey(), VIABILITY

### Community 30 - "Dependency Injection Wiring"
Cohesion: 0.60
Nodes (3): DependencyInjection, IConfiguration, IServiceCollection

## Knowledge Gaps
- **151 isolated node(s):** `find-polluter.sh script`, `find-polluter.sh script`, `net9.0`, `Microsoft.EntityFrameworkCore.Design (9.0.2)`, `Swashbuckle.AspNetCore (10.2.3)` (+146 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Aukaria.Application.Interfaces` connect `API DTOs & Contracts` to `PDF Extraction Services`, `Document Classification Service`, `Repository & Domain Entities`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **Why does `AnalisisPredialService` connect `PDF Extraction Services` to `API DTOs & Contracts`, `Document Classification Service`, `Repository & Domain Entities`, `Controller Endpoints`?**
  _High betweenness centrality (0.030) - this node is a cross-community bridge._
- **Why does `AnalisisResultadoJsonDto` connect `Word Report Generation` to `API DTOs & Contracts`, `PDF Extraction Services`, `Claude API Integration`?**
  _High betweenness centrality (0.030) - this node is a cross-community bridge._
- **What connects `find-polluter.sh script`, `find-polluter.sh script`, `net9.0` to the rest of the system?**
  _151 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `API DTOs & Contracts` be split into smaller, more focused modules?**
  _Cohesion score 0.05858585858585859 - nodes in this community are weakly interconnected._
- **Should `Animation Recipes` be split into smaller, more focused modules?**
  _Cohesion score 0.056429232192414434 - nodes in this community are weakly interconnected._
- **Should `Claude API Integration` be split into smaller, more focused modules?**
  _Cohesion score 0.06342780026990553 - nodes in this community are weakly interconnected._