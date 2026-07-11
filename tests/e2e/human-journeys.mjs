import assert from 'node:assert/strict';
import { createRequire } from 'module';

const requireFromRunDirectory = createRequire(`${process.cwd()}/package.json`);
const { chromium, firefox, webkit } = requireFromRunDirectory('playwright');

const baseURL = process.env.BASE_URL || 'https://bilitool.vn';
const browserFactories = { chromium, firefox };
const unavailableBrowsers = ['webkit'];
const journeyResults = [];

async function acceptCookies(page) {
  const accept = page.getByRole('button', { name: /Accept & Continue|Đồng ý|Accepter/i });
  if (await accept.isVisible().catch(() => false)) {
    await accept.click();
    await assert.rejects(async () => accept.waitFor({ state: 'visible', timeout: 500 }), /Timeout/).catch(() => {});
  }
}

async function runJourney(browserName, journeyName, action) {
  const browser = await browserFactories[browserName].launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    locale: 'en-US',
    extraHTTPHeaders: { 'Accept-Language': 'en-US,en;q=0.9' }
  });
  const page = await context.newPage();
  const errors = [];
  page.on('console', message => {
    if (message.type() === 'error' && !message.text().includes('WebSocket closed with status code: 1006')) errors.push(message.text());
  });
  page.on('pageerror', error => errors.push(error.message));
  try {
    await action(page, context);
    assert.deepEqual(errors, [], `console errors: ${errors.join(' | ')}`);
    journeyResults.push({ browser: browserName, journey: journeyName, passed: true });
  } catch (error) {
    journeyResults.push({ browser: browserName, journey: journeyName, passed: false, error: error.message });
  } finally {
    await context.close();
    await browser.close();
  }
}

async function navigationAndCookie(page) {
  await page.goto(`${baseURL}/`, { waitUntil: 'networkidle' });
  await assert.doesNotReject(() => page.getByRole('region', { name: /Cookie & Privacy Policy/i }).waitFor());
  await acceptCookies(page);
  await page.locator('.mobile-menu-btn').click();
  await page.waitForFunction(() => document.querySelector('.mobile-menu-btn')?.getAttribute('aria-expanded') === 'true');
  await page.getByRole('link', { name: /Clinical Guidelines/i }).click();
  await assert.strictEqual(new URL(page.url()).pathname, '/huong-dan');
  await assert.ok(await page.getByRole('heading', { level: 1 }).isVisible());
}

async function languageSwitch(page, context) {
  await page.goto(`${baseURL}/`, { waitUntil: 'networkidle' });
  await acceptCookies(page);
  await page.locator('.mobile-menu-btn').click();
  await page.getByRole('button', { name: /Language/i }).click();
  await page.getByRole('link', { name: /Français/i }).click();
  await page.waitForLoadState('networkidle').catch(() => {});
  await page.waitForFunction(() => document.documentElement.lang === 'fr');
  await page.waitForFunction(() => /Calculateur|Bilirubine/.test(document.title));
  assert.match(await page.title(), /Calculateur|Bilirubine/);
  await context.clearCookies();
  await page.goto(`${baseURL}/`, { waitUntil: 'networkidle' });
  await page.locator('.mobile-menu-btn').click();
  await page.getByRole('button', { name: /Language|Langue|Ngôn ngữ/i }).click();
  await page.getByRole('link', { name: /English/i }).click();
  await page.waitForLoadState('networkidle').catch(() => {});
  await page.waitForFunction(() => document.documentElement.lang === 'en');
  await page.waitForFunction(() => /Calculator|Bilirubin/.test(document.title));
  assert.match(await page.title(), /Calculator|Bilirubin/);
}

async function calculatorFlow(page) {
  await page.goto(`${baseURL}/`, { waitUntil: 'networkidle' });
  await acceptCookies(page);
  await page.getByRole('button', { name: 'Enter Age (hours)' }).click();
  const age = page.getByPlaceholder('1 – 672');
  const bilirubin = page.getByPlaceholder('Enter value...');
  await age.click();
  await page.keyboard.type('48');
  await age.press('Tab');
  await bilirubin.fill('12');
  await bilirubin.press('Tab');
  await page.getByLabel('Unit').selectOption('MgDl');
  await page.getByLabel('Gestational Age').selectOption('40');
  await page.getByRole('button', { name: /Calculate Bilirubin Thresholds/i }).click();
  await page.getByText(/CLINICAL RESULT|KẾT QUẢ LÂM SÀNG|RÉSULTAT CLINIQUE/i).waitFor();
  assert.match(await page.locator('body').innerText(), /NORMAL|PHOTOTHERAPY|AAP 2022|NICE CG98/);
}

async function deepLinkFlow(page) {
  const query = 'ngaysinh=09/07/2026&giosinh=01:25&ngaymau=10/07/2026&giomau=09:25&bili=12&donvi=UmolL&tuoithai=40&autocalc=true';
  await page.goto(`${baseURL}/may-tinh?${query}`, { waitUntil: 'networkidle' });
  await page.getByText(/CLINICAL RESULT|KẾT QUẢ LÂM SÀNG|RÉSULTAT CLINIQUE/i).waitFor();
  assert.match(await page.locator('body').innerText(), /NORMAL|PHOTOTHERAPY|AAP 2022|NICE CG98/);
}

async function keyboardFlow(page) {
  await page.goto(`${baseURL}/`, { waitUntil: 'networkidle' });
  await acceptCookies(page);
  await page.keyboard.press('Tab');
  const firstFocus = await page.evaluate(() => document.activeElement?.getAttribute('aria-label') || document.activeElement?.textContent?.trim());
  assert.ok(firstFocus, 'keyboard focus did not land on an interactive element');
  await page.locator('.mobile-menu-btn').focus();
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => document.querySelector('.mobile-menu-btn')?.getAttribute('aria-expanded') === 'true');
}

for (const browserName of Object.keys(browserFactories)) {
  await runJourney(browserName, 'navigation-and-cookie', navigationAndCookie);
  if (browserName === 'chromium') {
    await runJourney(browserName, 'language-switch', languageSwitch);
  }
  await runJourney(browserName, 'calculator-flow', calculatorFlow);
  await runJourney(browserName, 'deep-link-autocalc', deepLinkFlow);
  await runJourney(browserName, 'keyboard-navigation', keyboardFlow);
}

const failed = journeyResults.filter(result => !result.passed);
const report = { baseURL, passed: failed.length === 0, total: journeyResults.length, skipped: unavailableBrowsers.map(browser => ({ browser, reason: 'Linux WebKit graphics driver unavailable in this runner' })), failed, results: journeyResults };
console.log(JSON.stringify(report, null, 2));
process.exit(report.passed ? 0 : 1);
