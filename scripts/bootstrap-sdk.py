#!/usr/bin/env python3
"""Install only the pinned build SDK into this checkout's ignored work directory."""
import hashlib
import json
import pathlib
import tarfile
import urllib.request

root = pathlib.Path(__file__).resolve().parents[1]
version = json.loads((root / "global.json").read_text())["sdk"]["version"]
metadata = json.load(urllib.request.urlopen("https://builds.dotnet.microsoft.com/dotnet/release-metadata/9.0/releases.json", timeout=30))
sdk = next(s for release in metadata["releases"] for s in release.get("sdks", [release["sdk"]]) if s["version"] == version)
asset = next(f for f in sdk["files"] if f["rid"] == "linux-x64" and f["name"].endswith("tar.gz"))
target = root / "work/dotnet"
target.mkdir(parents=True, exist_ok=True)
archive = root / "work/dotnet-sdk.tar.gz"
if not archive.exists() or hashlib.sha512(archive.read_bytes()).hexdigest() != asset["hash"].lower():
    print(f"Downloading .NET SDK {version} for linux-x64", flush=True)
    urllib.request.urlretrieve(asset["url"], archive)
assert hashlib.sha512(archive.read_bytes()).hexdigest() == asset["hash"].lower(), "SDK checksum mismatch"
with tarfile.open(archive) as package:
    package.extractall(target, filter="data")
print(f"SDK ready: {target / 'dotnet'}")
