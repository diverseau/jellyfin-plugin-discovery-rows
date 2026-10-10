# Discovery Rows 1.0.0 verification

Built on **11 October 2026, Australia/Perth**, in `/home/diverse/Projects/jellyfin-plugin-discovery-rows`. This report distinguishes runtime checks from design assumptions.

## Shipped scope

- All 15 initial discovery definitions, three award rows, four series-unit rows and 16 optional theme presets: **38 shelves**.
- A shared typed selector/ranker, precise runtime rules, provider-paired quality evidence, deterministic ordering and logical deduplication.
- Guarded HSS callbacks, fresh delivery checks, user-specific candidate caches, permission/history invalidation and optional effective-layout allocation.
- Plugin-owned durable JSON evidence/history/credential stores with validation, atomic writes, corrupt-file preservation and recovery backups. No direct SQL against Jellyfin and no metadata/NFO/tag writes.
- Administrator configuration, source editing/import/export, exclusions and coverage/score previews; authenticated per-user hide/restore with a Plugin Pages menu entry.
- Optional TMDB background enrichment, cancellation, bounded rate/retry/timeout behavior, attribution and retention of manual evidence.

The JSON store uses the explicitly allowed small-store fallback from the design, avoiding SQLite/native load-context dependencies. Schema 1 is validated and preserved; unsupported schemas are rejected. A later storage migration is not needed to use this release.

## Exact stack

| Component | Tested version |
|---|---|
| Jellyfin server/Web | 10.11.11 official container |
| Home Screen Sections | 3.0.2.0 |
| File Transformation | 3.0.1.0 |
| Plugin Pages | 3.0.1.0 |
| Build SDK | .NET 9.0.318, official published SHA-512 verified |
| Browser test runner | Playwright Core 1.64.0, system Chromium |

The container and artifact locks are in [build-lock.json](build-lock.json). HSS/companion binaries are external dependencies and are not included in the package.

## Automated tests

**103 passing tests, zero failures/skips** at the recorded run. Release builds use warnings-as-errors and produced zero warnings/errors.

Behavioral cases include strict/inclusive minute boundaries down to a single tick; null/zero runtime; 88/105-minute editions; missing/inaccessible/long pilots; special episodes; reviewed versus unknown/negative facets; tiny/missing vote samples; rating scales and staleness; discovery exposure; exact work-linked award body/outcome/ceremony filters; missing finales; inaccessible required episodes; double-episode files; stale/unknown/unaired manifests; conflicting episode IDs; and real season-card requirements.

Additional cases cover authenticated claim equality, malformed/missing/API-key identities, real JObject construction, matching synchronous wrappers for every CSS-safe ID, import schemas and conflicting records, configuration bounds, deterministic/scarcity-aware allocation, franchise caps, atomic all-or-nothing imports, reload/preservation of manual evidence, corrupt-file repair, separate private credentials, immutable published snapshots, per-user feedback and prior-bucket delivery history.

Provider tests use a simulated HTTP handler: 401 stops without retries and does not disclose the credential; 429 retries within a budget and preserves successful JSON; repeated 503 stops after three attempts; 404 does not retry; cancellation stops outstanding work. No real TMDB account credential was supplied, so real-account enrichment and a broad provider corpus are **not** claimed as experimentally verified.

## Live server checks

The disposable fixture contains **20 movie/series titles** (13 movies, seven series; fourteen regular episodes), with actual tiny local H.264 files and local NFOs. A/B libraries are separate; a child profile adds tag and parental restrictions. Fixtures carry explicitly synthetic facts, retained only under ignored `work/`.

The API suite passed **25 assertions**: all 38 shelves registered; unique readable settings labels; correct A/B/child access and stable order; blocked tag/rating enforcement; forged/mismatched HSS IDs rejected for ordinary users and administrators; `AdditionalData` cannot change the row; administrator endpoints reject ordinary users; invalid schemas fail; imported moods/awards/completeness produce valid rows; wrong award bodies do not; first-season rows return Season DTOs; own hide/restore leaves other users unchanged; policy changes invalidate warm results; parallel callbacks preserve ordering; and result requests perform zero provider calls.

On this 20-title fixture, 25 warm HTTP result calls measured approximately **14 ms median and 17 ms p95** in the first recorded performance run. This includes local HTTP/controller overhead, not full-page Web rendering. These numbers do not establish performance on a 10,000-title production library; the larger-library handler targets in the design remain tuning goals.

## Browser and lifecycle checks

Headless Chromium exercised the actual transformed Jellyfin Web bundle:

- Discovery rows render on the homepage and a deliberately underfilled shelf stays hidden.
- The dashboard shows all 38 controls and coverage/score previews.
- Preferences load and search titles; a normal user navigates through Plugin Pages, hides a title and restores it.
- Ordinary title cards navigate to native details and start **native video playback** (video element has loaded data and a native stream request is observed).
- No browser runtime errors or TMDB requests occurred during those flows.
- Restarting the fixture preserves plugin configuration, imported evidence, HSS layout, user policies and stable registration IDs without duplicates. Package-only clean-install and HSS-absent recovery are checked separately before final packaging.

Additional live checks confirm that disabling a shelf is honored even with old HSS page metadata, coordinated overlapping movie shelves are disjoint, and installing the missing HSS dependencies followed by restart activates registration/labels/preferences successfully. HSS 3.0.2.0's first per-user save was observed not to persist on a fresh settings file; saving again succeeded. The install guide documents that dependency workaround without altering HSS code or private settings files.

Screenshots and raw compact reports were captured under ignored `work/screenshots/` and `work/*verification.json`. A review copy of the main screenshots/report accompanies the final artifact outputs.

## Limits and future work

This release supports stable Jellyfin Web shelves. It does not automatically choose/reorder the whole page, coordinate unrelated HSS shelves, run a local classifier, claim Netflix algorithms, or certify independently implemented native clients. English shelf labels are seeded as safe fallbacks; translated wording and Jellyfin 12 builds remain future work.

Runtime promises remain conservative when a card offers several editions: every known selectable version must fit. Unknown or ambiguous metadata excludes factual shelves. Editorial and award truth still depends on administrators supplying reviewed evidence; the plugin verifies structure, provenance fields and work linkage rather than independently judging every imported claim.

The deployed production server/library was never accessed or altered. This is an installable local release with a documented exact-stack test fixture, not an automatic production deployment or public release publication.
