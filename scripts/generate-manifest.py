#!/usr/bin/env python3
"""Generate Jellyfin's repository manifest from the exact release ZIP."""
import argparse
import datetime
import hashlib
import json
import pathlib
import urllib.parse
import zipfile
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--archive", type=pathlib.Path, required=True)
parser.add_argument("--source-url", required=True)
parser.add_argument("--output", type=pathlib.Path, default=pathlib.Path("manifest.json"))
parser.add_argument("--timestamp", help="UTC ISO timestamp; defaults to now")
parser.add_argument("--changelog", required=True)
parser.add_argument("--previous", type=pathlib.Path, default=pathlib.Path("manifest.json"), help="Retain earlier releases from this manifest")
args = parser.parse_args()
root = pathlib.Path(__file__).resolve().parents[1]
version = ET.parse(root / "Directory.Build.props").findtext(".//Version") + ".0"
url = urllib.parse.urlparse(args.source_url)
if url.scheme != "https" or not url.netloc or not url.path.endswith(".zip"):
    parser.error("source-url must be an HTTPS URL ending in .zip")
with zipfile.ZipFile(args.archive) as archive:
    required = {"Jellyfin.Plugin.DiscoveryRows.dll", "DiscoveryRows.Core.dll",
                "Jellyfin.Plugin.DiscoveryRows.deps.json"}
    if not required.issubset(archive.namelist()):
        parser.error("archive is missing Discovery Rows runtime files")
    if any(n.endswith(".dll") and n not in required for n in archive.namelist()):
        parser.error("archive contains unexpected dependency assemblies")
timestamp = args.timestamp or datetime.datetime.now(datetime.timezone.utc).isoformat()
if datetime.datetime.fromisoformat(timestamp.replace("Z", "+00:00")).utcoffset() != datetime.timedelta(0):
    parser.error("timestamp must include UTC timezone")
manifest = [{
    "guid": "b87d3d47-7a2d-43c6-89d2-592885b90d3e",
    "name": "Discovery Rows",
    "overview": "Time, mood, awards and personal discovery shelves for Home Screen Sections.",
    "description": "Adds 38 discovery shelves for local playable media with one-click HSS setup and shared selection from the plugin dashboard. Includes time-based picks, reviewed moods, awards, complete stories, personal exclusions and administrator previews. Requires Jellyfin 10.11.11 and Home Screen Sections 3.0.2.0 with File Transformation and Plugin Pages. Moods and themes automatically match library genres, tags, year and optional TMDB keywords. Includes simple corrections, populated-shelf selection and whole-library enrichment. Awards still require actual records.",
    "owner": "diverseau",
    "category": "General",
    "versions": [{
        "version": version,
        "targetAbi": "10.11.11.0",
        "sourceUrl": args.source_url,
        "checksum": hashlib.md5(args.archive.read_bytes()).hexdigest(),
        "timestamp": timestamp,
        "changelog": args.changelog
    }]
}]
if args.previous.exists():
    previous = json.loads(args.previous.read_text())
    prior = next((p for p in previous if p["guid"] == manifest[0]["guid"]), None)
    if prior:
        manifest[0]["versions"].extend(v for v in prior["versions"] if v["version"] != version)
        manifest[0]["versions"].sort(key=lambda v: tuple(map(int, v["version"].split("."))), reverse=True)
args.output.write_text(json.dumps(manifest, indent=2) + "\n")
print(f"Generated {args.output} from {args.archive}")
