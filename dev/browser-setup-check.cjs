const { chromium } = require('../work/browser/node_modules/playwright-core');
const fs = require('node:fs');
const assert = require('node:assert/strict');
(async () => {
  const fixturePassword = process.env.DISCOVERY_FIXTURE_PASSWORD;
  assert.ok(fixturePassword, "Set DISCOVERY_FIXTURE_PASSWORD for the disposable fixture before running this check.");
  const base = 'http://127.0.0.1:18096';
  const auth = JSON.parse(fs.readFileSync('work/fixture/auth.json','utf8')).admin;
  const headers = { 'X-Emby-Token': auth.token, 'Content-Type':'application/json' };
  async function api(path, method='GET', body) {
    const r = await fetch(base+path,{method,headers,body:body===undefined?undefined:JSON.stringify(body)});
    assert.ok(r.ok, path+' HTTP '+r.status);
    return r.status===204?null:r.json();
  }
  const info = await api('/System/Info');
  assert.equal(info.Version,'10.11.11'); assert.ok(info.ServerName.startsWith('Discovery Rows disposable'));
  const guid='b87d3d47-7a2d-43c6-89d2-592885b90d3e', hss='b8298e01-2697-407a-b44d-aa8dc795e850';
  const original=await api('/Plugins/'+guid+'/Configuration');
  const originalHss=await api('/Plugins/'+hss+'/Configuration');
  const browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
  const page=await browser.newPage({viewport:{width:1440,height:1100}});
  const errors=[]; page.on('pageerror',e=>{errors.push(e.message);console.error('Browser error:',page.url(),e.stack);});
  async function settings() {await page.goto(base+'/web/index.html#/configurationpage?name=discoveryrows');await page.locator('#drHssSetupStatus').filter({hasText:/shelves/}).waitFor();}
  async function shelves() {await settings();await page.getByRole('button',{name:'Shelves',exact:true}).click();await page.locator('#drRows .dr-row').first().waitFor();}
  async function save() {await page.getByRole('button',{name:'Save shelf settings',exact:true}).click();await page.locator('#drMessage').filter({hasText:'Settings saved.'}).waitFor();}
  try {
    await page.goto(base+'/web/index.html#/login');
    await page.locator('#txtManualName').fill('fixture-admin');
    await page.locator('#txtManualPassword').fill(fixturePassword);
    await page.getByRole('button',{name:'Sign In',exact:true}).click();
    await page.waitForURL(/#\/home$/);
    await page.locator('.DiscoveryRowsUnder90Movies .card').first().waitFor();
    await page.waitForLoadState('networkidle');
    await settings();
    await page.locator('#drAllowUserSelection').uncheck();
    await page.getByRole('button',{name:'Enable all shelves in HSS',exact:true}).click();
    await page.locator('#drMessage').filter({hasText:'Restart Jellyfin once'}).waitFor();
    assert.equal((await api('/DiscoveryRows/HomepageSetup')).Simplified,true);
    await page.screenshot({path:'work/screenshots/one-place-setup.png',fullPage:true});
    await page.getByRole('button',{name:'Shelves',exact:true}).click();
    const sizes=await page.locator('#drRows input[type=number]').evaluateAll(es=>es.map(e=>e.value));
    const unstarted=await page.locator('#drRows [data-field="ExcludeStarted"]').evaluateAll(es=>es.map(e=>e.checked));
    await page.getByRole('button',{name:'Select all',exact:true}).click();
    assert.equal(await page.locator('#drRows [data-field="Enabled"]:checked').count(),38);
    assert.deepEqual((await api('/Plugins/'+guid+'/Configuration')).Rows,original.Rows,'Bulk controls need an explicit save');
    await save(); assert.ok((await api('/Plugins/'+guid+'/Configuration')).Rows.every(r=>r.Enabled));
    await page.getByRole('button',{name:'Clear selection',exact:true}).click();
    assert.equal(await page.locator('#drRows [data-field="Enabled"]:checked').count(),0);
    assert.deepEqual(await page.locator('#drRows input[type=number]').evaluateAll(es=>es.map(e=>e.value)),sizes);
    assert.deepEqual(await page.locator('#drRows [data-field="ExcludeStarted"]').evaluateAll(es=>es.map(e=>e.checked)),unstarted);
    await save(); assert.ok((await api('/Plugins/'+guid+'/Configuration')).Rows.every(r=>!r.Enabled));
    await page.goto(base+'/web/index.html#/home');
    await page.locator('.homeSectionsContainer').waitFor();
    await page.waitForLoadState('networkidle');
    await page.locator('.DiscoveryRowsUnder90Movies:visible').waitFor({state:'hidden'});
    await shelves();
    assert.equal(await page.locator('#drRows [data-field="Enabled"]:checked').count(),0,'Saved selection survives page reload');
    await page.locator('.dr-row[data-key="under90"] [data-field="Enabled"]').check();
    await save();
    await page.screenshot({path:'work/screenshots/one-place-shelves.png',fullPage:true});
    await page.goto(base+'/web/index.html#/home');
    await page.locator('.DiscoveryRowsUnder90Movies .card').first().waitFor();
    await page.waitForLoadState('networkidle');
    await page.screenshot({path:'work/screenshots/one-place-home.png',fullPage:true});
    assert.deepEqual(errors,[]);
    fs.writeFileSync('work/browser-setup-verification.json',JSON.stringify({version:(await api('/DiscoveryRows/Status')).pluginVersion,setupButton:true,sharedSelection:true,selectAll:38,clearSelection:0,explicitSave:true,otherShelfOptionsPreserved:true,savedSelectionReloads:true,disabledShelfHidden:true,enabledShelfReturns:true,browserErrors:errors},null,2)+'\n');
    console.log('Browser setup checks passed: one click, select all, clear, explicit save, unchanged sizes/filters, persistence, real homepage hide/show.');
  } finally {
    await api('/DiscoveryRows/Configuration','PUT',original);
    await api('/Plugins/'+hss+'/Configuration','POST',originalHss);
    await browser.close();
  }
})().catch(e=>{console.error(e);process.exit(1)});
