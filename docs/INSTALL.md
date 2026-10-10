# Install Discovery Rows 1.0.1

Supported and tested stack: **Jellyfin server/Web 10.11.11, Home Screen Sections 3.0.2.0**. The development fixture also used **File Transformation 3.0.1.0 and Plugin Pages 3.0.1.0**. Native clients and Jellyfin 12.x are not certified by this release.

## Update from 1.0.0

In Dashboard → Plugins → Catalogue, choose Discovery Rows and install **1.0.1.0**, then restart Jellyfin and refresh Home. Existing shelf settings, HSS selection and plugin data are retained. Version 1.0.1 registers the shelves before the first homepage request, fixing a startup timing issue that let HSS cache a layout without them. The repository URL stays the same.

## Install from the catalogue

1. Install HSS and its dependencies using [the publisher's repository](https://www.iamparadox.dev/jellyfin/plugins/manifest.json), selecting builds for Jellyfin 10.11.11. Restart Jellyfin if those were newly installed.
2. Open **Dashboard → Plugins → Repositories → New Repository**. Enter **Discovery Rows** as the name and paste this URL:

   ```text
   https://raw.githubusercontent.com/diverseau/jellyfin-plugin-discovery-rows/main/manifest.json
   ```

3. Click **Add**, open **Catalogue**, choose **Discovery Rows**, and install **1.0.1.0**. Restart Jellyfin.
4. Follow **Configure the shelves** below.

The repository provides Discovery Rows only. HSS and its dependencies must be installed separately. Jellyfin validates the downloaded ZIP against the manifest's MD5; SHA-256 hashes are also included with the [release](https://github.com/diverseau/jellyfin-plugin-discovery-rows/releases/tag/v1.0.1).

## Manual package installation

1. Install HSS and its dependencies using [the publisher's repository](https://www.iamparadox.dev/jellyfin/plugins/manifest.json), selecting builds for Jellyfin 10.11.11. Restart Jellyfin if those were newly installed.
2. Stop Jellyfin. Create `DiscoveryRows_1.0.1.0` inside **your server's actual plugins directory**, then extract `DiscoveryRows-1.0.1.0-jellyfin-10.11.11.zip` into it. On the official Docker image, that is `/config/plugins/DiscoveryRows_1.0.1.0`; package installs commonly use `/var/lib/jellyfin/plugins`. Use the path for your installation.
3. Ensure the Jellyfin service account can read the extracted files and write its own data/configuration directories. Start Jellyfin.
4. Follow **Configure the shelves** below.

The archive contains only our plugin/core assemblies, dependency metadata, installation guide and notices. It does not install or change your running server automatically.

## Configure the shelves

1. In Dashboard → Plugins, open **Discovery Rows**. Check that 38 shelves have registered and the local index is populated. Initial indexing runs in background, generally within 15 seconds after startup, plus library processing time.
2. Open **Home Screen Sections** configuration. Choose a small selection of Discovery Rows shelves and set their order/view mode. **Save HSS settings**. Registration alone does not persist HSS's layout entries.
3. Open **Modular Home** in the user's menu, enable it and select shelves. Refresh the homepage.

## Useful offline start

Use Movies Under 90 Minutes, 90-Minute Comedies/Thrillers, Short Episodes, 30-Minute Laughs and Short Documentaries where the library can fill them. Start with 6–10 discovery rows alongside Continue Watching and Next Up. Give each a distinct HSS order number to avoid HSS shuffling rows sharing an index.

By default each row needs **six credible candidates** and displays up to **16 cards**. An administrator may explicitly lower the minimum for small curated shelves. Time rows reject null runtime and any selectable edition exceeding the limit. Series rows require a real accessible regular starting episode; the plugin does not substitute a later file for a missing pilot.

The first 15 presets are enabled in Discovery Rows configuration. Awards, complete-story and theme presets are registered but disabled until enabled by the administrator. HSS selection and Discovery Rows eligibility settings are separate. A registered row can legitimately remain empty.

## Add reviewed evidence

1. In Discovery Rows → Evidence, select a test user and find a title. Its work key is based on verified local TMDB/IMDb metadata, or its Jellyfin item ID if no provider ID exists.
2. Choose **Edit evidence** or **Add reviewed facet**, adjust the JSON, then **Validate and import**. Use confidence 0–1, a review timestamp and source. Keep a review record/source URL where appropriate.
3. In Preview, inspect candidate counts, rejected conditions and per-title scores.

Imports replace the included work keys while preserving other records. Provider updates retain manual facets, exclusions, non-TMDB ratings, manual manifests and awards. Evidence export omits credentials and personal user history. Example formats are in the source repository's `samples/` directory; placeholders must be replaced with your actual work keys and facts.

Mood rows need reviewed labels; general Comedy does not automatically mean feel-good, popcorn or a strong opening. Awards require body, ceremony year, category, outcome, exact work link and evidence URL. Person-only awards do not qualify a film. Complete-story rows require reconciled required episodes plus reviewed complete-limited-story evidence. Ended/canceled status alone does not establish a resolved story.

## Optional TMDB

Set an **API read access token** in Setup, enable TMDB, save settings, and run **Discovery Rows: optional TMDB enrichment** from Scheduled Tasks or the plugin page. An API key is not the same credential as the read access bearer token. The token is stored separately and is never returned by status/configuration/export endpoints.

Only existing local provider IDs and metadata parameters go to TMDB; viewing history stays local. Jobs run in bounded batches with a rate budget, timeouts and limited retries. Homepage callbacks do not invoke providers or classifiers. Invalid credentials stop repeated retries until replaced. Existing evidence is retained on provider errors.

## Personal preferences

Use **Discovery preferences** in the user menu (Plugin Pages) to find titles and hide/restore them for your own account. A hide applies to all Discovery Rows shelves. It does not change Jellyfin watched state or anyone else's preferences.

## Updates, data and removal

- The plugin's stable ID is `b87d3d47-7a2d-43c6-89d2-592885b90d3e`. Shelf IDs also remain stable, preserving HSS selections.
- Jellyfin stores the small configuration in its plugin configuration directory. Plugin-owned evidence/history/token files are under `<Jellyfin DataPath>/discovery-rows/` (normally `/config/data/discovery-rows` in the official container). Back that directory and the plugin configuration up before replacing versions.
- Stop Jellyfin before replacing the package and remove the old Discovery Rows version folder so only one version is installed. Restart and recheck status; retain the existing HSS layout.
- To uninstall, stop Jellyfin and remove only the Discovery Rows plugin folder. Restart. The plugin's data can be retained for reinstall, or deliberately removed separately. Jellyfin library metadata/NFO/tags are never rewritten by this plugin.
- Corrupt evidence/history is preserved and reported, with a recovery backup made when valid data is subsequently saved.

## Troubleshooting

**Registered but absent:** save HSS section settings, enable Modular Home for the user, and refresh the browser. Then inspect Preview for minimum-size or missing-evidence conditions.

**Preview has cards but Home omits the shelf:** use Discovery Rows 1.0.1.0 or later and restart Jellyfin after updating. Version 1.0.0 could register after the initial HSS layout had already been cached. Separately, HSS 3.0.2.0 can retain an old row order after settings changes; restarting the server clears that cache. Browser refresh alone may reuse it. Orders 20–25 place the rows after existing rows with smaller order numbers.

**First user selection reverts:** on a fresh HSS 3.0.2.0 install, its first per-user Modular Home save can create an empty settings file without saving the selection. Reopen Modular Home and save again. This was observed in the fixture and matches the [tagged HSS save implementation](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/HomeScreen/HomeScreenManager.cs). Discovery Rows honors the effective selection once persisted and does not modify HSS's private settings files.

**Index empty/stale:** run the rebuild task and inspect Jellyfin logs. Only actual local playable records are included; remote channels, missing files and placeholders are excluded. A refresh failure retains the preceding snapshot.

**Wrong/generic settings labels:** status should show label compatibility initialized. The bridge supplies English fallback names in HSS's in-memory language packs. Restart or rebuild; this version does not claim localized shelf translations.

**Too much repetition:** enable coordination and confirm effective HSS layout is available. Deduplication covers selected Discovery Rows shelves, not unrelated HSS sections. Repeated refreshes within a daily bucket intentionally preserve order.

**Cannot start a complete-story row:** check episode numbering/order, every required episode's local availability/access, manifest freshness, total duration and reviewed story completeness. A satisfying narrative ending cannot be inferred from file counts.

**Server version mismatch:** use the exact tested stack. Full-page automatic composition, native-client support and a Jellyfin 12 build are separate future integrations.
