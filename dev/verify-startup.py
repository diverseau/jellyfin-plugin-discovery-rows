#!/usr/bin/env python3
"""Check the first homepage request on the stopped, disposable local fixture."""
import http.client
import json
import pathlib
import subprocess
import time
import urllib.error
import urllib.request

root = pathlib.Path(__file__).resolve().parents[1]
container = "discovery-rows-fixture-101111"
base = "http://127.0.0.1:18096"
auth = json.loads((root / "work/fixture/auth.json").read_text())["admin"]

def request(path):
    req = urllib.request.Request(base + path, headers={"X-Emby-Token": auth["token"]})
    with urllib.request.urlopen(req, timeout=40) as response:
        return json.load(response)

inspection = json.loads(subprocess.check_output(["docker", "inspect", container], text=True))[0]
assert not inspection["State"]["Running"], "Stop the disposable fixture first, so its first request is observable."
assert all(m["Source"].startswith(str(root / "work/fixture") + "/") for m in inspection["Mounts"]), "Unexpected fixture mount."
subprocess.run(["docker", "start", container], check=True, capture_output=True)
deadline = time.monotonic() + 60
while time.monotonic() < deadline:
    try:
        info = request("/System/Info")
        assert info["Version"] == "10.11.11" and info["ServerName"].startswith("Discovery Rows disposable")
        break
    except (urllib.error.URLError, OSError, http.client.HTTPException):
        time.sleep(.1)
else:
    raise TimeoutError("Fixture startup timed out")

status = request("/DiscoveryRows/Status")
assert status["registeredRows"] == 38, "Registration must precede the first available HTTP request."
path = "/HomeScreen/Sections?userId=" + auth["id"] + "&language=en"
initial = request(path)["Items"]
assert any(s["Section"] == "DiscoveryRowsUnder90Movies" for s in initial), "First layout omitted the selected discovery shelf."
deadline = time.monotonic() + 45
while time.monotonic() < deadline:
    status = request("/DiscoveryRows/Status")
    if status["indexedTitles"]:
        break
    time.sleep(.2)
else:
    raise TimeoutError("Fixture library index did not populate")

# Do not create a fresh page hash: the existing first-request cache must work.
cached = request(path)["Items"]
assert any(s["Section"] == "DiscoveryRowsUnder90Movies" for s in cached)
cards = request("/HomeScreen/Section/DiscoveryRowsUnder90Movies?userId=" + auth["id"])["Items"]
assert cards and all(i["Name"].startswith(("A ", "B ")) for i in cards), "Actual HSS callback must produce the administrator's eligible local titles."
result = {
    "pluginVersion": status["pluginVersion"], "firstRequestRegisteredRows": 38,
    "firstLayoutIncludesDiscovery": True, "sameCachedLayoutIncludesDiscovery": True,
    "homepageCallbackCards": len(cards), "indexedTitles": status["indexedTitles"],
    "labelsReady": status["labelsReady"]
}
(root / "work/startup-verification.json").write_text(json.dumps(result, indent=2) + "\n")
print("Startup regression checks passed:", json.dumps(result))
