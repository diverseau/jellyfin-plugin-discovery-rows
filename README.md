# Jellyfin Discovery Rows

An installable server plugin that adds **38 discovery shelves** to **Home Screen Sections 3.0.2.0** on **Jellyfin 10.11.11**. Shelves offer different reasons to watch your local media: time available, mood, quality, recognition, complete stories and themes.

Version **1.0.3**, built and verified on **11 October 2026, Australia/Perth**. The authoritative project folder is `/home/diverse/Projects/jellyfin-plugin-discovery-rows`.

## Install and use

Add this repository in **Dashboard → Plugins → Repositories → New Repository**:

- **Name:** Discovery Rows
- **URL:** `https://raw.githubusercontent.com/diverseau/jellyfin-plugin-discovery-rows/main/manifest.json`

Then install **Discovery Rows** from the Catalogue and restart Jellyfin. Install HSS, File Transformation and Plugin Pages separately. Follow [INSTALL.md](docs/INSTALL.md) for dependencies and shelf setup. The [release download](https://github.com/diverseau/jellyfin-plugin-discovery-rows/releases/tag/v1.0.3) also supports manual installation. This release has been verified on disposable servers; it has not been deployed to a production server.

The first 15 shelves are available by default; seven award/completeness shelves and sixteen themes are optional. Use **Setup → Enable all shelves in HSS** once, leaving personal selection unchecked, then choose what appears only in **Discovery Rows → Shelves**. Select all / Clear selection are available. Restart once after the HSS setup; later shelf changes need only a save and Home refresh. HSS controls ordering, while Discovery Rows controls eligibility, scores, explanations and coordination. Individual selections in Modular Home remain optional.

- Runtime/genre shelves work offline without credentials.
- Mood/theme shelves automatically match genres, existing tags, year and optional TMDB keywords. Preview explains each estimate; reviewed corrections override it. No per-title tagging is required for these matches.
- Quality/gems use supported provider ratings/votes and exposure evidence; optional TMDB enrichment runs in background tasks.
- Awards require work-linked records with ceremony/category/outcome/source. One-night/weekend shelves can automatically use an ended TMDB miniseries only after reconciling every required local episode and duration. Strong-pilot and first-season opinion shelves remain optional reviewed selections.
- **Select shelves with enough matches** finds usable shelves for a chosen user, then save to apply them. Administrator settings include simple per-shelf Include/Exclude/Reset corrections, advanced imports, source inspection and previews. Users can hide/restore titles through **Discovery preferences** in the menu.
- Missing facts or too few candidates hide the affected shelf. Homepage callbacks do not call TMDB or an LLM. Netflix is never a runtime dependency.

## Build

```bash
python scripts/bootstrap-sdk.py
./scripts/build.sh
```

The bootstrap installs the pinned .NET 9.0.318 SDK under ignored `work/dotnet`, verifies its published SHA-512, and leaves system SDK packages untouched. You may set `DISCOVERY_DOTNET` to an existing compatible executable. The build restores exact NuGet lock files, compiles, runs tests and packages our assemblies. There is no HSS binary reference or bundled Jellyfin dependency.

## Verification

See [VERIFICATION.md](docs/VERIFICATION.md) for evidence, actual fixture sizes/timings and limits. Tests cover factual boundaries, user identity/access, imports and persistence, provider failures, deterministic allocation, browser rendering and native playback.

`dev/verify-server.py` and `dev/browser-check.cjs` target the disposable fixture only; see [development instructions](dev/README.md). Live TMDB account access was not used. Synthetic fixture facts are never shipped as seed records.

## Project map

| Path | Purpose |
|---|---|
| `src/DiscoveryRows.Core/` | Definitions, evidence validation, factual eligibility, scoring, deduplication and allocation. |
| `src/Jellyfin.Plugin.DiscoveryRows/` | Jellyfin/HSS adapters, DI, authenticated endpoints, dashboard/preferences, background tasks and durable JSON stores. |
| `tests/DiscoveryRows.Tests/` | Behavioral, identity, persistence and simulated-provider tests. |
| `samples/` | Non-factual evidence templates with inactive placeholder records. |
| `scripts/` | SDK bootstrap, locked build, package creation and repository manifest generation. |
| `docs/` | Installation, verification, research, original design and release scope. |
| `work/` | Ignored SDK, pinned research checkouts and disposable development fixture. |

The [original implementation plan](docs/IMPLEMENTATION_PLAN.md), [row catalogue](docs/ROW_CATALOGUE.md), [research](docs/RESEARCH.md) and [backlog](docs/BACKLOG.md) explain the design and future extensions. [Source lock](docs/research/source-lock.json) records research revisions. [Notices](NOTICE.md) cover dependencies and attribution; code uses GPL-3.0.

Automatic full-page row composition, optional model-based mood matching, translated shelf wording, native-client integrations and Jellyfin 12 support remain future work. They are not needed to use this release's stable Web shelves.

Release maintainers: see [PUBLICATION.md](docs/PUBLICATION.md) for updating the repository manifest and download together.
