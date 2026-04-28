# BelarusHeritage

[Русская версия](README.ru.md)

Web application about cultural heritage of Belarus with catalog, map, timeline, quizzes, and advanced route builder.

## Stack

- ASP.NET Core MVC (`.NET 8`)
- Entity Framework Core + Pomelo MySQL provider
- ASP.NET Core Identity + JWT (cookie token transport)
- Leaflet + MapLibre for maps
- OSRM (road routing + alternatives), Nominatim (geocoding/reverse geocoding)

## Main Features

- Heritage objects catalog with multilingual content (`ru`, `be`, `en`)
- Interactive map and timeline
- Quiz module
- Admin area for managing content
- Unified UI localization via `Localization/UiText.cs` (`UiText.Lang/Is/T`)
- Admin dashboard language switch (`ru` / `be` / `en`)
- Route Builder:
  - private start/end points
  - road-based route drawing
  - alternatives by segment with visual selection
  - route optimization
  - save personal private copy from public shared route
- Map page (`/Map`) uses the same map foundation as Route Builder/Share:
  - Leaflet + MapLibre localized vector style
  - fallback to OSM raster tiles

## Requirements

- .NET SDK 8
- MySQL 8+ (or compatible)

## Quick Start

1. Clone repository
2. Configure database connection in `appsettings.json` (or use environment overrides)
3. Restore and run:

```bash
dotnet restore
dotnet run
```

Application starts on local ASP.NET Core URLs (example: `https://localhost:xxxx` / `http://localhost:xxxx`).

## Database

- EF Core is configured in `Program.cs` via `UseMySql(...)`.
- Initial schema SQL is located in `Data/Migrations/001_InitialSchema.sql`.
- Startup seeding and idempotent schema checks are in `Data/DbSeeder.cs`.

## Configuration Notes

- `appsettings.json` contains runtime settings (site, JWT, mail, etc.).
- For local machine-specific secrets, prefer separate non-tracked files like:
  - `appsettings.Development.local.json`
  - `appsettings.Local.json`

These files are ignored by `.gitignore`.

## Localization

- UI strings are centralized in `Localization/UiText.cs` and used in Razor views via `UiText.T(Context, "key")`.
- Language is resolved from `culture` cookie and normalized to `ru`, `be`, or `en`.
- Multilingual content data remains in domain fields (`*_ru`, `*_be`, `*_en`).

## Maps and Routing

- Base map (`/Map`, `/RouteBuilder`, `/RouteBuilder/Share`): MapLibre localized style (with fallback to OSM raster tiles)
- Road routes and alternatives: `/RouteBuilder/BuildRoadPolyline` and `/RouteBuilder/BuildRoadAlternatives` (proxy endpoints)
- Geocoding: Nominatim

## Git Prep Checklist (before first push)

- Ensure local-only files are ignored (`.vs`, `artifacts_build`, `bin`, `obj`, logs)
- Verify no secrets are committed in tracked config files
- If generated files were already tracked before adding `.gitignore`, untrack them once:

```bash
git rm -r --cached .vs artifacts_build bin obj
git add .gitignore
git commit -m "chore: add gitignore and project readme"
```

## License

Add your chosen license file (`LICENSE`) before public release.
