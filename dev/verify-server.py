#!/usr/bin/env python3
"""Behavioral API checks against the explicitly disposable, initialized fixture."""
import concurrent.futures
import datetime
import json
import os
import pathlib
import statistics
import time
import urllib.error
import urllib.parse
import urllib.request

root = pathlib.Path(__file__).resolve().parents[1]
base = os.environ.get("DISCOVERY_FIXTURE_URL", "http://127.0.0.1:18096")
auth = json.loads((root / "work/fixture/auth.json").read_text())
checks = []

def request(path, method="GET", body=None, who="admin", expected=200):
    headers = {"Content-Type": "application/json", "X-Emby-Token": auth[who]["token"]}
    req = urllib.request.Request(base + path, data=json.dumps(body).encode() if body is not None else None, headers=headers, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=30)
    except urllib.error.HTTPError as ex:
        assert ex.code == expected, (path, ex.code, ex.read().decode()[:500])
        return None
    assert response.status == expected, (path, response.status, expected)
    data = response.read()
    return json.loads(data) if data else None

def check(name, condition):
    assert condition, name
    checks.append(name)
    print("PASS", name, flush=True)

def result(section, who="a", user=None, suffix=""):
    uid = user or auth[who]["id"]
    return request("/HomeScreen/Section/" + section + "?userId=" + uid + suffix, who=who)["Items"]

info = request("/System/Info")
assert info["Version"] == "10.11.11" and info["ServerName"].startswith("Discovery Rows disposable"), "Only run mutations against our disposable fixture."
status = request("/DiscoveryRows/Status")
check("38 reflected shelves with labels initialized", status["registeredRows"] == 38 and status["labelsReady"])
types = request("/ModularHomeViews/Sections?language=en")["Items"]
ours = [s for s in types if s["Section"].startswith("DiscoveryRows")]
check("distinct readable settings names", len({s["DisplayText"] for s in ours}) == 38)

for who, prefix in [("a", "A "), ("b", "B "), ("child", "A ")]:
    first = result("DiscoveryRowsUnder90Movies", who)
    second = result("DiscoveryRowsUnder90Movies", who)
    check(who + " sees only allowed library and stable order", bool(first) and all(i["Name"].startswith(prefix) for i in first) and [i["Id"] for i in first] == [i["Id"] for i in second])
child = result("DiscoveryRowsUnder90Movies", "child")
check("blocked tag and parental rating filtered", not any(i["Name"].endswith(("09", "10")) for i in child))
check("forged HSS userId fails before cache use", result("DiscoveryRowsUnder90Movies", "b", auth["a"]["id"]) == [])
check("administrator HSS userId mismatch also fails", result("DiscoveryRowsUnder90Movies", "admin", auth["a"]["id"]) == [])
same = result("DiscoveryRowsUnder90Movies")
check("untrusted AdditionalData cannot select another row", [i["Id"] for i in same] == [i["Id"] for i in result("DiscoveryRowsUnder90Movies", suffix="&additionalData=" + urllib.parse.quote('{"rowId":"gems","userId":"other"}'))])
request("/DiscoveryRows/Evidence", who="a", expected=403)
request("/DiscoveryRows/Status", who="a", expected=403)
check("administrator data endpoints reject ordinary users", True)
request("/DiscoveryRows/Evidence", "PUT", {"schemaVersion": 99, "items": []}, expected=400)
check("unknown evidence schema rejected", True)

library = request("/DiscoveryRows/Library?userId=" + auth["a"]["id"])["items"]
film = next(i for i in library if i["Name"] == "A Fixture Film 01")
series = next(i for i in library if i["Kind"] == "Series")
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
facets = lambda names: [{"name": n, "positive": True, "confidence": .95, "reviewed": True, "source": "synthetic_fixture_review", "reviewedAt": now} for n in names]
episode_list = request("/Shows/" + series["Id"] + "/Episodes?userId=" + auth["a"]["id"])["Items"]
doc = {"schemaVersion": 1, "items": [
    {"workKey": film["WorkKey"], "facets": facets(["humour", "uplifting", "popcorn", "broad_appeal", "found_family"]), "rating": {"provider": "synthetic_fixture", "value": 8.5, "scale": 10, "votes": 10000, "fetchedAt": now}, "awards": [{"workKey": film["WorkKey"], "body": "academy", "ceremonyYear": 2025, "category": "Fixture category", "outcome": "winner", "evidenceUrl": "https://fixture.invalid/synthetic-award", "contribution": "work"}]},
    {"workKey": series["WorkKey"], "facets": facets(["strong_opening", "complete_limited_story", "great_first_season", "low_cognitive_load", "episodic"]), "manifest": {"workKey": series["WorkKey"], "order": "aired", "finished": True, "complete": True, "source": "synthetic_fixture", "fetchedAt": now, "episodes": [{"season": e["ParentIndexNumber"], "number": e["IndexNumber"], "required": True, "airDate": "2024-01-01T00:00:00Z"} for e in episode_list]}}
]}
(root / "work/fixture/synthetic-evidence.json").write_text(json.dumps(doc, indent=2))
request("/DiscoveryRows/Evidence", "PUT", doc)
config = request("/Plugins/b87d3d47-7a2d-43c6-89d2-592885b90d3e/Configuration")
saved = json.loads(json.dumps(config))
for row in config["Rows"]:
    row["Enabled"] = True; row["Minimum"] = 1; row["Limit"] = 8
config["CoordinateRows"] = False
request("/DiscoveryRows/Configuration", "PUT", config, expected=204)
hss_id = "b8298e01-2697-407a-b44d-aa8dc795e850"
hss_saved = request("/Plugins/" + hss_id + "/Configuration")
hss_all = json.loads(json.dumps(hss_saved))
known = {s["SectionId"] for s in hss_all["SectionSettings"]}
for n, section in enumerate(ours, 20):
    if section["Section"] not in known:
        hss_all["SectionSettings"].append({"SectionId": section["Section"], "Enabled": True, "AllowUserOverride": True, "LowerLimit": 1, "UpperLimit": 1, "OrderIndex": n, "ViewMode": "Portrait", "HideWatchedItems": False, "PluginConfigurations": []})
# Exercise evidence shelves even when an existing fixture user's saved selection
# omits them. Restore the administrator's original HSS configuration afterward.
for section in hss_all["SectionSettings"]:
    if section["SectionId"].startswith("DiscoveryRows"):
        section["Enabled"] = True
        section["AllowUserOverride"] = False
request("/Plugins/" + hss_id + "/Configuration", "POST", hss_all, expected=204)
for section in ["DiscoveryRowsFeelGoodMovies", "DiscoveryRowsGoldenGlobes", "DiscoveryRowsOscars", "DiscoveryRowsStrongOpening", "DiscoveryRowsOneNight", "DiscoveryRowsCompleteSeries", "DiscoveryRowsFirstSeasons"]:
    items = result(section)
    if section == "DiscoveryRowsGoldenGlobes":
        check("unrelated award body cannot qualify Golden Globes", not items)
    else:
        check("evidence-backed live shelf " + section, bool(items))
        if section == "DiscoveryRowsFirstSeasons": check("first-season row returns season DTO", items[0]["Type"] == "Season")

request("/DiscoveryRows/Preferences", "POST", {"itemId": film["Id"], "hidden": True}, who="a", expected=204)
check("explicit user hide applies to all our rows", not result("DiscoveryRowsFeelGoodMovies"))
check("another user's preference unchanged", any(i["Id"] == film["Id"] for i in result("DiscoveryRowsFeelGoodMovies", "child")))
request("/DiscoveryRows/Preferences", "POST", {"itemId": film["Id"], "hidden": True}, who="b", expected=403)
request("/DiscoveryRows/Preferences/Restore", "POST", {"workKey": film["WorkKey"]}, who="a", expected=204)
check("restoring own preference re-enables title", bool(result("DiscoveryRowsFeelGoodMovies")))

settings = request("/Users/" + auth["a"]["id"])["Policy"]
blocked = json.loads(json.dumps(settings)); blocked["BlockedTags"] = ["fixture", "blocked"]
request("/Users/" + auth["a"]["id"] + "/Policy", "POST", blocked, expected=204)
check("same-session policy update invalidates warm recommendations", not result("DiscoveryRowsUnder90Movies"))
request("/Users/" + auth["a"]["id"] + "/Policy", "POST", settings, expected=204)

with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
    batches = list(pool.map(lambda _: result("DiscoveryRowsUnder90Movies"), range(20)))
check("parallel requests preserve deterministic order", all([i["Id"] for i in b] == [i["Id"] for i in batches[0]] for b in batches))
times = []
for _ in range(25):
    start = time.perf_counter(); result("DiscoveryRowsUnder90Movies"); times.append((time.perf_counter() - start) * 1000)
check("homepage calls did not invoke TMDB", request("/DiscoveryRows/Status")["providerRequests"] == status["providerRequests"])
request("/DiscoveryRows/Configuration", "PUT", saved, expected=204)
request("/Plugins/" + hss_id + "/Configuration", "POST", hss_saved, expected=204)
report = {"target": "Jellyfin 10.11.11 / HSS 3.0.2.0", "checks": checks, "passed": len(checks), "fixtureTitles": status["indexedTitles"],
          "warmHttpMedianMs": statistics.median(times), "warmHttpP95Ms": sorted(times)[int(.95 * len(times))],
          "data": "Synthetic fixture evidence only; never distributed as factual seed data."}
(root / "work/server-verification.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({k: v for k, v in report.items() if k != "checks"}, indent=2))
