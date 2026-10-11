#!/usr/bin/env python3
"""Verify one-place shelf selection against the disposable exact-version fixture."""
import copy
import hashlib
import http.client
import json
import pathlib
import subprocess
import time
import urllib.error
import urllib.request

root = pathlib.Path(__file__).resolve().parents[1]
base = 'http://127.0.0.1:18096'
container = 'discovery-rows-fixture-101111'
auth = json.loads((root / 'work/fixture/auth.json').read_text())
guid = 'b87d3d47-7a2d-43c6-89d2-592885b90d3e'
hss = 'b8298e01-2697-407a-b44d-aa8dc795e850'
checks = []

def request(path, method='GET', body=None, who='admin', expected=200):
    req = urllib.request.Request(base + path, method=method,
        data=json.dumps(body).encode() if body is not None else None,
        headers={'X-Emby-Token': auth[who]['token'], 'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req, timeout=40) as r:
            assert r.status == expected, (path, r.status)
            raw = r.read()
            return json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        if e.code != expected: raise

def check(name, value):
    assert value, name
    checks.append(name)
    print('PASS', name, flush=True)

def restart():
    subprocess.run(['docker', 'restart', container], check=True, capture_output=True)
    until = time.monotonic() + 60
    while time.monotonic() < until:
        try:
            s = request('/DiscoveryRows/Status')
            if s['registeredRows'] == 38 and s['indexedTitles'] == 20:
                return
        except (urllib.error.URLError, OSError, http.client.HTTPException):
            pass
        time.sleep(.25)
    raise TimeoutError('Disposable fixture did not become ready')

def private_hashes():
    directory = root / 'work/fixture/config/plugins/configurations/Jellyfin.Plugin.HomeScreenSections'
    return {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in directory.glob('*.json')}

inspection = json.loads(subprocess.check_output(['docker','inspect',container], text=True))[0]
assert all(m['Source'].startswith(str(root / 'work/fixture') + '/') for m in inspection['Mounts'])
info = request('/System/Info')
assert info['Version'] == '10.11.11' and info['ServerName'].startswith('Discovery Rows disposable')
original = request('/Plugins/' + guid + '/Configuration')
original_hss = request('/Plugins/' + hss + '/Configuration')
files_before = private_hashes()
assert files_before, 'Fixture must have existing saved personal settings to verify preservation.'
ids = {r['definition']['SectionId'] for r in request('/DiscoveryRows/Rows')}
try:
    request('/DiscoveryRows/HomepageSetup', who='a', expected=403)
    request('/DiscoveryRows/HomepageSetup', 'POST', {'allowUserSelection': False}, who='a', expected=403)
    check('ordinary users cannot inspect or change shared homepage setup', True)
    status = request('/DiscoveryRows/HomepageSetup', 'POST', {'allowUserSelection': False})
    check('one click enables and locks all 38 HSS Discovery shelves', status['Simplified'] and status['EnabledRows'] == 38 and status['LockedRows'] == 38)
    prepared = request('/Plugins/' + hss + '/Configuration')
    external = lambda c: [s for s in c['SectionSettings'] if s['SectionId'] not in ids]
    globals_only = lambda c: {k:v for k,v in c.items() if k != 'SectionSettings'}
    check('other HSS sections and global configuration remain unchanged', external(prepared) == external(original_hss) and globals_only(prepared) == globals_only(original_hss))
    check('setup leaves Discovery shelf configuration unchanged', request('/Plugins/' + guid + '/Configuration') == original)
    saved_rows = {s['SectionId']: s for s in original_hss['SectionSettings']}
    check('existing Discovery positions and styles are preserved', all(
        all(s.get(k) == saved_rows[s['SectionId']].get(k) for k in ['ViewMode','HideWatchedItems','PluginConfigurations'])
        and (saved_rows[s['SectionId']]['OrderIndex'] == 999 or s['OrderIndex'] == saved_rows[s['SectionId']]['OrderIndex'])
        for s in prepared['SectionSettings'] if s['SectionId'] in ids and s['SectionId'] in saved_rows))
    for who in ['admin','a','b','child']:
        effective = request('/ModularHomeViews/UserSettings?userId=' + auth[who]['id'], who=who)
        check(who + ' receives all Discovery shelves without a personal settings save', ids <= set(effective['EnabledSections']))
    check('setup never rewrites HSS private user settings files', private_hashes() == files_before)
    request('/DiscoveryRows/HomepageSetup', 'POST', {'allowUserSelection': False})
    check('repeating setup is idempotent', request('/Plugins/' + hss + '/Configuration') == prepared)
    personal = request('/DiscoveryRows/HomepageSetup', 'POST', {'allowUserSelection': True})
    check('personal selection remains an explicit option', personal['EnabledRows'] == 38 and personal['LockedRows'] == 0 and not personal['Simplified'])
    request('/DiscoveryRows/HomepageSetup', 'POST', {'allowUserSelection': False})
    restart()
    path = '/HomeScreen/Sections?userId=' + auth['admin']['id'] + '&language=en'
    layout = request(path)['Items']
    check('after one restart HSS layout includes all 38 Discovery shelves', ids <= {s['Section'] for s in layout})
    config = copy.deepcopy(original)
    config['CoordinateRows'] = False
    for row in config['Rows']:
        row['Enabled'] = row['Key'] == 'under90'
        if row['Key'] == 'under90': row['Minimum'] = 1
    request('/DiscoveryRows/Configuration', 'PUT', config, expected=204)
    cards = lambda section: request('/HomeScreen/Section/' + section + '?userId=' + auth['admin']['id'])['Items']
    check('only the enabled plugin shelf returns cards', bool(cards('DiscoveryRowsUnder90Movies')) and not cards('DiscoveryRows90MinuteComedies'))
    next(r for r in config['Rows'] if r['Key'] == 'under90')['Enabled'] = False
    request('/DiscoveryRows/Configuration', 'PUT', config, expected=204)
    check('plugin checkbox disables shelf on the same cached HSS layout', not cards('DiscoveryRowsUnder90Movies') and request(path)['Items'] == layout)
    next(r for r in config['Rows'] if r['Key'] == 'under90')['Enabled'] = True
    request('/DiscoveryRows/Configuration', 'PUT', config, expected=204)
    check('plugin checkbox restores shelf without another HSS or personal save', bool(cards('DiscoveryRowsUnder90Movies')) and request('/Plugins/' + hss + '/Configuration') == prepared and private_hashes() == files_before)
    status_after = request('/DiscoveryRows/HomepageSetup')
    check('setup status exposes readiness counts only', set(status_after) == {'Available','EnabledRows','LockedRows','TotalRows','Simplified'})
    (root / 'work/homepage-setup-verification.json').write_text(json.dumps({'version':request('/DiscoveryRows/Status')['pluginVersion'],'passed':len(checks),'checks':checks},indent=2) + '\n')
finally:
    request('/DiscoveryRows/Configuration', 'PUT', original, expected=204)
    request('/Plugins/' + hss + '/Configuration', 'POST', original_hss, expected=204)
    restart()
