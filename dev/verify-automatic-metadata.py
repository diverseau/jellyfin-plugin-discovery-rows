#!/usr/bin/env python3
"""Exercise automatic shelves with synthetic metadata on the disposable fixture only."""
import copy
import http.client
import json
import pathlib
import subprocess
import time
import urllib.error
import urllib.request

root=pathlib.Path(__file__).resolve().parents[1]
base='http://127.0.0.1:18096'
auth=json.loads((root/'work/fixture/auth.json').read_text())
guid='b87d3d47-7a2d-43c6-89d2-592885b90d3e'
hss='b8298e01-2697-407a-b44d-aa8dc795e850'
checks=[]

def request(path,method='GET',body=None,who='admin',expected=200):
    req=urllib.request.Request(base+path,method=method,data=json.dumps(body).encode() if body is not None else None,
        headers={'X-Emby-Token':auth[who]['token'],'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(req,timeout=40) as r:
            assert r.status==expected,(path,r.status)
            raw=r.read();return json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        if e.code!=expected:raise

def check(name,value):
    assert value,name;checks.append(name);print('PASS',name,flush=True)

info=request('/System/Info')
assert info['Version']=='10.11.11' and info['ServerName'].startswith('Discovery Rows disposable')
inspection=json.loads(subprocess.check_output(['docker','inspect','discovery-rows-fixture-101111'],text=True))[0]
assert all(m['Source'].startswith(str(root/'work/fixture')+'/') for m in inspection['Mounts'])
original=request('/Plugins/'+guid+'/Configuration')
original_hss=request('/Plugins/'+hss+'/Configuration')
original_evidence=request('/DiscoveryRows/Evidence')
film=next(i for i in request('/DiscoveryRows/Library?userId='+auth['admin']['id'])['items'] if i['Name']=='A Fixture Film 01')
original_item=request('/Items/'+film['Id']+'?userId='+auth['admin']['id'])
work=next(w for w in original_evidence['Items'] if w['WorkKey']==film['WorkKey'])
status=request('/DiscoveryRows/Status')
try:
    check('existing configuration enables automatic mode on upgrade',original['UseAutomaticMetadata'])
    modified=copy.deepcopy(original_item);modified['Genres']=['Family','Comedy','Animation'];modified['OfficialRating']='PG';modified['ProductionYear']=1985
    request('/Items/'+film['Id'],'POST',modified,expected=204)
    data=copy.deepcopy(work);data['Facets']=[];data['Awards']=[];data['Keywords']=['based on novel','witchcraft'];data['Excluded']=False
    request('/DiscoveryRows/Evidence','PUT',{'SchemaVersion':1,'Items':[data]})
    config=copy.deepcopy(original);config['UseAutomaticMetadata']=True;config['CoordinateRows']=False
    for r in config['Rows']:
        r['Enabled']=r['Key'] in ['family','books','witchcraft','nostalgia80'];r['Minimum']=1;r['Limit']=8;r['ExcludeStarted']=False
    request('/DiscoveryRows/Configuration','PUT',config,expected=204)
    request('/DiscoveryRows/HomepageSetup','POST',{'allowUserSelection':False})
    uid=auth['admin']['id']
    def coverage(row):return request('/DiscoveryRows/Coverage?userId='+uid+'&row='+row)[0]
    def cards(section,who='admin'):return request('/HomeScreen/Section/'+section+'?userId='+auth[who]['id'],who=who)['Items']
    for key,section in [('family','DiscoveryRowsThemefamily'),('books','DiscoveryRowsThemebooks'),('witchcraft','DiscoveryRowsThemewitchcraft'),('nostalgia80','DiscoveryRowsThemenostalgia80')]:
        c=coverage(key)
        check(key+' matches existing genres/year or keywords without reviewed facets',any(e['id']==film['Id'] and e['explanation']['MatchSource']=='automatic_metadata' for e in c['examples']))
        check(key+' returns actual HSS cards',any(i['Id']==film['Id'] for i in cards(section)))
    check('automatic matching never writes reviewed labels',not next(w for w in request('/DiscoveryRows/Evidence')['Items'] if w['WorkKey']==film['WorkKey'])['Facets'])
    check('automatic shelves respect separate library access',not any(i['Id']==film['Id'] for i in cards('DiscoveryRowsThemefamily','b')))
    request('/DiscoveryRows/Availability?userId='+uid,who='a',expected=403)
    request('/DiscoveryRows/FacetOverride','POST',{'workKey':film['WorkKey'],'row':'family','match':False},who='a',expected=403)
    check('availability and shared corrections are administrator-only',True)
    request('/DiscoveryRows/FacetOverride','POST',{'workKey':film['WorkKey'],'row':'oscars','match':True},expected=400)
    check('facet corrections cannot fabricate award records',True)
    availability=request('/DiscoveryRows/Availability?userId='+uid)
    check('availability can discover disabled shelves without changing configuration',next(r for r in availability if r['key']=='laughmovies')['candidates']>0 and request('/Plugins/'+guid+'/Configuration')==config)
    request('/DiscoveryRows/FacetOverride','POST',{'workKey':film['WorkKey'],'row':'family','match':False},expected=204)
    check('simple shelf correction immediately overrides a warm automatic result',not any(i['Id']==film['Id'] for i in cards('DiscoveryRowsThemefamily')))
    check('shelf-specific correction leaves other shelves intact',any(i['Id']==film['Id'] for i in cards('DiscoveryRowsThemebooks')))
    request('/DiscoveryRows/FacetOverride','POST',{'workKey':film['WorkKey'],'row':'family','match':None},expected=204)
    check('reset restores automatic matching without JSON editing',any(i['Id']==film['Id'] for i in cards('DiscoveryRowsThemefamily')))
    config['UseAutomaticMetadata']=False;request('/DiscoveryRows/Configuration','PUT',config,expected=204)
    check('strict reviewed-only mode remains available',not cards('DiscoveryRowsThemefamily'))
    config['UseAutomaticMetadata']=True;request('/DiscoveryRows/Configuration','PUT',config,expected=204)
    mature=copy.deepcopy(modified);mature['OfficialRating']='R';request('/Items/'+film['Id'],'POST',mature,expected=204)
    check('fresh metadata revalidation removes a newly mature Family Night candidate',not any(i['Id']==film['Id'] for i in cards('DiscoveryRowsThemefamily')))
    request('/Items/'+film['Id'],'POST',modified,expected=204)
    check('homepage and preview matching perform zero provider calls',request('/DiscoveryRows/Status')['providerRequests']==status['providerRequests'])
    (root/'work/automatic-metadata-verification.json').write_text(json.dumps({'version':status['pluginVersion'],'passed':len(checks),'checks':checks,'data':'Synthetic fixture metadata, not real-library classification accuracy.'},indent=2)+'\n')
finally:
    request('/Items/'+film['Id'],'POST',original_item,expected=204)
    request('/DiscoveryRows/Evidence','PUT',original_evidence)
    request('/DiscoveryRows/Configuration','PUT',original,expected=204)
    request('/Plugins/'+hss+'/Configuration','POST',original_hss,expected=204)
