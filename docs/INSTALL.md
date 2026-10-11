# Install Discovery Rows 1.0.3

Supported and tested stack: **Jellyfin server/Web 10.11.11, Home Screen Sections 3.0.2.0**. The development fixture also used **File Transformation 3.0.1.0 and Plugin Pages 3.0.1.0**. Native clients and Jellyfin 12.x are not certified by this release.

## Update from earlier releases

In Dashboard → Plugins → Catalogue, choose Discovery Rows and install **1.0.3.0**, then restart Jellyfin. Existing shelf configuration, HSS settings and plugin data are retained on upgrade. The repository URL stays the same. Automatic metadata matching defaults on, including when loading old configuration. If you already completed the 1.0.2 HSS setup, you can keep that layout.

Open Setup and save **Automatically match moods and themes from library metadata**. In Shelves choose a user, click **Select shelves with enough matches**, then **Save shelf settings** and refresh Home. TMDB is optional for local genre/tag/year matches; its keywords improve theme coverage.

## Install from the catalogue

1. Install HSS and its dependencies using [the publisher's repository](https://www.iamparadox.dev/jellyfin/plugins/manifest.json), selecting builds for Jellyfin 10.11.11. Restart Jellyfin if those were newly installed.
2. Open **Dashboard → Plugins → Repositories → New Repository**. Enter **Discovery Rows** as the name and paste this URL:

   ```text
   https://raw.githubusercontent.com/diverseau/jellyfin-plugin-discovery-rows/main/manifest.json
   ```

3. Click **Add**, open **Catalogue**, choose **Discovery Rows**, and install **1.0.3.0**. Restart Jellyfin.
4. Follow **Configure the shelves** below.

The repository provides Discovery Rows only. HSS and its dependencies must be installed separately. Jellyfin validates the downloaded ZIP against the manifest's MD5; SHA-256 hashes are also included with the [release](https://github.com/diverseau/jellyfin-plugin-discovery-rows/releases/tag/v1.0.3).

## Manual package installation

1. Install HSS and its dependencies using [the publisher's repository](https://www.iamparadox.dev/jellyfin/plugins/manifest.json), selecting builds for Jellyfin 10.11.11. Restart Jellyfin if those were newly installed.
2. Stop Jellyfin. Create `DiscoveryRows_1.0.3.0` inside **your server's actual plugins directory**, then extract `DiscoveryRows-1.0.3.0-jellyfin-10.11.11.zip` into it. On the official Docker image, that is `/config/plugins/DiscoveryRows_1.0.3.0`; package installs commonly use `/var/lib/jellyfin/plugins`. Use the path for your installation.
3. Ensure the Jellyfin service account can read the extracted files and write its own data/configuration directories. Start Jellyfin.
4. Follow **Configure the shelves** below.

The archive contains only our plugin/core assemblies, dependency metadata, installation guide and notices. It does not install or change your running server automatically.

## Configure the shelves in one place

1. In Dashboard → Plugins, open **Discovery Rows → Setup**. Check that 38 shelves have registered and the local index is populated. Initial indexing runs in background, generally within 15 seconds after startup, plus library processing time.
2. Leave **Let users choose Discovery shelves separately in Modular Home** unchecked, then click **Enable all shelves in HSS**. This saves all 38 HSS entries as enabled and locked to the shared administrator selection. It preserves other HSS sections, global settings, existing Discovery order/card styles and saved personal selections. New or placeholder-order shelves receive distinct positions after configured rows.
3. **Restart Jellyfin once** to clear HSS 3.0.2.0's cached layout. Enable Modular Home in each user's menu if it is not already enabled; no individual Discovery shelf selection is needed in shared mode.
4. In **Discovery Rows → Shelves**, check only the shelves you want and click **Save shelf settings**. **Select all** and **Clear selection** change the enabled checkboxes without changing sizes or filters; save to apply them. Refresh Home.

After this setup, leave all 38 Discovery entries enabled in HSS. Choose what appears only in Discovery Rows. Disabled shelves return no cards and remain hidden, even though HSS lists them as enabled. Later shelf toggles need a save and browser refresh, with no HSS edit, personal settings save or restart. HSS still controls row ordering and card styles.

For individual selections, check the personal-selection option before clicking the setup button. Existing personal selections are retained; each user can choose shelves in Modular Home, subject to the enabled checkboxes in Discovery Rows. Switching between shared and personal modes needs another HSS setup and restart. You may also configure everything manually through HSS as in earlier releases.

## Useful offline start

Use Movies Under 90 Minutes, 90-Minute Comedies/Thrillers, Short Episodes, 30-Minute Laughs and Short Documentaries where the library can fill them. Start with 6–10 discovery rows alongside Continue Watching and Next Up. Give each a distinct HSS order number to avoid HSS shuffling rows sharing an index.

By default each row needs **six credible candidates** and displays up to **16 cards**. An administrator may explicitly lower the minimum for small curated shelves. Time rows reject null runtime and any selectable edition exceeding the limit. Series rows require a real accessible regular starting episode; the plugin does not substitute a later file for a missing pilot.

The first 15 presets are enabled in Discovery Rows configuration. Awards, complete-story and theme presets are registered but disabled until enabled by the administrator. In shared mode, HSS keeps every Discovery entry enabled and the plugin checkboxes control visibility. In personal mode, HSS user selection is an additional filter. A registered row can legitimately remain empty.

## Automatic matching and occasional corrections

Automatic matching is enabled by default. Family Night uses the Family genre (or appropriate Animation/rating metadata) and excludes known mature ratings and dark genres. Comedy supports laughter; Action/Adventure/Science Fiction supports popcorn picks. Feel-Good is an estimate from explicit keywords or Comedy with Family/Romance/Animation, excluding dark genres/keywords. Existing Jellyfin tags are usable too. All 16 themes have keyword/year rules; actual coverage depends on the available metadata. See [AUTOMATIC_MATCHING.md](AUTOMATIC_MATCHING.md) for the rules and limits.

In Shelves, **Select shelves with enough matches** evaluates all 38 shelves, including disabled ones, for the selected user using saved eligibility settings. It changes only enabled checkboxes; **Save shelf settings** applies them. Coordination can still leave overlapping shelves below their minimum. Empty optional awards/opinion shelves need not be selected.

Preview shows the exact genre, tag, keyword or year behind each automatic match. Estimates are never saved as reviewed labels. If a pick is wrong, use **Remove from this shelf** in Preview. In Evidence, find a title, choose a shelf, and use **Include in selected shelf**, **Exclude from selected shelf**, or **Reset to automatic**. These are administrator corrections shared by users, subject to each user's access/history; individual users can still hide titles through Discovery preferences. Turn automatic matching off in Setup for reviewed-only operation.

Advanced JSON import/export remains for factual awards and curated records. Imports replace included work keys while preserving other records; provider updates retain reviewed facets, exclusions, awards and manual metadata/manifests. Awards still need a specific work, body, ceremony, category, outcome and source. Strong-pilot/first-season opinion rows still need reviewed labels; they are not inferred from the rating of an entire series.

One-night/weekend rows can use TMDB's **Ended + Miniseries** metadata without a hand-written story label, but require a fresh complete episode manifest, all required local accessible episodes and the total runtime within the limit. This is a complete airing/file check, not a guarantee of a satisfying narrative ending. Canceled status alone does not qualify automatically.

## Optional TMDB

Set an **API read access token** once in Setup, enable TMDB, save settings, and click **Enrich whole library** (or run **Discovery Rows: optional TMDB enrichment**) from Scheduled Tasks or the plugin page. An API key is not the same credential as the read access bearer token. The token is stored separately and is never returned by status/configuration/export endpoints.

Only existing local provider IDs and metadata parameters go to TMDB; viewing history stays local. One run processes all eligible missing/stale local TMDB IDs in batches; it no longer stops after the first 100. Existing older records are backfilled with catalogue metadata. The six-hour scheduled task maintains the cache. Jobs have a rate budget, cancellation, timeouts and limited retries. Homepage callbacks do not invoke providers or classifiers. Invalid credentials stop repeated retries until replaced. Existing evidence is retained on provider errors.

## Personal preferences

Use **Discovery preferences** in the user menu (Plugin Pages) to find titles and hide/restore them for your own account. A hide applies to all Discovery Rows shelves. It does not change Jellyfin watched state or anyone else's preferences.

## Updates, data and removal

- The plugin's stable ID is `b87d3d47-7a2d-43c6-89d2-592885b90d3e`. Shelf IDs also remain stable, preserving HSS selections.
- Jellyfin stores the small configuration in its plugin configuration directory. Plugin-owned evidence/history/token files are under `<Jellyfin DataPath>/discovery-rows/` (normally `/config/data/discovery-rows` in the official container). Back that directory and the plugin configuration up before replacing versions.
- Stop Jellyfin before replacing the package and remove the old Discovery Rows version folder so only one version is installed. Restart and recheck status; retain the existing HSS layout.
- To uninstall, stop Jellyfin and remove only the Discovery Rows plugin folder. Restart. The plugin's data can be retained for reinstall, or deliberately removed separately. Jellyfin library metadata/NFO/tags are never rewritten by this plugin.
- Corrupt evidence/history is preserved and reported, with a recovery backup made when valid data is subsequently saved.

## Troubleshooting

**Registered but absent:** use Setup → Enable all shelves in HSS, restart once, and confirm Modular Home is enabled for the user. Check the desired shelf is enabled in Discovery Rows, then inspect Preview for minimum-size or missing-evidence conditions.

**Preview has cards but Home omits the shelf:** use Discovery Rows 1.0.3.0 or later and restart Jellyfin after updating. Version 1.0.1 fixed the startup registration issue: 1.0.0 could register after the initial HSS layout had already been cached. Separately, HSS 3.0.2.0 can retain an old row order after settings changes; restarting the server clears that cache. Browser refresh alone may reuse it. Orders 20–25 place the rows after existing rows with smaller order numbers.

**First user selection reverts:** on a fresh HSS 3.0.2.0 install, its first per-user Modular Home save can create an empty settings file without saving the selection. Reopen Modular Home and save again. This was observed in the fixture and matches the [tagged HSS save implementation](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/blob/d9798e410cacd4b8fbac4f57fcf3d5c05dd42dbd/src/Jellyfin.Plugin.HomeScreenSections/HomeScreen/HomeScreenManager.cs). Discovery Rows honors the effective selection once persisted and does not modify HSS's private settings files.

**Index empty/stale:** run the rebuild task and inspect Jellyfin logs. Only actual local playable records are included; remote channels, missing files and placeholders are excluded. A refresh failure retains the preceding snapshot.

**Wrong/generic settings labels:** status should show label compatibility initialized. The bridge supplies English fallback names in HSS's in-memory language packs. Restart or rebuild; this version does not claim localized shelf translations.

**Too much repetition:** enable coordination and confirm effective HSS layout is available. Deduplication covers selected Discovery Rows shelves, not unrelated HSS sections. Repeated refreshes within a daily bucket intentionally preserve order.

**Cannot start a complete-story row:** check episode numbering/order, every required episode's local availability/access, manifest freshness, total duration and reviewed story completeness. A satisfying narrative ending cannot be inferred from file counts.

**Server version mismatch:** use the exact tested stack. Full-page automatic composition, native-client support and a Jellyfin 12 build are separate future integrations.
