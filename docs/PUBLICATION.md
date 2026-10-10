# Repository publication

Repository URL for Jellyfin:

```text
https://raw.githubusercontent.com/diverseau/jellyfin-plugin-discovery-rows/main/manifest.json
```

The root manifest describes the exact release ZIP hosted in GitHub Releases. Jellyfin 10.11.11 expects an array of packages, four-part assembly/ABI versions, a HTTPS ZIP source URL and an MD5 checksum of the complete ZIP. SHA-256 release hashes provide an additional independent check.

For release 1.0.1, build and generate the manifest:

```bash
./scripts/build.sh
python scripts/generate-manifest.py \
  --archive artifacts/DiscoveryRows-1.0.1.0-jellyfin-10.11.11.zip \
  --source-url https://github.com/diverseau/jellyfin-plugin-discovery-rows/releases/download/v1.0.1/DiscoveryRows-1.0.1.0-jellyfin-10.11.11.zip \
  --changelog "Register shelves before the first homepage request; fix startup layout caching." \
  --output work/next-manifest.json
```

Publish the ZIP, `SHA256SUMS` and `release.json` as release assets before publishing the manifest that points to them. Do not rebuild or replace a published version's archive; changing it requires updating the checksum and is unsuitable for immutable releases. For future versions, update assembly/package metadata and retain prior version entries in the manifest, newest first. The generator reads the current project version and keeps prior entries from `manifest.json`; copy the generated manifest into place only after its release archive is live.

Only tracked source and release artifacts are published. Ignored `work/` contains disposable server data and access tokens and must never be uploaded. Upstream Jellyfin/HSS assemblies are not included in our ZIP.

Verify anonymous retrieval of both URLs, compare MD5/SHA-256, then add the repository and perform a catalogue installation on a disposable Jellyfin 10.11.11 instance. Restart it and confirm the loaded assembly version, HSS registration and administrator status endpoint. Production installation remains the user's choice.
