# Disposable integration fixture

The fixture created during development lives under ignored `work/fixture`. Container name: `discovery-rows-fixture-101111`. Port: `127.0.0.1:18096`, with no public bind and no production mounts. Stop/remove it after use. Tokens in `work/fixture/auth.json` are fixture-only and must not be committed or included in packages.

Verified image digest:

```text
jellyfin/jellyfin@sha256:aefb67e6a7ff1debdd154a78a7bbb780fd0c873d8639210a7f6a2016ad2b35db
```

The source lock lists exact companion artifacts. Their downloaded SHA-256 values are in `docs/build-lock.json`; published MD5 values were checked too. The fixture has 13 movie files and seven two-episode series. Each file is a tiny synthetic H.264 MP4, with local NFO metadata; remote metadata/image providers are disabled for the fixture libraries.

Create disposable accounts `fixture-admin`, `fixture-a`, `fixture-b`, and `fixture-child` with a local test password. Supply that password to browser checks through `DISCOVERY_FIXTURE_PASSWORD`; never commit it. A/child have MoviesA + ShowsA; B has MoviesB. The child blocks a tag and high parental rating. This is deliberately disposable test data, never an example deployment configuration.

## Re-run against the retained fixture

```bash
./scripts/build.sh
# With the fixture stopped, copy the package's three runtime files into its plugin folder.
# Start the existing named fixture, then wait for startup/registration/indexing.
python dev/verify-server.py
npm install --prefix work/browser --no-audit --no-fund playwright-core@1.64.0
read -r -s -p "Fixture password: " DISCOVERY_FIXTURE_PASSWORD
export DISCOVERY_FIXTURE_PASSWORD
node dev/browser-check.cjs
unset DISCOVERY_FIXTURE_PASSWORD
```

`verify-server.py` checks the server's version and fixture name before mutations. It validates identity/access, editorial and awards imports, local completeness, user preferences, warm-cache policy changes, parallel ordering and absence of provider work on the result path. Synthetic evidence is saved only under `work/fixture` and never packaged. Configuration is restored at successful completion; if interrupted, inspect the fixture's settings before rerunning.

`browser-check.cjs` uses a new headless Chromium process, not the user's browser session. Set `CHROMIUM_PATH` if needed. It checks the real Jellyfin/HSS homepage, hides a deliberately underfilled row, exercises settings/preview/preferences, and starts native playback. It restores the temporarily changed configuration in `finally`. Screenshots and compact verification JSON go under ignored `work/`.

Some additional one-time checks (ordinary-user Plugin Pages hide/restore and restart persistence) are recorded in the verification report. The checks cover the exact stack; they do not establish native-client or Jellyfin 12 support.

`verify-startup.py` requires the retained fixture to be stopped with the current package already installed. It starts that named container, polls immediately until HTTP is available, and checks that the very first HSS layout already includes the selected discovery shelves. It reuses that cached layout after indexing and verifies the actual HSS result callback. Run `python dev/verify-startup.py`; stop the fixture afterward. It does not alter plugin or user configuration.

`verify-homepage-setup.py` exercises the admin-only one-time HSS setup, shared/personal selection, preservation and subsequent plugin-only shelf toggles. It restarts the disposable fixture and restores both configurations in `finally`. `browser-setup-check.cjs` clicks the new controls and verifies their persistence and real homepage hide/show behavior. Run these sequentially, because the suites temporarily change the same fixture settings.

`verify-automatic-metadata.py` temporarily gives one synthetic movie ordinary Family/Comedy/Animation metadata and keyword/year inputs, then checks actual automatic HSS results, explanation sources, access filtering, corrections, strict mode, revalidation and zero provider calls. `browser-automatic-check.cjs` exercises the same automatic workflow in Web, including selecting matching shelves and correcting/resetting a pick without JSON. Both restore item/evidence/plugin/HSS data in `finally`; run them sequentially with other fixture suites.
