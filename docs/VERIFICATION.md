# Discovery Rows 1.0.3 verification

Built on **11 October 2026, Australia/Perth**, in `/home/diverse/Projects/jellyfin-plugin-discovery-rows`. This report distinguishes runtime checks from design assumptions.

## Version 1.0.3 automatic metadata

Automatic matching is on by default and uses local genres/tags/year/rating plus optional cached catalogue keywords and metadata. All 16 themes and five ordinary mood facets have explicit rules; ended-miniseries metadata can support one-night/weekend checks after full episode/runtime reconciliation. Unknown signals stay unknown. Awards and pilot/first-season opinions are not guessed. Reviewed negative/positive corrections override automatic estimates; concurrent provider updates preserve those corrections. Metadata estimates are reported as `automatic_metadata`, never persisted as reviewed labels.

There are 48 new automated cases on top of the 108 prior tests (156 total): positive and strict-mode cases, dark/mature family exclusions, exact terms rather than misleading substrings, precedence, missing/local/provider metadata, unknown facts, access/history gates, ended/canceled miniseries and missing episodes, movie/TV detail parsing, whole-library batching/backfill and atomic correction/provider persistence. Release builds produce zero warnings/errors.

The new live suite passes 20 assertions on the exact stack. A synthetic existing title receives ordinary Family/Comedy/Animation genres, a year and catalogue keywords with no reviewed facets; actual HSS callbacks return it in Family, Books, Witchcraft and '80s shelves. Explanation sources match, another library user cannot see it, corrections immediately override warm results, reset restores matching, strict mode suppresses unreviewed picks, a newly mature rating is honored during delivery revalidation, and all result/preview calls make zero provider requests. Fixture metadata, evidence and both plugin configurations are restored in `finally`. See [automatic-metadata-verification.json](automatic-metadata-verification.json).

The actual Web browser selects shelves with enough candidates (including previously disabled ones), omits an unsupported awards shelf, saves the selection, displays the automatic reason and renders Family Night. Its Include/Exclude/Reset controls change that shelf without JSON. No browser errors or provider calls occur. See [browser-automatic-verification.json](browser-automatic-verification.json). The prior startup, shared-HSS setup, bulk selection, 25 API assertions and native details/playback checks also pass on 1.0.3.

The published archive was fetched anonymously and checked against manifest MD5 and release SHA-256. A normal Catalogue upgrade from 1.0.2.0 loads 1.0.3.0, registers all shelves and retains preferences/configuration and evidence export; the stored evidence file remains byte-for-byte unchanged. Automatic mode defaults on and the new availability endpoint works. All 16 publication/upgrade assertions pass. See [upgrade-verification-103.json](upgrade-verification-103.json).

Provider details/batch behavior is verified using synthetic JSON and simulated HTTP errors; no real TMDB account token was supplied. A fixture proves implementation behavior, not precision/recall over the user's 459-title library. Mood/theme rules are estimates and coverage depends on metadata. The [matching guide](AUTOMATIC_MATCHING.md) states the inputs and limits.

## Version 1.0.2 one-place selection

The new administrator-only Setup action clones HSS's public typed configuration and saves it through HSS's public `UpdateConfiguration` method. It enables all 38 exact owned IDs, sets one instance each, and optionally disables per-user overrides so existing users receive the shared selection. No HSS private user file is edited. Global settings and other plugin sections are retained. Existing Discovery positions/styles remain; missing or placeholder-order entries receive unique positions. The action is explicit and repeatable; it does not continuously overwrite later HSS edits.

Five additional automated tests cover preservation, unchanged source input, unique new orders, idempotence, optional personal selection, exact ownership/duplicate handling and safe integer overflow. The live setup suite passed 17 assertions and verifies ordinary-user denial, all four fixture users receiving the 38 rows without a personal save, private settings file hashes staying identical, configuration/position/style preservation, personal-mode switching and minimal status output. After one restart, all 38 are present in HSS metadata; disabling and re-enabling a shelf solely in Discovery Rows changes the actual HSS callback on the same cached layout. See [homepage-setup-verification.json](homepage-setup-verification.json).

The browser setup suite clicks the actual setup button, selects all 38, clears the selection, saves and reloads it, and changes one checkbox to restore an actual homepage shelf. Bulk actions require an explicit save and preserve card limits/minimums/unstarted filters. The new controls produce no browser runtime errors. See [browser-setup-verification.json](browser-setup-verification.json).

The published 1.0.2 archive was fetched anonymously and checked against the manifest MD5 and release SHA-256. A normal Catalogue upgrade from 1.0.1.0 loaded 1.0.2.0, registered all 38 shelves and preserved shelf configuration, evidence and user selection. All 13 publication/upgrade checks pass. See [upgrade-verification-102.json](upgrade-verification-102.json).

HSS 3.0.2.0 retains old page metadata after configuration changes, so the first setup requires one restart. Later plugin checkbox changes work through the cached all-shelf layout with a Home refresh. Insufficient evidence/candidates still hide shelves. The shared option applies to all users who enable Modular Home; it does not enable Modular Home itself.

## Version 1.0.1 startup regression

Version 1.0.0 could delay HSS registration until the background worker observed core startup completion. The first homepage request could therefore cache a layout without Discovery Rows. The reproduced sequence returned zero discovery sections both before and after registration, while a fresh page hash returned the three selected discovery sections. Library preview could still show eligible/allocated cards.

Version 1.0.1 registers synchronously when the hosted service starts, before HTTP serving begins. The library index still waits for core startup, with a one-second startup retry interval. HSS's cache and private settings files are not modified. Status reports the actual assembly version and no longer repeats the instruction to save HSS settings regardless of whether they are already saved.

The fixture regression test polls immediately from a stopped server. Its first available HTTP request now reports 38 registered shelves; the first HSS layout includes the selected discovery shelf, the same cached layout retains it after indexing, and the actual HSS callback returns eight eligible cards. See [startup-verification.json](startup-verification.json). This test would fail on 1.0.0. The 103 automated tests, 25 live API assertions and real browser/native playback checks also pass on 1.0.1.

The published 1.0.1 archive was fetched anonymously and verified against MD5/SHA-256. A normal catalogue upgrade from 1.0.0.0 then loaded 1.0.1.0 after restart, registered 38 shelves and preserved the full shelf configuration, evidence export and user selection. Status reports 1.0.1. See [upgrade-verification-101.json](upgrade-verification-101.json) for the 13 checks.

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

**156 passing tests, zero failures/skips** at the recorded 1.0.3 run. Release builds use warnings-as-errors and produced zero warnings/errors.

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

## Hosted repository installation

The public [repository manifest](https://raw.githubusercontent.com/diverseau/jellyfin-plugin-discovery-rows/main/manifest.json) and [release archive](https://github.com/diverseau/jellyfin-plugin-discovery-rows/releases/tag/v1.0.0) were retrieved anonymously. Both the Jellyfin MD5 checksum and the release SHA-256 matched the published ZIP.

On the disposable Jellyfin 10.11.11 fixture, Discovery Rows was removed while stopped, the public repository was added through the same repository API used by the dashboard, and the Catalogue resolved version 1.0.0.0. The normal package installation endpoint downloaded and extracted the release with matching runtime-file hashes. After restart, Jellyfin reported the plugin Active, all 38 HSS shelves registered, labels initialized and the preferences page available. Nine checks passed; [repository-verification.json](repository-verification.json) records the results without credentials.

## Limits and future work

This release supports stable Jellyfin Web shelves. It does not automatically choose/reorder the whole page, coordinate unrelated HSS shelves, run a local classifier, claim Netflix algorithms, or certify independently implemented native clients. English shelf labels are seeded as safe fallbacks; translated wording and Jellyfin 12 builds remain future work.

Runtime promises remain conservative when a card offers several editions: every known selectable version must fit. Unknown or ambiguous metadata excludes factual shelves. Editorial and award truth still depends on administrators supplying reviewed evidence; the plugin verifies structure, provenance fields and work linkage rather than independently judging every imported claim.

The deployed production server/library was never accessed or altered. This is a published installable release with a documented exact-stack test fixture. Production installation remains the user's choice.
