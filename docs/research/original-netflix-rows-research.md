# Netflix discovery rows → one Jellyfin HSS plugin

Research date: 7 October 2026, Australia/Perth.

## Assessment and scope

This is a strong plugin idea. The useful feature is a collection of different reasons to watch something: time available, mood, recognition, story ingredients, and personal relevance. A small number of shared selectors can support a large catalogue of row definitions.

This investigation covers public Netflix browse collections, row headings visible within those collections, Netflix's own explanation of recommendation and curation, and the Home Screen Sections project's documented extension interface. Australian pages were preferred where available; some evidence comes from other regions. Public pages can redirect, change titles, fail to load, or show cached content. A collection's existence does not establish that Netflix currently shows it on every homepage, or in Australia.

There is no defensible claim to have found **every** Netflix row. Netflix says row choice, membership, and ordering can be personalized, and its curation team creates new collections. Its code catalogue is much larger than the useful set of discovery rows considered here. The catalogue below deliberately excludes most ordinary country/genre combinations.

**Evidence labels:**

- **V:** name verified on a Netflix public page or in Netflix's own documentation. May be a browse collection rather than an always-visible homepage row.
- **H:** name documented by Netflix, but the linked collection was unavailable, renamed, or redirected during this investigation. Historical/documented evidence, not confirmation of a currently usable page.
- **U:** supplied by you; exact wording not independently confirmed in accessible primary evidence.
- **C:** independently corroborated by a secondary source, with no accessible primary confirmation of the exact heading.
- **P:** proposed Jellyfin extension. Not presented as a Netflix row.

All membership rules, thresholds, rankings, difficulty assessments, and defaults below are **proposed Jellyfin implementations**, not Netflix's disclosed internal algorithms. Netflix does not publish exact selection rules for these headings.

## What Netflix actually documents

Netflix describes three separate decisions: which rows to show, which titles belong in them, and how to order those titles. Its inputs include viewing and rating history, similar viewers, title information, and viewing context. Recent interactions can outweigh older ones. [N1]

Its curation team also creates named collections around combinations of themes, seasonal ideas, and cultural events. An entertaining label is sometimes an editorial framing of a collection, rather than a precise genre or a single numeric filter. [N2]

Therefore, the plugin needs three layers: **eligibility**, **ranking inside a row**, and **selection of rows for the page**. Treating every row as a genre query sorted by rating will produce repetitive shelves with misleading labels.

## Your examples, translated into practical rules

| Row | Evidence | Proposed membership and ranking | Main limitation |
|---|---|---|---|
| Golden Globe Nominees | V [N3] | Verified work-linked nominations, including winners; configurable award years/categories. Rank eligible local titles by taste fit and quality. | An actor's unrelated nomination must not qualify all their films. |
| In a Hurry? Try These 30-Minute Hits | V [N4] | Series with an accessible next/start episode no longer than 30 minutes. For discovery, prefer consistently short regular episodes. | Series-level runtime can hide long pilots/finales. |
| Crowd pleasers | V [N5] | Broad-appeal editorial tag plus adequately supported audience approval; mild taste personalization. | Popularity, family suitability, and quality are separate properties. |
| Movies that make you laugh | U | Comedy intent/humour tags; comedy genre as a fallback. Rank by user taste, humour match, and supported quality. | Some dramedies are sad, some dark comedies are bleak. |
| Perfect Popcorn Movies | U | Accessible, entertaining, energetic films; boost action/adventure/heist/spectacle and playful pacing. | Does not mean merely highest-rated action films or highest budgets. |
| Hooked from the first episode | U | Series with an explicitly reviewed strong-opening/pilot tag. Pilot rating can support ranking where sufficiently voted. | General series ratings do not measure the opening; no need to fabricate a retention metric. |
| Just simply incredible movies | U | A demanding quality selector with adequate evidence, optional critical acclaim, and user relevance. | A tiny sample with a 9/10 average should not dominate established films. |
| Gems for You | C [N6] | Unwatched, strong taste fit, supported quality, and lower relative exposure. | A famous blockbuster the user hasn't watched is not necessarily a hidden gem. |

The four U headings are preserved because you observed them. Failure to retrieve them publicly does not show they are fictitious. Their exact Netflix membership and wording remain unverified here.

## Catalogue: time and commitment

**Data requirements:** L = local Jellyfin data; E = external enrichment; T = editorial/theme tags. These describe a dependable implementation, not whether a crude approximation is possible.

| Row / collection | Evidence | Suggested rule | Data |
|---|---|---|---|
| In a Hurry? Try These 30-Minute Hits | V [N4] | Accessible regular episode ≤30 minutes; prefer consistently short series. | L |
| 30-Minute Laughs | V [N4] | Short-episode comedy; humour intent increases precision. | L + optional T |
| 90-Minute Movies | V [N7] | Positive playable runtime ≤90 minutes. Configurable lower bound keeps shorts separate. | L |
| Tight on Time? Hit Movies Under 90 Minutes | V [N8] | Strict runtime <90 minutes plus a credible quality/appeal floor. | L + optional E |
| 90-Minute Comedies | V [N9] | Runtime ≤90 minutes and comedy membership. | L |
| 90-Minute Family Movies | V [N10] | Runtime ≤90 minutes, family intent, and the current user's permitted content. | L + T |
| 90-Minute Horror | V [N11] | Runtime ≤90 minutes and horror membership. | L |
| 90-Minute Rom-Coms | V [N12] | Runtime ≤90 minutes and romantic-comedy intent. | L + T |
| 90-Minute Thrillers | V [N13] | Runtime ≤90 minutes and thriller membership. | L |
| Short-Ass Movies | V [N14] | A broader short-film-night preset; choose and disclose a cutoff, e.g. ≤100 minutes. | L |
| Watch in One Night | V [N15] | Complete limited series or self-contained season with total required runtime ≤4 hours. | L + E/T |
| Watch in One Weekend | V [N16] | Complete limited series or self-contained season with total required runtime ≤12 hours. | L + E/T |
| Most Binge-worthy TV | V [N17] | Serial momentum, strong progression/cliffhangers, and user fit. | T + optional E |

The 4-hour, 12-hour, and 100-minute cutoffs are plugin design choices. They are not claims about Netflix's limits. Support strict versus approximate presets with titles that accurately reflect the chosen rule.

## Catalogue: quality and recognition

| Row / collection | Evidence | Suggested rule | Data |
|---|---|---|---|
| Golden Globe Nominees | V [N3] | Work-linked nomination records; do not confuse nomination with win. | E |
| Oscar Winners | V [N18] | Verified wins attached to the film or work-specific contribution. Allow award-category filtering. | E |
| Emmy-Winning TV Shows | V [N19] | Verified work-linked Emmy wins; configurable all categories versus major categories. | E |
| Emmy-winning TV Dramas | V [N20] | Emmy membership plus drama intent. | E + L |
| Emmy-winning TV Comedies | V [N20] | Emmy membership plus comedy intent. | E + L |
| Award-Winning Movies | V [N8] | Verified win from an explicit configurable award allowlist. | E |
| Award-Winning TV Shows | V [N15] | Same award allowlist with series/season records. | E |
| Critically Acclaimed Movies | V [N5] | Credible critic data or reviewed acclaim tags. Use a different heading if only audience ratings exist. | E/T |
| Venice Film Festival Award-winning Movies | V [N3] | Verified festival award records, category and year retained. | E |

A score sorted shelf and an awards shelf should look different. Recognition is a factual eligibility condition; personal taste can still determine the order within it.

## Catalogue: mood, accessibility, and personality

| Row / collection | Evidence | Suggested rule | Data |
|---|---|---|---|
| Crowd pleasers | V [N5] | Broad appeal and supported audience approval; avoid a harsh prestige-only threshold. | E + T |
| Casual Viewing | V [N15] | Episodic, approachable, low cognitive load. Sitcom/unscripted genres are only fallback clues. | T |
| Feel-good Movies | V [N5] | Warm/uplifting emotional experience with confidence above a threshold. | T |
| Emotional Movies | V [N3] | Strong emotional impact, including bittersweet and tragic films. Distinguish from feel-good. | T |
| Suspenseful Movies | V [N8] | Sustained tension; genre and keywords help but are incomplete. | L + T |
| Familiar Favorites | V [N9] | Broadly familiar classics/franchises or reviewed comfort favourites. | E/T |
| Oddballs & Outcasts | V [N15] | Outsider protagonists, eccentric ensembles, or social misfits. | T |
| Irreverent TV Comedies | V [N15] | Comedy with irreverent/satirical/offbeat tone. | L + T |
| Cringe Comedy | V [N4] | Social embarrassment and awkwardness as the comedy mechanism. | T |
| Goofy TV Shows | V [N4] | Silly/absurd comedic tone. | T |
| Romantic Favorites | V [N5] | Romantic focus plus established appeal; genre intersection alone is a fallback. | L + E/T |
| Get In on the Action | V [N8] | Energetic action-led content, weighted toward user taste. | L + T |

### Why “crowd pleasers” cannot be just high ratings

The public comedy page places accessible comedy, animation, and adventure together in this row. It includes titles whose reputation is not equivalent to critical prestige. That supports interpreting the label as broad entertainment appeal; it does **not** expose Netflix's selection formula. [N5]

## Catalogue: story ingredients and curated niches

| Row / collection | Evidence | Suggested rule | Data |
|---|---|---|---|
| Action with a Side of Romance | V [N21] | Action is primary; romance is a meaningful secondary ingredient. | T + L |
| Chosen Family | V [N22] | Found-family bonds are central, rather than any film containing friends. | T |
| Cyberpunk | V [N23] | Relevant setting/aesthetic/theme, supported by tags and keywords. | T + E |
| Dystopian Futures | V [N2] | Explicit future dystopia setting; exclude ordinary contemporary crime. | T + E |
| Don't Watch Hungry | V [N24] | Food/cooking/dining is a central attraction. Can mix films, documentaries, and series. | T + E |
| Family Movie Night | V [N2] | Family intent plus content permissions and age-rating configuration. | L + T |
| Female Friendship TV / Gal-Pal TV Shows | V [N25] | Female friendship is central. Public collection and Tudum wording differ. | T |
| High Brow Horror | V [N26] | Reviewed art/prestige horror membership. High scores alone are insufficient. | T + optional E |
| K-Dramas for Beginners | H [N2] | Curated accessible entry points, with modest commitment and a range of tones. Linked code redirected to NotFound. | T + E |
| Movies Directed By Women | V [N27] | Verified director metadata; allow manual overrides. Never infer identity from names. | E/T |
| Pop Culture Icons | V [N28] | Biographies/documentaries about culturally prominent people. | T + E |
| Reluctant Adults | V [N29] | Adult immaturity, arrested development, or resistance to adult responsibilities. | T |
| Relentless Crime Thrillers | H [N2] | Crime-led tension and sustained momentum. Documented name; linked page failed to load. | T + L |
| Schemers & Scammers / Up to No Good | V [N30] | Con artists, fraud, deception, and scams. Can mix documentary and fiction. Tudum uses the former name; the live public collection returned the latter. | T + E |
| Small Town Charm | V [N31] | Small-community setting plus warmth/comfort. The setting alone also matches horror. | T + E |
| Supernatural Soaps | V [N32] | Supernatural premise plus relationship-led serialized melodrama. | T + L |
| Swipe Right | V [N33] | Dating and relationship-seeking narratives, including unscripted formats. | T |
| Take a Trip Around the World | V [N34] | Travel/locations/cultural exploration are the attraction. Foreign origin alone is not enough. | T + E |
| Team Players | H [N2] | Team sport, competition, and sporting journeys. Documented; linked page failed to load. | T + E |
| Totally Awesome '80s | V [N35] | 1980s-set nostalgia, whether produced in that decade or later. | T + E |
| Nostalgic '90s | H [N2] | 1990s setting or explicit nostalgia. Linked code redirected to NotFound. | T + E |
| Gentle British Reality TV | V [N36] | British unscripted content with a gentle/low-conflict tone. | T + E |
| Witchcraft & the Dark Arts | V [N37] | Witchcraft/occult subject matter, across relevant genres. | T + E |
| Small Town Scares | V [N38] | Small-town setting plus horror or unsettling menace. | T + E |
| Twisted Christmas | V [N39] | Christmas setting plus dark, unconventional, or action/horror treatment. | T |
| Human Connections | V [N40] | Central interpersonal connection, warmth, belonging, or solidarity. | T |
| Spicy Romance | V [N41] | Romance with explicit sensual/sexual emphasis; always apply user's content restrictions. | T + L |
| Lavish Reality Lifestyles | V [N42] | Unscripted luxury, status, wealthy lifestyles, or aspirational property. | T + E |
| Six Degrees of Kevin Bacon | V [N43] | Reviewed actor-connection collection. A true graph-distance selector needs a sufficiently complete cast graph. | E/T |
| Like, Share, Follow | H [N2] | Internet/social-media-driven themes. Documented collection, but linked page failed to load. | T + E |
| Swipe Left | H [N2] | Dating/romance gone wrong. Documented collection, but linked page failed to load. | T |

## Catalogue: adaptations and real-life grounding

| Row / collection | Evidence | Suggested rule | Data |
|---|---|---|---|
| Movies Based on Books | V [N44] | Verified literary-adaptation relationship or curated adaptation tags. | E/T |
| TV Shows Based on Books | V [N45] | Same rule for television; display series, not individual episodes. | E/T |
| Movies Based on a True Story | V [N46] | Explicit dramatized true-story relationship; distinguish from documentaries. | E/T |
| True Crime | V [N47] | Real crime subject matter; retain documentary versus dramatization classification. | L + E/T |

## Additional Jellyfin ideas worth implementing

These are **P**, proposed extensions, not independently verified Netflix headings.

| Proposed row | Rule | Why it fits a personal media server |
|---|---|---|
| A Whole Story, One Sitting | Complete limited series/film-sized anthology arc within a configurable budget. | Avoids accidentally advertising a short first season of an enormous ongoing show. |
| Complete Series, Ready to Watch | Series has ended and all required aired episodes are locally accessible. | Useful reassurance for someone who dislikes starting incomplete stories. |
| Great First Seasons | Reviewed excellent opening season, locally complete, with supported season quality. | More achievable than measuring “instant hook” from aggregate scores. |
| Easy Watches for Tonight | Low cognitive load and short/medium commitment; personalize without presuming mood from clock time. | A concrete use case rather than another genre list. |
| Hidden in Your Library | Quality/taste candidates not recently recommended and not watched. | Can work without global popularity data if labelled honestly. |
| Freshly Added, Already Acclaimed | Recent local addition plus trustworthy acclaim. | Separates local arrival from original release date. |
| Big Screen Night | Spectacle tag and a suitable local video/audio version. | Uses the quality of the actual files owned. |
| The Next Chapter | Next available unwatched franchise entry after completed earlier instalments. | More useful than recommending sequel 4 to someone who has not seen 1–3. |
| More Like Your Recent Favourites | Recent positive preferences with theme/people/content similarity. | Adds value if it improves HSS's existing similarity rows. |
| Short, Sharp Documentaries | Standalone documentaries within a runtime budget and relevance threshold. | Runtime rules are dependable; tags improve topic fit. |
| Outside Your Usual Picks | Strong quality with controlled distance from the user's familiar tastes. | Introduces variety without random low-relevance picks. |
| Worth Another Watch | Previously enjoyed films with a long enough rewatch interval. | Different rules from unwatched discovery; explicit opt-in. |

## Implementation plan

### 1. Keep one plugin and make rows definitions

Recommended shared services:

1. **Library index:** movies, series, regular episodes, provider identifiers, runtimes, genres, tags, people, local versions, access permissions, and user progress.
2. **Metadata enrichment cache:** keywords, vote counts, external ratings with provider attribution, popularity, series status, complete season episode manifests, and curated recognition lists.
3. **Editorial classifier:** stable mood, theme, pacing, audience-intent, and commitment labels. Manual overrides take priority.
4. **User preference scorer:** favourites, completion, recent choices, and optional negative feedback.
5. **Row evaluator:** hard eligibility followed by row-specific ranking.
6. **Page planner:** chooses rows and allocates distinct candidates across them.
7. **HSS adapter:** registers sections and returns the requested user's local results.

Do not create separate networking and ranking implementations for every heading. Many shelves share a selector with different parameters or display text.

Netflix category codes are provenance/discovery aids, not Jellyfin query identifiers, TMDB genre IDs, or a runtime dependency. The plugin does not need a Netflix account or live Netflix scraping to operate.

### 2. Use factual metadata for factual claims

Local runtimes and availability are the best authority for what this user can play. TMDB documents keyword, discovery, movie/TV detail, season detail, and list endpoints; season detail can supply per-episode runtime and rating evidence. [T1–T6]

Do not assume a typical Jellyfin installation already stores TMDB vote counts, popularity, all keywords, detailed award records, or editorial mood. Enrich what is missing; keep provider and timestamp with every value. The exact local fields and loaded metadata must be checked against the target Jellyfin build during implementation.

TMDB distinguishes popularity from short-window trending. Neither should be represented as a direct universal audience satisfaction score, nor as Netflix's actual popularity/retention. [T7]

For awards, prefer a versioned curated mapping verified against award-organizer records. The Golden Globes has an official winners/nominees database. [A1] Store award body, ceremony year, category, outcome, work ID, and evidence URL. An external list is an ingestion format, not automatically an authoritative awards source. A generic awards-count string is insufficient to identify which body nominated which work.

Example awards scope options:

- Any work-linked nomination/win, including acting/music/technical contributions tied to this work.
- Major work awards only, such as best picture/series.
- Specific ceremony year, multiple years, or all time.
- Nominated including winners, nominees excluding winners, or winners only.

Movie release year and award ceremony year must remain separate. Festival titles and aliases need ID reconciliation. A later theatrical edition or alternate version should not become a separate recommendation merely because its file ID differs.

### 3. Add a compact editorial vocabulary

Suggested plugin-owned enrichment record, kept separate from library tags unless an administrator opts into writing tags:

```json
{
  "schemaVersion": 1,
  "mediaType": "series",
  "providerIds": { "tmdb": "<verified-id>" },
  "facets": {
    "tone": { "warm": 0.85, "dark": 0.15 },
    "pacing": { "fast": 0.75, "strongOpening": 0.9 },
    "themes": ["found_family", "small_town"],
    "structure": ["serialized", "limited_series"],
    "audienceIntent": ["popcorn", "comfort"]
  },
  "evidence": [
    { "source": "manual_review", "field": "pacing.strongOpening", "confidence": 0.9 }
  ],
  "classifierVersion": "1",
  "reviewedAt": "<timestamp>"
}
```

The confidence numbers illustrate a schema, not measured accuracy. Separate whether a tag is present from how confidently it was assigned. Keep unsupported properties unknown instead of false.

A local LLM can suggest labels offline using title metadata, descriptions, and trustworthy known information. It should not fabricate awards, runtime, completeness, episode ratings, or viewer retention. Full-video inference is unnecessary for an initial version. Strong-opening and emotional-outcome claims need curated evidence or explicit low-confidence treatment; a short synopsis often cannot establish either.

Cache by provider ID, metadata hash, and classifier version. Classify only changed/new items in a scheduled job. Permit manual exclusions and corrections. Never call an LLM synchronously while the homepage is waiting.

### 4. Give each row hard conditions and a distinct score

All candidates first pass: user authorization, permitted libraries/content, playable local availability, supported item type, valid required metadata, and the row's factual conditions.

After those gates, a configurable score can combine normalized components:

```text
score = a*tasteFit + b*qualityEvidence + c*rowFit + d*freshness
        + e*discoveryValue - f*recentExposure - g*franchiseRepetition
```

Different rows require different weights. Quality matters strongly for “incredible movies”; accessibility and row fit matter strongly for “popcorn”; limited exposure matters for “gems”. Runtime should gate a time promise, not merely contribute a small boost.

For audience quality, use a shrinkage estimate when a compatible rating and vote count are available:

```text
adjustedRating = (v / (v+m))*R + (m / (v+m))*C
```

R is the title's mean, v its vote count, C a relevant baseline for the same provider/media type, and m a configurable evidence-strength parameter. This is a proposed ranking method. Different rating providers/scales cannot be combined blindly. Missing votes should reduce confidence, not be invented or treated as proof of bad quality. Unrated media remains eligible for theme/time shelves where ratings are not required.

Start with modest content-based personalization. A household with a few users has too little data to mimic Netflix-scale collaborative filtering. Completion is useful but noisy: autoplay, children on a shared profile, and importing watched state can distort it. Avoid equating a single abandoned episode with a dislike.

### 5. Enforce runtime and completeness carefully

- Convert local runtime units correctly; reject zero/null values when duration is mandatory. Use the playable version's runtime, including extended editions.
- For short-episode rows, require the next playable episode to fit the budget. For discovery rows, also examine the distribution of regular episode lengths. Exclude specials from the typical runtime calculation.
- A median of 25 minutes does not justify showing a series with a 65-minute pilot under a literal 30-minute promise.
- For one-night/weekend rows, sum all required episodes for the promised unit. A limited series is preferable; an ordinary complete season needs a season-specific heading/card context.
- Verify required episodes against an external season/series manifest. A local-only episode count cannot reveal an absent finale.
- Count episodes the current user can actually access. Distinguish “whole story”, “whole season”, and “remaining unwatched episodes”.
- A canceled or ended show may have an unresolved story. Complete local files do not establish a satisfying narrative ending.
- Group alternate encodes and multi-episode files carefully; deduplicate logical episodes. Account for unaired episodes, specials policy, and metadata naming mismatches.

### 6. Plan the page as a whole

Suggested initial settings, to be tuned against the actual library:

| Setting | Starting point |
|---|---|
| Discovery rows shown | 6–10, in addition to normal continue/next-up rows |
| Titles per discovery row | 12–20 |
| Minimum credible candidates | 6; optionally lower for a deliberately small curated shelf |
| Eligibility rule | Hide a row that cannot meet its factual promise |
| Same logical title | Once across the first 6 discovery rows where feasible |
| Franchise cap | Usually 1–2 titles per row |
| Row rotation | Stable for a day or session, configurable |
| History handling | Exclude watched from discovery; rewatch shelves use their own policy |
| Empty/enrichment failure | Use a valid fallback shelf or hide; never silently relax factual membership |

Diversity is conditional, not a blanket ban on related items. A director/franchise shelf may intentionally contain related works. Deduplication must run before the minimum-size test; otherwise a row can look healthy until earlier shelves consume its candidates.

For row choice, favour strong candidates, distinct concepts, taste coverage, and rows that have not been shown repeatedly. Consider one time row, one quality/recognition row, two mood rows, one niche theme, and one exploration row. Keep controls for pinned shelves and explicit per-user disabling.

Static row titles should remain stable even as items rotate. Rotating shelves must not overwrite user's saved ordering or expose another user's candidates. A seed keyed by user, row, and time bucket gives controlled variation without re-shuffling on every request.

### 7. Integrate with the correct HSS project

The intended project is **IAmParadox27/jellyfin-plugin-home-sections**. Its README documents registration through reflection, using a payload with section ID, display text, limit, route, additional data, and result-handler assembly/class/method. The handler receives UserId and AdditionalData and returns QueryResult<BaseItemDto>. [H1]

Use stable IDs per independently selectable row. Make a shared handler dispatch by row definition. Cache by user, row/settings, library revision, and enrichment version. Check access again at delivery. Register idempotently once HSS is available. Match the installed Jellyfin/HSS versions and verify the exact callback contract in source before coding; this report does not claim compiled compatibility.

Treat whole-page automatic composition as an integration goal requiring source verification. Start with independently selectable stable shelves. Dynamic row ordering, changing headings, hiding empty shelves, and cross-row coordination may require an additional supported adapter or HSS changes; the registration example alone does not prove those capabilities.

### 8. Avoid duplicating existing HSS rows

HSS already describes similarity, rewatch, genre, studio, decade, people, hidden-gem, collection-completion, and discovery/request shelves. Its hidden-gem description uses high ratings and unwatched status, without an explicit obscurity condition. [H1]

Your plugin's most distinctive contribution is therefore mood, commitment, recognition, richer hidden-gem ranking, and page diversity. Let users disable overlapping built-in rows. Be precise with time and completeness before spending effort on another “starring actor” shelf.

The documented target is Jellyfin Web; independently implemented native clients need separate verification. [H1]

## Recommended release order

**First release:** short episodes, under-90-minute films, short comedies/thrillers, audience-quality films with honest labels, movies that make you laugh, popcorn, feel-good, casual viewing, crowd pleasers, gems for you, and a pilot-hook shelf backed by curated tags. Add one-night/weekend rows only after episode completeness checking is dependable. An offline profile without external APIs should show only the shelves it can support honestly.

**Second release:** verified Golden Globe/Oscar/Emmy shelves, award filters, fully populated one-night/weekend shelves, curated first-season picks, and clearer explanation of eligibility in the administrator interface.

**Third release:** richer niches such as found family, small-town charm, schemes/scams, art horror, gentle reality, and seasonal presets. Grow the catalogue without adding homepage clutter. Pick the rows the library can fill well.

My recommendation is roughly **12–15 strong definitions for the initial release**, with **6–10 visible at once**, and a larger optional preset catalogue. The list above should be treated as a menu of selectors, not a homepage containing every shelf.

## Validation that matters before release

1. A user with restricted libraries or age permissions never receives disallowed media, through either results or a cache populated by another user.
2. 89-, 90-, and 91-minute movie variants obey the advertised strict/inclusive boundaries; missing runtime never qualifies accidentally.
3. A 25-minute series with a 60-minute pilot fails a literal short-start condition. A missing finale fails “complete in one night”.
4. An actor's unrelated award does not qualify a work. Award year, release year, nominee, and winner remain distinct.
5. A film with a handful of high ratings cannot outrank broadly supported quality solely because of its average.
6. Cross-row deduplication still leaves credible shelves. Editions and encodes do not become multiple discoveries.
7. Provider/LLM failure serves a valid cached result or hides the affected shelf. Homepage requests do not initiate unbounded enrichment work.
8. HSS registers after restart without duplicate IDs, and existing user layout settings survive version updates.
9. New users get diverse sensible defaults; established users get distinct ordering. Genre, exposure, and history signals have honest meanings.
10. The administrator can inspect why an item qualified, correct a tag, exclude it, and see what metadata is missing.

## Sources

Netflix headings below establish observed names/collections only. Custom plugin rules above are independent recommendations.

- [N1] [Netflix: how recommendations work](https://help.netflix.com/en/node/100639).
- [N2] [Netflix Tudum: category codes and curation](https://www.netflix.com/tudum/articles/netflix-secret-codes-guide). Dated May 12, 2025; includes documented collections whose linked pages were unavailable during this investigation.
- [N3] [Golden Globe Nominees, code 81396373](https://www.netflix.com/browse/genre/81396373). Includes the emotional and Venice award shelf headings used above.
- [N4] [30-Minute Laughs, code 81602050](https://www.netflix.com/au/browse/genre/81602050). Includes the hurry, cringe, and goofy headings.
- [N5] [Comedy Movies, code 6548](https://www.netflix.com/au/browse/genre/6548). Includes crowd pleasers, feel-good, critical acclaim, and romantic favourites.
- [N6] [What's on Netflix: category codes and personalized collections](https://www.whats-on-netflix.com/what-to-watch/the-netflix-id-bible-every-category-on-netflix/). Secondary corroboration for Gems for You; not primary evidence of its membership algorithm.
- [N7] [90-Minute Movies, code 81466194](https://www.netflix.com/browse/genre/81466194).
- [N8] [Thrillers, code 8933](https://www.netflix.com/au/browse/genre/8933). Includes the tight-on-time, action, suspenseful, and award-winning headings.
- [N9] [90-Minute Comedies, code 81466224](https://www.netflix.com/au/browse/genre/81466224). Includes familiar favourites.
- [N10] [90-Minute Family Movies, code 81466229](https://www.netflix.com/au/browse/genre/81466229).
- [N11] [90-Minute Horror, code 81466239](https://www.netflix.com/au/browse/genre/81466239).
- [N12] [90-Minute Rom-Coms, code 81466228](https://www.netflix.com/browse/genre/81466228). Australian request redirected to New Zealand during retrieval.
- [N13] [90-Minute Thrillers, code 81466222](https://www.netflix.com/au/browse/genre/81466222).
- [N14] [Short-Ass Movies, code 81603903](https://www.netflix.com/browse/genre/81603903).
- [N15] [TV Sitcoms, code 3903](https://www.netflix.com/au/browse/genre/3903). Includes one-night/weekend, casual, irreverent, ensemble, oddballs, and award-winning headings.
- [N16] [Watch in One Weekend, code 3182735](https://www.netflix.com/browse/genre/3182735).
- [N17] [Most Binge-worthy TV, code 1191605](https://www.netflix.com/gb/browse/genre/1191605).
- [N18] [Oscar Winners, code 107234](https://www.netflix.com/browse/genre/107234).
- [N19] [Emmy-Winning TV Shows, code 90911](https://www.netflix.com/browse/genre/90911).
- [N20] [Netflix Emmy Collection, code 82900738](https://www.netflix.com/browse/genre/82900738).
- [N21] [Action with a Side of Romance, code 81647318](https://www.netflix.com/browse/genre/81647318).
- [N22] [Chosen Family, code 81231348](https://www.netflix.com/browse/genre/81231348).
- [N23] [Cyberpunk as a shelf on Thrillers](https://www.netflix.com/au/browse/genre/8933); also documented in [N2].
- [N24] [Don't Watch Hungry, code 3272152](https://www.netflix.com/browse/genre/3272152).
- [N25] [Female Friendship TV, code 1143288](https://www.netflix.com/browse/genre/1143288); called Gal-Pal TV Shows in [N2].
- [N26] [High Brow Horror, code 3261672](https://www.netflix.com/browse/genre/3261672).
- [N27] [Movies Directed By Women, code 2974953](https://www.netflix.com/browse/genre/2974953).
- [N28] [Pop Culture Icons, code 81278963](https://www.netflix.com/browse/genre/81278963).
- [N29] [Reluctant Adults, code 3053870](https://www.netflix.com/browse/genre/3053870).
- [N30] [Up to No Good, code 81493295](https://www.netflix.com/browse/genre/81493295); called Schemers & Scammers in [N2].
- [N31] [Small Town Charm, code 81615585](https://www.netflix.com/browse/genre/81615585).
- [N32] [Supernatural Soaps, code 81238162](https://www.netflix.com/browse/genre/81238162).
- [N33] [Swipe Right, code 81582488](https://www.netflix.com/browse/genre/81582488).
- [N34] [Take a Trip Around the World, code 81282911](https://www.netflix.com/browse/genre/81282911).
- [N35] [Totally Awesome '80s, code 2314106](https://www.netflix.com/browse/genre/2314106).
- [N36] [Gentle British Reality TV, code 81240711](https://www.netflix.com/browse/genre/81240711).
- [N37] [Witchcraft & the Dark Arts, code 81552046](https://www.netflix.com/browse/genre/81552046).
- [N38] [Small Town Scares, code 81496215](https://www.netflix.com/browse/genre/81496215).
- [N39] [Twisted Christmas, code 2300975](https://www.netflix.com/browse/genre/2300975).
- [N40] [Human Connections, code 81271205](https://www.netflix.com/browse/genre/81271205).
- [N41] [Spicy Romance, code 81572628](https://www.netflix.com/browse/genre/81572628).
- [N42] [Lavish Reality Lifestyles, code 81418611](https://www.netflix.com/browse/genre/81418611).
- [N43] [Six Degrees of Kevin Bacon, code 81614959](https://www.netflix.com/browse/genre/81614959).
- [N44] [Movies Based on Books, code 9889](https://www.netflix.com/browse/genre/9889).
- [N45] [TV Shows Based on Books as a shelf on Thriller TV](https://www.netflix.com/au/browse/genre/89811).
- [N46] [Movies Based on a True Story, code 920](https://www.netflix.com/browse/genre/920).
- [N47] [True Crime, code 108820](https://www.netflix.com/au/browse/genre/108820).
- [H1] [Home Screen Sections source and README](https://github.com/IAmParadox27/jellyfin-plugin-home-sections). Read current registration, built-in sections, and client integration documentation.
- [T1] [TMDB movie details](https://developer.themoviedb.org/reference/movie-details).
- [T2] [TMDB movie keywords](https://developer.themoviedb.org/reference/movie-keywords).
- [T3] [TMDB movie discovery](https://developer.themoviedb.org/reference/discover-movie).
- [T4] [TMDB TV discovery](https://developer.themoviedb.org/reference/discover-tv).
- [T5] [TMDB season details](https://developer.themoviedb.org/reference/tv-season-details).
- [T6] [TMDB list details](https://developer.themoviedb.org/v4/reference/list-details).
- [T7] [TMDB popularity versus trending](https://developer.themoviedb.org/docs/popularity-and-trending).
- [T8] [TMDB API FAQ and attribution requirements](https://developer.themoviedb.org/docs/faq). Follow current provider requirements when distributing an enrichment feature.
- [A1] [Golden Globes official awards database](https://goldenglobes.com/winners-nominees/).

No plugin code was compiled or installed as part of this research. No Netflix internal ranking weights, HSS binary compatibility, or client-wide support were experimentally verified.
