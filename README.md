# Jellyfin Discovery Rows

An installable server plugin that adds **38 discovery shelves** to **Home Screen Sections 3.0.2.0** on **Jellyfin 10.11.11**. Shelves offer different reasons to watch your local media: time available, mood, quality, recognition, complete stories and themes.

Version **1.0.0**, built and verified on **11 October 2026, Australia/Perth**. The authoritative project folder is `/home/diverse/Projects/jellyfin-plugin-discovery-rows`.

## Install and use

Follow [INSTALL.md](docs/INSTALL.md). The built package is `artifacts/DiscoveryRows-1.0.0.0-jellyfin-10.11.11.zip`, with hashes and metadata alongside it. Install HSS, File Transformation and Plugin Pages separately. This build has not been deployed to a production server or published remotely.

The first 15 shelves are available by default; seven award/completeness shelves and sixteen themes are optional. HSS owns homepage selection/order. Discovery Rows owns eligibility, scores, explanations and coordination among its own selected shelves.

- Runtime/genre shelves work offline without credentials.
- Mood shelves require reviewed manual/imported facets, rather than silently assuming a genre implies a mood.
- Quality/gems use supported provider ratings/votes and exposure evidence; optional TMDB enrichment runs in background tasks.
- Awards require work-linked records with ceremony/category/outcome/source. Whole-story rows require fresh reconciled episode manifests and reviewed narrative completeness.
- Administrator settings include evidence editing/import/export, source inspection, exclusions and previews. Users can hide/restore titles through **Discovery preferences** in the menu.
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
| `scripts/` | SDK bootstrap, locked build and local package creation. |
| `docs/` | Installation, verification, research, original design and release scope. |
| `work/` | Ignored SDK, pinned research checkouts and disposable development fixture. |

The [original implementation plan](docs/IMPLEMENTATION_PLAN.md), [row catalogue](docs/ROW_CATALOGUE.md), [research](docs/RESEARCH.md) and [backlog](docs/BACKLOG.md) explain the design and future extensions. [Source lock](docs/research/source-lock.json) records research revisions. [Notices](NOTICE.md) cover dependencies and attribution; code uses GPL-3.0.

Automatic full-page row composition, local-model label suggestions, translated shelf wording, native-client integrations and Jellyfin 12 support remain future work. They are not needed to use this release's stable Web shelves.
