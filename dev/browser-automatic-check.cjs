const {chromium}=require('../work/browser/node_modules/playwright-core');
const fs=require('node:fs');const assert=require('node:assert/strict');
(async()=>{
 const base='http://127.0.0.1:18096';const auth=JSON.parse(fs.readFileSync('work/fixture/auth.json','utf8')).admin;
 const headers={'X-Emby-Token':auth.token,'Content-Type':'application/json'};
 async function api(path,method='GET',body){const r=await fetch(base+path,{method,headers,body:body===undefined?undefined:JSON.stringify(body)});assert.ok(r.ok,path+' HTTP '+r.status);return r.status===204?null:r.json();}
 const info=await api('/System/Info');assert.equal(info.Version,'10.11.11');assert.ok(info.ServerName.startsWith('Discovery Rows disposable'));
 const guid='b87d3d47-7a2d-43c6-89d2-592885b90d3e',hss='b8298e01-2697-407a-b44d-aa8dc795e850';
 const original=await api('/Plugins/'+guid+'/Configuration'),originalHss=await api('/Plugins/'+hss+'/Configuration'),originalEvidence=await api('/DiscoveryRows/Evidence');
 const film=(await api('/DiscoveryRows/Library?userId='+auth.id)).items.find(i=>i.Name==='A Fixture Film 01');
 const originalItem=await api('/Items/'+film.Id+'?userId='+auth.id);
 const browser=await chromium.launch({executablePath:'/usr/bin/chromium',headless:true,args:['--no-sandbox']});const page=await browser.newPage({viewport:{width:1440,height:1100}});
 const errors=[];let providerCalls=0;page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(r.url().includes('api.themoviedb.org'))providerCalls++;});
 async function settings(){await page.goto(base+'/web/index.html#/configurationpage?name=discoveryrows');await page.locator('#drHssSetupStatus').filter({hasText:/shelves/}).waitFor();}
 try{
  await api('/Items/'+film.Id,'POST',{...originalItem,Genres:['Family','Comedy','Animation'],OfficialRating:'PG'});
  const data=structuredClone(originalEvidence.Items.find(w=>w.WorkKey===film.WorkKey));data.Facets=[];data.Awards=[];data.Keywords=['based on novel'];data.Excluded=false;
  await api('/DiscoveryRows/Evidence','PUT',{SchemaVersion:1,Items:[data]});
  const config=structuredClone(original);config.UseAutomaticMetadata=true;config.CoordinateRows=false;for(const r of config.Rows){r.Enabled=r.Key==='under90';r.Minimum=1;r.Limit=8;r.ExcludeStarted=false;}
  await api('/DiscoveryRows/Configuration','PUT',config);await api('/DiscoveryRows/HomepageSetup','POST',{allowUserSelection:false});
  await page.goto(base+'/web/index.html#/login');await page.locator('#txtManualName').fill('fixture-admin');await page.locator('#txtManualPassword').fill('Fixture-only-2026!');await page.getByRole('button',{name:'Sign In',exact:true}).click();
  await page.waitForURL(/#\/home$/);await page.locator('.DiscoveryRowsUnder90Movies .card').first().waitFor();await page.waitForLoadState('networkidle');
  await settings();assert.ok(await page.locator('#drAutomatic').isChecked());await page.screenshot({path:'work/screenshots/automatic-setup.png',fullPage:true});
  await page.getByRole('button',{name:'Shelves',exact:true}).click();
  await page.locator('#drShelfUser').selectOption({label:'fixture-admin'});
  await page.getByRole('button',{name:'Select shelves with enough matches',exact:true}).click();await page.locator('#drMessage').filter({hasText:'shelves selected with enough eligible titles'}).waitFor();
  assert.ok(await page.locator('.dr-row[data-key="family"] [data-field="Enabled"]').isChecked());assert.ok(await page.locator('.dr-row[data-key="books"] [data-field="Enabled"]').isChecked());assert.equal(await page.locator('.dr-row[data-key="oscars"] [data-field="Enabled"]').isChecked(),false);
  await page.getByRole('button',{name:'Save shelf settings',exact:true}).click();await page.locator('#drMessage').filter({hasText:'Settings saved.'}).waitFor();
  await page.screenshot({path:'work/screenshots/automatic-shelves.png',fullPage:true});
  await page.getByRole('button',{name:'Preview',exact:true}).click();await page.locator('#drPreviewUser').selectOption({label:'fixture-admin'});await page.locator('#drPreviewRow').selectOption('family');
  await page.getByRole('button',{name:'Inspect coverage',exact:true}).click();await page.locator('#drCoverage').filter({hasText:'Automatic metadata: Family genre'}).waitFor();await page.screenshot({path:'work/screenshots/automatic-preview.png',fullPage:true});
  await page.goto(base+'/web/index.html#/home');await page.locator('.DiscoveryRowsThemefamily .card').first().waitFor();await page.waitForLoadState('networkidle');await page.screenshot({path:'work/screenshots/automatic-home.png',fullPage:true});
  await settings();await page.getByRole('button',{name:'Evidence',exact:true}).click();await page.locator('#drEvidenceUser').selectOption({label:'fixture-admin'});await page.locator('#drSearch').fill('A Fixture Film 01');await page.getByRole('button',{name:'Find local titles',exact:true}).click();await page.locator('#drLibrary select').waitFor();await page.locator('#drLibrary select').selectOption('family');
  await page.getByRole('button',{name:'Exclude from selected shelf',exact:true}).click();await page.locator('#drMessage').filter({hasText:'Shelf exclusion saved'}).waitFor();
  let c=await api('/DiscoveryRows/Coverage?userId='+auth.id+'&row=family');assert.equal(c[0].candidates,0);
  await page.screenshot({path:'work/screenshots/automatic-corrections.png',fullPage:true});
  await page.getByRole('button',{name:'Reset to automatic',exact:true}).click();await page.locator('#drMessage').filter({hasText:'Shelf label reset to automatic'}).waitFor();
  c=await api('/DiscoveryRows/Coverage?userId='+auth.id+'&row=family');assert.ok(c[0].candidates>0);
  await page.getByRole('button',{name:'Include in selected shelf',exact:true}).click();await page.locator('#drMessage').filter({hasText:'Reviewed inclusion saved'}).waitFor();
  c=await api('/DiscoveryRows/Coverage?userId='+auth.id+'&row=family');assert.equal(c[0].examples[0].explanation.MatchSource,'reviewed_label');
  assert.deepEqual(errors,[]);assert.equal(providerCalls,0);
  fs.writeFileSync('work/browser-automatic-verification.json',JSON.stringify({version:(await api('/DiscoveryRows/Status')).pluginVersion,automaticDefault:true,selectMatchingShelves:true,emptyAwardShelfNotSelected:true,previewExplainsMatch:true,familyHomepageRenders:true,includeExcludeResetWithoutJson:true,providerCalls,browserErrors:errors},null,2)+'\n');
  console.log('Automatic browser checks passed: select matching shelves, Family Night without labels, explanations, Include/Exclude/Reset controls, no browser errors/provider calls.');
 }finally{await api('/Items/'+film.Id,'POST',originalItem);await api('/DiscoveryRows/Evidence','PUT',originalEvidence);await api('/DiscoveryRows/Configuration','PUT',original);await api('/Plugins/'+hss+'/Configuration','POST',originalHss);await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
