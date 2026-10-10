# Build backlog

Execute in milestone order for **Jellyfin 10.11.11 / HSS 3.0.2.0**. This is the original build sequence. Checked tasks were implemented and verified for version 1.0.0; remaining items are broader scale checks or future extensions. See VERIFICATION.md for concrete evidence and limits.

## M0 — Establish the exact integration

- [x] B00: Create solution and plugin from the official template, replacing its IDs/names/package versions; fix target `net9.0` and Jellyfin packages 10.11.11 with runtime exclusions. Establish GPL-3.0 licensing and freeze a new plugin GUID.
- [x] B01: Use project-local .NET 9 SDK or SDK container; pin an available patch after checking the official feed. The current host has no SDK. Add reproducible build commands without changing system runtime packages.
- [x] B02: Create disposable Jellyfin server/Web 10.11.11 fixture; obtain exact HSS 3.0.2.0 and companion 3.0.1.0 artifacts from the captured manifest. Record SHA-256 values, check publisher checksums where supplied, and pin container image digest. Use synthetic or explicitly chosen fixture media, no production mounts.
- [x] B03: Register one CSS-safe test row through the actual HSS JObject type; prove public handler DI and direct `QueryResult<BaseItemDto>` return. Exercise startup ordering, repeat registration, absent HSS and unsupported capability behavior.
- [x] B04: Implement authenticated-context equality guard before all data access; prove two disjoint users and forged `userId` requests cannot expose IDs, names or cached recommendations. Test missing user/context and API-key requests.
- [x] B05: Implement capability-checked label bridge to seed distinct row names into HSS's in-memory translation packs; verify admin/user selectors, English fallback, and homepage headings without private-field patches.
- [x] B06: Confirm HSS admin-save onboarding, preserved layout after restart, CSS selectors, empty-row hiding, portrait/default display, details/navigation and normal playback.

**Exit:** one reliable shelf on the exact stack, distinct selectable name, guarded identity, and restart persistence. If reflected DI/context or labels fail, resolve the adapter before developing recommendations. Document the tested boundary rather than claiming source inspection proved compatibility.

## M1 — Ship useful local-only rows (0.1)

- [x] B10: Build normalized immutable local snapshot through supported Jellyfin library APIs, covering permissions, mixed libraries, movies, series, regular episodes, alternate versions, missing items and provider IDs.
- [x] B11: Implement runtime and episode-start eligibility; compare exact ticks and enforce all selectable versions. Add under-90 movies and 30-minute laughs first.
- [x] B12: Extend the shared selectors to the six 0.1 rows in the catalogue. Add configured genre mapping and logical identity deduplication.
- [x] B13: Implement bounded per-user candidate cache, fresh final access/watched checks, DTO generation and revision-based invalidation. Coalesce events; add scheduled reconciliation and cancellation.
- [x] B14: Add minimal administrator status/settings, candidate coverage, disabled-row reasons, manual exclusions and an explanation preview. Keep secrets and other users' results out of normal-user endpoints.
- [x] B15: Run behavioral fixtures and exact-stack browser checks; package a development release with manual installation instructions and exact dependency matrix.

**Exit:** no API key required; 89:59, 90:00 and 90:01 behave correctly; mixed editions do not break time promises; missing/long pilots cannot qualify; no cross-user cache leakage. Empty/insufficient shelves hide and explain their cause in diagnostics.

## M2 — Add editorial evidence and quality (0.2)

- [ ] B20: Finalize evidence-store packaging behind its interface. Add versioned migrations, import validation, manual-over-provider precedence, conflict reporting and transactional snapshot updates.
- [x] B21: Add administrator facet import/editor for humour, feel-good, popcorn, low cognitive load, broad appeal and strong opening. Require review provenance; measure actual library coverage.
- [x] B22: Add optional TMDB detail/keyword enrichment with provider attribution, API-token set/clear handling, bounded queue/rate/timeout/retry controls and incremental jobs.
- [x] B23: Add same-provider rating/vote pairing and configurable shrinkage, modest taste scoring, cold-user behavior, recommendation-delivery history and deterministic time buckets.
- [x] B24: Complete the remaining nine rows, distinguishing local hidden-library suggestions from evidence-backed gems. Add coverage/explanation UI and metadata-source attribution.
- [x] B25: Add optional read-only effective-HSS-layout adapter and deterministic allocation among our enabled rows. If unavailable, retain independent row operation and report the limitation; never deduplicate by callback arrival order.
- [ ] B26: Exercise outages, invalid tokens, stale/manual evidence, small rating samples, cache bounds, parallel callbacks and policy changes. Measure warm/cold latency on documented 10k-title fixtures and review mood false positives.

**Exit:** 15 stable definitions are selectable, unsupported rows remain empty, mood evidence is inspectable/correctable, and homepage callbacks make zero provider/classifier calls. Suggested handler targets: warm p95 <=250 ms and local cold p95 <=1 second; report measured hardware and full-home latency separately.

## M3 — Add recognition and completeness (0.3)

- [x] B30: Define and validate work-linked award import format and category/year/outcome filters. Start with manually verified records; decide distributable seed data separately.
- [x] B31: Implement series/season manifest ingestion and order reconciliation, multi-episode-file coverage/runtime, specials policy, accessibility and freshness constraints.
- [x] B32: Add Golden Globe/Oscar/Emmy rows plus complete limited-story one-night/weekend rows. Add complete-series/first-season presets only if their card/unit context works accurately.
- [x] B33: Test unrelated actor awards, nominee/winner ambiguity, ceremony/release-year confusion, missing finales, inaccessible episodes, unresolved canceled stories and ambiguous episode orders.

**Exit:** factual shelves can show evidence for every qualifying work; no incomplete or ambiguous unit satisfies a whole-story/time promise.

## M4 — Grow optional presets and prepare distribution

- [x] B40: Add high-coverage theme presets using the shared selectors. Keep homepage density manageable and document overlap with the actually installed HSS version.
- [ ] B41: If useful, add optional background local-model label suggestions with structured validation and human review. Do not generate factual awards/runtime/completeness evidence.
- [ ] B42: Add localized resources and self-service per-user hide/feedback controls, using the authenticated identity and preserving HSS layout settings.
- [x] B43: Publishable package preparation: exact ABI manifest, private dependency audit, checksums, changelog, install/upgrade/uninstall instructions, migrated-data backup and clean restarts. Public publishing is a later explicit action.
- [ ] B44: Investigate automatic page composition with a supported upstream extension/factory interface; prepare a separate HSS change if necessary. Native clients and Jellyfin 12.x require separate compatibility milestones.

**Exit:** supported client/version matrix reflects runtime tests, not assumptions; rows remain honest under missing metadata; users can understand why items appear.

## Verification fixtures to keep

| Fixture | Expected outcome |
|---|---|
| User A only library A; user B only B; both caches warmed | No IDs or titles from the other library, including forged HSS query IDs. |
| Child profile with blocked tag/unrated/age content | Excluded on query and final delivery, also after same-session policy changes. |
| 89:59 / 90:00 / 90:01 movies | Strict under-90 includes only first; inclusive 90-minute presets include first two. |
| 88-minute and 105-minute alternate editions | Excluded from strict time row under ordinary HSS playback. |
| Null/zero/negative runtime | Excluded whenever duration is required. |
| 25-minute series with a 60-minute pilot or missing pilot | Excluded from literal short-start row. |
| 9.8 rating / 5 votes versus supported quality | Tiny sample cannot satisfy quality floor merely through its mean. |
| Local rating and unrelated provider vote count | Never paired into a composite score. |
| Earlier row consumes another row's candidates | Reallocate/suppress after final minimum test; no callback-order dependence. |
| Finale missing, double-episode file, inaccessible special | Reconcile actual required coverage and avoid double-counting file duration. |
| Imported award attached to person but not work | Does not qualify any work until a verified work link is present. |
| Provider unavailable, index warming or HSS absent | Valid local/cached results or empty row with diagnostics; no unbounded homepage work. |
| Update and restart | Frozen IDs and user layout remain; no duplicate section registrations. |

## Completion record

When building, append milestone evidence with source/package versions, fixture commands, screenshots where useful, actual timings, remaining limitations, and artifact checksums. Do not mark a milestone finished until its exit criteria have been demonstrated.

## Version 1.0.0 completion notes

The delivered implementation uses atomic versioned JSON stores instead of SQLite; B20 remains open for a future storage-migration workflow, while validated imports, corrupt-file backups and manual precedence are implemented. B26's failure/concurrency/policy checks passed on the 20-title fixture; a 10k-title end-to-end benchmark and real-library editorial QA remain open. B42's self-service feedback is implemented; translated shelf wording remains future work. Local-model suggestions, automatic full-page composition, native-client integration and Jellyfin 12 builds were deliberately left outside this release.
