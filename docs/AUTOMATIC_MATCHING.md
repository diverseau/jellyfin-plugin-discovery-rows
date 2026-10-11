# Automatic discovery in 1.0.3

Automatic metadata matching replaces the requirement to hand-label a whole library. It is on by default, including when loading older configurations. The original manual-first research/design documents are historical; the current user request explicitly prioritizes useful automatic shelves.

Use **Shelves → Select shelves with enough matches**, then save. This checks all shelves for the selected user's accessible titles using the saved eligibility settings. It does not reduce minimums, ignore watched/access exclusions, change HSS ordering or change the library metadata. Candidate counts are independent; coordinating overlapping shelves can still suppress an underfilled row.

## Inputs and precedence

Local Jellyfin genres, existing tags, year and official rating work offline. Optional TMDB enrichment caches keywords, genres, release/first-air year, countries, series type/status, paired rating/votes, popularity and episode manifests. Local genres/year take priority when present; provider data fills missing fields. Matching uses exact normalized keyword/tag terms, not loose substring searches of a synopsis. A one-time read access token enables whole-library enrichment; missing/stale items are processed in batches and the scheduled task maintains them.

[TMDB movie details](https://developer.themoviedb.org/reference/movie-details), [movie keywords](https://developer.themoviedb.org/reference/movie-keywords), [series details](https://developer.themoviedb.org/reference/tv-series-details), [append-to-response](https://developer.themoviedb.org/docs/append-to-response).

Reviewed per-facet corrections take priority. A reviewed negative label suppresses that facet even if metadata matches; a reviewed positive label uses its recorded confidence. **Reset to automatic** removes that particular facet override. Automatic matches never set `Reviewed=true`, never change NFOs/tags and never fabricate award records. Their score contribution is a ranking weight, not a calibrated probability.

## Main rules

| Shelf | Automatic signal |
|---|---|
| Movies That Make You Laugh | Comedy genre or explicit humour/stand-up/slapstick keyword/tag |
| Perfect Popcorn Movies | Action, Adventure or Science Fiction, or explicit blockbuster/popcorn keyword/tag |
| Feel-Good Movies | Explicit uplifting/feel-good/heartwarming/inspirational term, or Comedy with Family/Romance/Animation; dark genres and dark/tragedy keywords excluded |
| Casual Viewing | Sitcom/episodic/sketch-comedy terms, or short regular Comedy episodes; accessible starting episode required |
| Crowd Pleasers | Mainstream genre or explicit broad-appeal/blockbuster term, plus the existing supported rating/vote floor |
| Family Movie Night | Movies with Family genre and no dark genre/known mature rating; alternatively Animation with an all-ages/PG rating or an explicit family-film term |
| Totally Awesome '80s | Local/provider release year 1980–1989, or explicit 1980s-nostalgia term |
| Watch in One Night / Weekend | Ended TMDB Miniseries, fresh complete aired-order manifest, every required episode locally accessible and total runtime within the limit |

Family Night is a discovery filter, not a universal age-suitability certification. Jellyfin's actual account access and parental policies remain authoritative. An ended miniseries check establishes airing/file completeness; it does not guarantee a satisfying ending.

## Theme rules

The complete versioned term lists are in `src/DiscoveryRows.Core/AutomaticMetadataMatcher.cs`.

| Theme | Examples of required metadata |
|---|---|
| Chosen Family | found/chosen/surrogate family |
| Cyberpunk | cyberpunk, or Science Fiction plus cyberspace/virtual-reality/hacker/cybernetics |
| Dystopian Futures | dystopia/dystopian-future/post-apocalyptic |
| Don't Watch Hungry | chef/cooking/baking/restaurant/cuisine |
| Small Town Charm | small-town/village term with light Comedy/Romance/Family genres |
| Small Town Scares | small-town/village term with Horror/Thriller |
| Schemers & Scammers | con-artist/scam/fraud/swindler terms |
| High Brow Horror | Horror plus psychological-horror/arthouse/surrealism/elevated-horror/slow-burn |
| Gentle British Reality TV | GB/UK production/origin country, Reality genre and baking/gardening/crafts/cooking-competition term |
| Witchcraft & the Dark Arts | witchcraft/witch/witches/black-magic/occult |
| Based on Books | explicit based-on-novel/book/children's-book/graphic-novel/literary-adaptation term |
| Based on a True Story | explicit true-story adaptation term; documentaries excluded |
| Family Movie Night | Family/animation/rating rules above |
| Totally Awesome '80s | year/nostalgia rules above |
| Twisted Christmas | Christmas plus dark genre or dark-comedy term |
| Cringe Comedy | Comedy plus cringe/social-awkwardness/humiliation/mockumentary term |

These are explainable estimates. Keywords may be incomplete or describe an incidental element; the rules do not establish emotional outcomes or artistic quality. Preview shows the specific matched input. Correct only the exceptions you care about using simple shelf Include/Exclude/Reset controls, or hide a title personally through Discovery preferences.

## Remaining factual/curated shelves

Awards still need actual work-linked award records. Strong-opening and first-season opinion shelves remain optional reviewed selections; a series-wide rating does not prove either. The matching-selection button can leave unsupported shelves off. Unknown metadata produces no match; it does not cause a fallback that puts every title in every shelf.

No LLM, paid inference service or per-title review queue is required. Live TMDB account enrichment and real-library classification accuracy are not claimed as tested; synthetic inputs and provider-response fixtures verify the behavior and boundaries.
