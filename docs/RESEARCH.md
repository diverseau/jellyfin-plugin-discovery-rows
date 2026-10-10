# Research findings

Research date: **11 October 2026, Australia/Perth**. Target supplied by the owner: **Jellyfin 10.11.11 / HSS 3.0.2.0**. Findings below were recorded from primary documentation and pinned source before building the plugin. Runtime evidence from the subsequent implementation is in [verification](VERIFICATION.md).

## Feasibility

The useful product is a catalogue of distinct discovery selectors over the user's own playable media. Netflix describes independent personalization of row choice, membership, and ordering, while its curation article shows that many headings express editorial themes. These support the design direction, not an attempt to reproduce unpublished Netflix algorithms. [Netflix recommendation explanation](https://help.netflix.com/en/node/100639), [Netflix curation article](https://www.netflix.com/tudum/articles/netflix-secret-codes-guide).

A standalone HSS extension can supply stable shelves and personalized contents. Automatic page composition is a separate integration problem. High-quality moods depend on editorial evidence; awards depend on work-linked records; whole-story promises depend on reconciled episode manifests. More row names alone will not improve discovery.

## Version evidence

| Component | Evidence | Consequence |
|---|---|---|
| Jellyfin server 10.11.11 | Tag `v10.11.11`, commit `1fbd8739292cce610231be93daf43368733edf63` | Pin Controller/Model packages to 10.11.11; target `net9.0`. |
| Jellyfin Web 10.11.11 | Tag `v10.11.11`, commit `35c0793ece3adbd247eab290ae1effab851f3d37` | Verify against this Web bundle, including HSS's transformations. |
| HSS 3.0.2.0 | Tag `3.0.2.0`, commit `d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd` | Study this release rather than current `main`. |
| Companion plugins | Publisher manifest lists File Transformation and Plugin Pages 3.0.1.0 assets for ABI 10.11.11.0 | Use those exact assets in the development matrix initially. |

The HSS release notes explicitly retain Jellyfin 10.11.11 support and suspend Streamyfin patching. Its release assembly attribute remains `3.0.0.0`; checking only `Assembly.GetName().Version` cannot establish that package 3.0.2.0 is installed. Use plugin/package metadata plus capability checks. [HSS release](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/releases/tag/3.0.2.0), [publisher manifest](https://www.iamparadox.dev/jellyfin/plugins/manifest.json), [assembly attributes](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Properties/AssemblyInfo.cs).

Jellyfin's latest release endpoint currently resolves to 12.2. The implementation remains on the owner's requested 10.11.11; a 12.x build is a later compatibility task. [Current release endpoint](https://github.com/jellyfin/jellyfin/releases/latest).

## HSS's actual extension contract

At the pinned release:

- `PluginInterface.RegisterSection` accepts **Newtonsoft `JObject`**, rather than a JSON string.
- Registration locates a handler by exact assembly full name, class full name, and public method name. `ActivatorUtilities.CreateInstance` constructs it using HSS's service provider on each invocation.
- The public result method accepts one serializable payload and returns **`QueryResult<BaseItemDto>` synchronously**. Returning `Task<QueryResult<BaseItemDto>>` will fail the result cast.
- The callback payload has `UserId` and `AdditionalData`. It has no cancellation token, request limit, language, page hash, row order, or list of other active rows.
- The payload model has a `Limit` field, but registration constructs `PluginDefinedSection`, whose constructor fixes the instance limit to **1**. This is the number of shelf instances, not the number of cards.
- `CreateInstances` returns the same registered section object. Mutating its title/data per user can cross-contaminate users.
- The reflected path is suitable for an installed C# plugin. The alternative endpoint path makes a synchronous HTTP request without propagating a user credential; it adds unnecessary authentication and latency problems here.

[RegisterSection implementation](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/PluginInterface.cs), [registration model](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Model/SectionRegisterPayload.cs), [external section](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/HomeScreen/Sections/PluginDefinedSection.cs).

Section IDs are **strings** in the actual model. Web uses them in class names and `querySelector` without escaping. Use stable CSS-safe IDs beginning with letters, such as `DiscoveryRowsUnder90Movies`, rather than arbitrary numeric-leading UUIDs. This is an implementation inference from the model and rendering source; it needs the first integration test.

Registration stores handlers in a dictionary by section ID, replacing an existing entry on the generic instance path. Serialize our registration operations and keep IDs stable. HSS's admin page creates defaults for newly discovered sections when loaded, but the results service considers only sections represented in persisted `SectionSettings`. Registration alone does not ensure visible rows; onboarding must save HSS section settings. [Manager](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/HomeScreen/HomeScreenManager.cs), [admin configuration page](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Configuration/config.html).

## Homepage capabilities and limits

HSS groups sections by administrator `OrderIndex`, shuffles sections sharing an index, and caches the section metadata for a page. The external registration interface offers no per-user factory that can omit, rename, or reorder sections at page-planning time. Therefore the first version should preserve HSS layout ownership and expose independently selectable static shelves. [Section service](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Services/HomeScreenSectionService.cs).

The HSS loader initially hides a section and assigns its element as the items container's `parentContainer`. Jellyfin Web 10.11.11 removes that hiding class for nonempty results and adds it for empty results. Returning a valid empty result can therefore suppress a shelf visually, though it remains in section metadata/pagination. Browser testing must confirm this with the transformed bundle. [HSS loader](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Controllers/loadSections.js), [Web items container](https://github.com/jellyfin/jellyfin-web/blob/35c0793ece3adbd247eab290ae1effab851f3d37/src/elements/emby-itemscontainer/emby-itemscontainer.js).

The release's manager registers similarity, rewatch, genre, collections, latest/recent, requests, discover, and upcoming sections, along with core home sections. Several shelves listed in the current README, including hidden gems, studio, decade, and collection completion, are absent from that registration list. People sections are commented out. Avoid claiming that all of today's documented shelves are active in 3.0.2.0. The supplied report remains useful as a concept catalogue, not a release-specific capability map.

## Settings labels need an additional compatibility step

The tagged `ModularHomeViews/Sections` controller passes the literal fallback “Genre Section” to translation when a language is supplied. Both HSS settings pages supply a language. External IDs have no bundled translation key, so their settings labels can fall back to that generic text even though homepage headings use the registered title. The public `ITranslationManager.UpdateTranslationPack` method can add our ID-to-title entries to in-memory packs. A narrow reflected compatibility bridge should seed our names after HSS initializes and prove distinct labels in both settings pages. This adds no new row factory and does not modify bundled files. It is outside the documented registration example and remains an integration gate. [Settings controller](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Controllers/ModularHomeViewsController.cs), [translation manager](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Services/TranslationManager.cs).

## User identity at this release

The tagged `HomeScreenController` has `[Authorize]` on results requests, but forwards the query-string `userId` to the callback without comparing it with the authenticated user's claim. The current README describes stronger enforcement added after this release. Our plugin must check the current authenticated claim (`Jellyfin-UserId`) against the payload before any user cache lookup, library query, or DTO generation. A missing context, missing user claim, or mismatch yields an empty result and a bounded diagnostic. This protects our own result handlers; it does not change other HSS endpoints. [Tagged controller](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/Controllers/HomeScreenController.cs), [Jellyfin claim constant](https://github.com/jellyfin/jellyfin/blob/1fbd8739292cce610231be93daf43368733edf63/Jellyfin.Api/Constants/InternalClaimTypes.cs).

## Data that can support the shelves

Jellyfin exposes local runtimes, ratings, IDs, tags, genres, user state, and library change events. Query through `ILibraryManager` and `InternalItemsQuery(user)`, constrain permitted library roots, and recheck `IsVisibleStandalone(user)` before generating DTOs with `IDtoService`. Visibility includes parent/library and parental/tag checks. Its user-data and user-manager interfaces expose events for invalidation. [Library API](https://github.com/jellyfin/jellyfin/blob/1fbd8739292cce610231be93daf43368733edf63/MediaBrowser.Controller/Library/ILibraryManager.cs), [item visibility](https://github.com/jellyfin/jellyfin/blob/1fbd8739292cce610231be93daf43368733edf63/MediaBrowser.Controller/Entities/BaseItem.cs), [DTO API](https://github.com/jellyfin/jellyfin/blob/1fbd8739292cce610231be93daf43368733edf63/MediaBrowser.Controller/Dto/IDtoService.cs).

Neither local `CommunityRating` nor `CriticRating` alone establishes provider vote counts or a trustworthy critical-acclaim claim. Keep externally obtained scores paired with their own provider, scale, votes, and timestamp. TMDB provides details and keyword endpoints; it distinguishes popularity from trending. Use those signals for separate purposes. [Movie details](https://developer.themoviedb.org/reference/movie-details), [keywords](https://developer.themoviedb.org/reference/movie-keywords), [popularity](https://developer.themoviedb.org/docs/popularity-and-trending).

Series and season detail endpoints are suitable inputs to an episode reconciliation pipeline. They do not independently establish that a story has a satisfying ending or that alternate episode orders have been matched correctly. Local file runtime remains the authority for a time promise. [Series details](https://developer.themoviedb.org/reference/tv-series-details), [season details](https://developer.themoviedb.org/reference/tv-season-details).

Mood/pacing labels need a small reviewed vocabulary, administrator overrides, and unknown states. Synopsis keywords can suggest a theme but often cannot prove emotional outcome or a strong pilot. Start with manual/imported labels. Optional offline model suggestions can follow; factual records remain separate.

Awards should use versioned work-linked records reconciled to provider IDs, with source URLs, ceremony years, categories, and outcomes. The Golden Globes offers an official database; this does not establish a supported bulk-ingestion API or redistribution permission. Start with administrator-imported verified records and decide distributable datasets separately. [Official awards database](https://goldenglobes.com/winners-nominees/).

## Operational evidence

TMDB supports appending subresources to a detail request, which can reduce enrichment calls. Its limits can change and clients must handle 429 responses. Attribution requires an approved logo and an About/Credits notice. Use an administrator-supplied token; send only provider IDs and metadata parameters, not personal viewing history. [Append to response](https://developer.themoviedb.org/docs/append-to-response), [rate limiting](https://developer.themoviedb.org/docs/rate-limiting), [attribution FAQ](https://developer.themoviedb.org/docs/faq).

The official plugin template demonstrates `BasePlugin<PluginConfiguration>`, embedded dashboard pages, dependency injection, and runtime exclusion of Jellyfin assemblies. Its package versions are a template default, not our target. Both `Jellyfin.Controller` and `Jellyfin.Model` 10.11.11 were confirmed in NuGet's flat-container index. No SDK is installed locally, so the first milestone must supply a .NET 9 build environment. [Official template](https://github.com/jellyfin/jellyfin-plugin-template), [plugin documentation](https://jellyfin.org/docs/general/server/plugins/).

## What remains experimental

The first build must establish reflected JObject construction across load contexts, handler DI resolution, authenticated HttpContext availability, persistent HSS settings, CSS-safe IDs, empty-row rendering, and native DTO navigation/playback. Static inspection supports the approach but cannot certify those runtime behaviors. Larger scale latency, metadata coverage, and recommendation quality also require fixtures and the owner's real library after the prototype works.
