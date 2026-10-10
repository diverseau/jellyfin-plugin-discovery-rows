#!/usr/bin/env python3
"""Package only our assemblies; never redistribute Jellyfin or HSS binaries."""
import hashlib
import json
import pathlib
import zipfile

root = pathlib.Path(__file__).resolve().parents[1]
source = root / "src/Jellyfin.Plugin.DiscoveryRows/bin/Release/net9.0"
destination = root / "artifacts"
destination.mkdir(exist_ok=True)
files = ["Jellyfin.Plugin.DiscoveryRows.dll", "DiscoveryRows.Core.dll", "Jellyfin.Plugin.DiscoveryRows.deps.json"]
release = destination / "DiscoveryRows-1.0.0.0-jellyfin-10.11.11.zip"
with zipfile.ZipFile(release, "w", zipfile.ZIP_DEFLATED) as archive:
    for name in files:
        assert (source / name).exists(), f"Build first: missing {name}"
        archive.write(source / name, name)
    for name in ["LICENSE", "NOTICE.md", "docs/INSTALL.md"]:
        archive.write(root / name, pathlib.Path(name).name)
metadata = {"name": "Discovery Rows", "guid": "b87d3d47-7a2d-43c6-89d2-592885b90d3e", "version": "1.0.0.0",
            "targetAbi": "10.11.11.0", "framework": "net9.0", "hssVersion": "3.0.2.0", "archive": release.name,
            "sha256": hashlib.sha256(release.read_bytes()).hexdigest(),
            "files": {f: hashlib.sha256((source / f).read_bytes()).hexdigest() for f in files},
            "publication": "Local build; no public download URL or repository manifest has been published."}
(destination / "release.json").write_text(json.dumps(metadata, indent=2) + "\n")
(destination / "SHA256SUMS").write_text(f'{metadata["sha256"]}  {release.name}\n')
print(f"Packaged {release}")
