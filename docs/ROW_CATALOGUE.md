# Row catalogue and selector contracts

Target: Jellyfin 10.11.11 / HSS 3.0.2.0. These are **proposed Jellyfin rules**, not Netflix's internal membership or ranking algorithms. Exact original evidence labels remain in the supplied research file. This catalogue intentionally uses accurate plugin headings when the available data cannot support a stronger label.

Every row applies the shared user-identity, access, availability, logical-deduplication and evidence rules in [Implementation plan](IMPLEMENTATION_PLAN.md). Default discovery policy excludes played movies and any series with regular-episode playback started. All runtime conditions inspect known selectable editions, compare exact ticks, and reject unknown values. Cards are ordinary Jellyfin movie/series DTOs.

## First 15 definitions

IDs in this table are frozen planning identifiers, beginning with letters so HSS can safely use them in CSS selectors. Key names used by the core are separate from user-facing text.

| # | Heading / HSS ID | Hard membership conditions | Evidence and ranking | Release |
|---:|---|---|---|---|
| 1 | Movies Under 90 Minutes / `DiscoveryRowsUnder90Movies` | Movie; every known selectable runtime >0 and **<90 minutes**. | Local data; time/genre profile; runtime is a gate, not a boost. | 0.1 |
| 2 | 90-Minute Comedies / `DiscoveryRows90MinuteComedies` | Movie; all runtimes <=90; mapped Comedy genre or reviewed humour intent. | Local genre mapping; reviewed humour improves row-fit precision. | 0.1 |
| 3 | 90-Minute Thrillers / `DiscoveryRows90MinuteThrillers` | Movie; all runtimes <=90; mapped Thriller genre or reviewed thriller intent. | Local data; time/genre profile. Do not treat every Crime film as a thriller. | 0.1 |
| 4 | Short Episodes, 30 Minutes or Less / `DiscoveryRowsShortEpisodes` | Series; true regular starting episode exists; every known accessible regular episode/version <=30; no started regular episode. | Local episode data; time profile. Missing pilot or ambiguous ordering excludes. No claim that the whole series is locally complete. | 0.1 |
| 5 | 30-Minute Laughs / `DiscoveryRows30MinuteLaughs` | Same short-series gate plus Comedy mapping or reviewed humour intent. | Local data; humour/episodic fit; no inferred sitcom mood. | 0.1 |
| 6 | Short Documentaries / `DiscoveryRowsShortDocumentaries` | Standalone documentary movie; every runtime <=60; local Documentary mapping or reviewed documentary classification. | Local data; topic/taste fit. The 60-minute budget is a plugin preset. | 0.1 |
| 7 | Movies That Make You Laugh / `DiscoveryRowsLaughMovies` | Movie with reviewed `humour` assertion >=.75. | Mood profile. If only Comedy genre exists, use a separately named “Comedy Picks” definition rather than relaxing this row. | 0.2 |
| 8 | Perfect Popcorn Movies / `DiscoveryRowsPopcornMovies` | Movie; reviewed `popcorn` intent >=.75; manual bleak/slow exclusions take precedence. | Accessibility/entertainment row fit, taste and light quality weighting. Action genre alone is insufficient. | 0.2 |
| 9 | Feel-Good Movies / `DiscoveryRowsFeelGoodMovies` | Movie; reviewed uplifting/warm intent >=.80; explicit conflicting emotional-outcome override excludes. | Mood profile; sadness and emotional intensity do not imply feel-good. | 0.2 |
| 10 | Casual Viewing / `DiscoveryRowsCasualViewing` | Series; reviewed low cognitive load >=.80 and episodic/approachable structure; accessible regular starting episode. | Mood profile. Genre shortcuts may suggest candidates for review only. | 0.2 |
| 11 | Crowd Pleasers / `DiscoveryRowsCrowdPleasers` | Movie; reviewed broad appeal >=.75 plus provider-paired audience evidence meeting a configurable modest floor. | Default TMDB >=500 votes and adjusted score >=6.5; mood profile with quality as support. Family suitability remains a separate restriction. | 0.2 |
| 12 | Highly Rated Movies / `DiscoveryRowsHighlyRatedMovies` | Movie; supported same-provider rating+votes; configured quality floor. | Default TMDB >=100 votes and adjusted score >=7.0; quality profile. Do not call this “Critically Acclaimed” without critic evidence. | 0.2 |
| 13 | Hidden in Your Library / `DiscoveryRowsHiddenLibrary` | Movie or series; unwatched/unstarted; not delivered in prior buckets within the last 7 days; valid row/taste relevance. | Local discovery profile. Cold users use deterministic diverse picks; no global-obscurity claim. | 0.2 |
| 14 | Gems for You / `DiscoveryRowsGemsForYou` | Movie; supported quality; supported low relative exposure; no recent recommendation delivery in prior buckets. | Default quality floor as #12; exposure below the 40th percentile of user-accessible indexed valid TMDB movie popularity values when at least 50 observations exist. Taste/discovery profile; thresholds require tuning. | 0.2 |
| 15 | Hooked from the First Episode / `DiscoveryRowsStrongOpening` | Series; reviewed `strong_opening` >=.85 tied to the correct pilot/start; starting episode locally accessible; no started regular episode. | Mood profile; provider pilot rating can support rank, not substitute for reviewed evidence. | 0.2 |

Confidence thresholds are design settings. A value of .8 in an imported classifier record is not evidence of measured 80% accuracy; reviewed source and provenance are required separately. No fabricated fixture score or seed title should ship as factual evidence.

Optional, non-quality row ratings may be absent. Quality rows need their specified evidence. The explanatory interface reports a missing field distinctly from a failed threshold.

“Hidden in Your Library” and “Gems for You” overlap by design but have different promises. Recommend enabling one by default; when both are selected, the optional cross-row allocator should reserve the strongest externally supported gems and fill the local shelf with alternatives.

## Suggested initial setup

Offline installation: enable under-90 movies, one of short comedies/thrillers, short episodes, 30-minute laughs, and short documentaries where six candidates exist. After editorial/quality data is present, mix one time row, one quality/discovery row, two moods, a short-series row, and a niche row when implemented. Rows remain independently selectable; HSS settings determine actual order.

Do not show all 15 just because they are registered. Default 16 cards, minimum six, maximum 40; apply minimum after deduplication and current access checks. Administrator can explicitly allow a small curated shelf. A row with missing required evidence returns empty and its dashboard coverage explains why.

## Release 0.3: factual recognition and whole-story units

| Definition | Conditions and scope |
|---|---|
| Golden Globe Nominees | Verified nomination records tied to local work IDs; configurable ceremony years/categories; winners included unless explicitly excluded. |
| Oscar Winners | Verified Academy win with work-linked contribution; category allowlist; no actor-career inference. |
| Emmy-Winning TV | Verified series/season contribution; keep series-card eligibility distinct from awards to a single episode where policy excludes those. |
| Watch in One Night | Reviewed complete limited story; reconciled full manifest; every required episode locally accessible; deduplicated total runtime <=4 hours. |
| Watch in One Weekend | Same contract with <=12-hour budget. |
| Complete Series, Ready to Watch | Verified ended manifest with all aired required episodes locally accessible; label promises local completeness, not a resolved ending. |
| Great First Seasons | Reviewed opening-season quality; complete reconciled local season; require a season card or contextual unit that HSS can render accurately. |

Recognition rows require a validated ingestion source and provenance; they cannot ship based on a generic “awards” string. Whole-story shelves require strong reconciliation and unit-specific cards before they can make a time promise.

## Later preset catalogue

Use shared typed selectors with new frozen IDs and display labels. The original document supplies research headings and limitations for the following groups:

| Selector family | Optional presets | Additional requirement |
|---|---|---|
| Runtime + intent | 90-minute horror, family, rom-coms; broader short-night movies | Family and rom-com intent need tags; user permissions always apply. Literal limits must match labels. |
| Recognition | Emmy drama/comedy, festival awards, critically acclaimed | Body/category-specific work records or credible critic evidence. |
| Tone/experience | Suspense, irreverence, cringe, goofy, emotional, familiar comfort | Reviewed facets with distinct meanings. |
| Story/theme | Chosen family, cyberpunk, future dystopia, food, friendship, scams, small-town charm/scares, occult, supernatural soaps | Theme presence plus centrality/importance; setting alone is often inadequate. |
| Audience entry points | Beginner K-dramas, gentle British reality, family movie night | Reviewed accessibility/tone and appropriate audience intent. |
| Adaptation/grounding | Based on books, dramatized true stories, true crime | Verified relationship/classification; distinguish fiction and documentary. |
| Seasonal/cultural | Twisted Christmas, eighties/nineties nostalgia, travel, pop culture icons | Content theme/setting rather than release country or production year alone. |
| Personal-server features | Next franchise chapter, fresh local arrival + acclaim, big-screen night, outside usual taste, rewatch | Franchise order, arrival timestamps, actual file properties, or opt-in rewatch policy. |

Do not infer identity attributes from names for “directed by women”; support voluntarily sourced verified metadata/manual records if that preset is added. Actor graph distance needs a reliable cast graph; keep it out of early releases.

The preset library can grow independently of homepage density. Measure usable candidates, false positives, overlapping results and administrator correction effort before adding a preset to the recommended set.
