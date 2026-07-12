import { chromium, firefox, webkit } from 'playwright';
import assert from 'node:assert/strict';

const baseUrl = 'https://bilitool.vn';
const username = process.env.ADMIN_USERNAME;
const password = process.env.ADMIN_PASSWORD;
if (!username || !password) throw new Error('Set ADMIN_USERNAME and ADMIN_PASSWORD');
const viewports = [
  ['mobile-small', { width: 360, height: 800 }],
  ['mobile-large', { width: 430, height: 932 }],
  ['tablet', { width: 768, height: 1024 }],
  ['desktop', { width: 1440, height: 1100 }]
];
const engines = [['chromium', chromium], ['firefox', firefox], ['webkit', webkit]];
const failures = [];

const loginBrowser = await chromium.launch({ headless: true });
const loginContext = await loginBrowser.newContext({ viewport: { width: 1440, height: 1100 } });
const loginPage = await loginContext.newPage();
await loginPage.goto(`${baseUrl}/admin/login`, { waitUntil: 'networkidle' });
await loginPage.locator('input[name="Username"]').fill(username);
await loginPage.locator('input[name="Password"]').fill(password);
await Promise.all([loginPage.waitForURL(/\/admin$/), loginPage.getByRole('button').click()]);
await loginPage.getByRole('heading', { name: 'Trung tâm điều hành lâm sàng' }).waitFor();
const storageState = await loginContext.storageState();
await loginBrowser.close();

for (const [engineName, launcher] of engines) {
  const browser = await launcher.launch({ headless: true });
  for (const [viewportName, viewport] of viewports) {
    const context = await browser.newContext({ viewport, storageState });
    const page = await context.newPage();
    const consoleErrors = [];
    const serverErrors = [];
    page.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
    page.on('response', response => { if (response.status() >= 500) serverErrors.push(`${response.status()} ${response.url()}`); });
    try {
      await page.goto(`${baseUrl}/admin`, { waitUntil: 'networkidle' });
      await page.getByRole('heading', { name: 'Trung tâm điều hành lâm sàng' }).waitFor();
      await page.getByText('API p95 / lỗi 15 phút').waitFor();
      const body = await page.locator('body').innerText();
      for (const text of ['Escalation of care', 'Không đủ dữ liệu', 'API p95 / lỗi 15 phút']) assert.ok(body.includes(text), `Missing ${text}`);
      assert.match(body, /\d+ ẩn danh · \d+ đã đăng nhập \/ 30 ngày/, 'Activity split missing');
      assert.ok(!body.includes('Test Automation Delete'), 'Test data visible');
      assert.ok(!body.includes('Thß╗'), 'Mojibake visible');
      assert.ok(await page.locator('h1').count() === 1, 'Expected exactly one h1');
      assert.ok(await page.locator('canvas[role="img"]').count() >= 3, 'Chart alternatives missing');
      await page.keyboard.press('Tab');
      assert.notEqual(await page.evaluate(() => document.activeElement?.tagName), 'BODY', 'Keyboard focus lost');
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
      assert.ok(overflow <= 2, `Horizontal overflow ${overflow}px`);
      for (const path of ['/admin/tai-khoan', '/admin/benh-nhan', '/admin/nhat-ky', '/admin/thong-ke/bac-si', '/admin/thong-ke/benh-nhan']) {
        await page.goto(`${baseUrl}${path}`, { waitUntil: 'networkidle' });
        assert.ok(!page.url().includes('/admin/login'), `Redirected to login: ${path}`);
      }
      assert.equal(serverErrors.length, 0, `Server errors: ${serverErrors.join(', ')}`);
      assert.equal(consoleErrors.length, 0, `Console errors: ${consoleErrors.join(', ')}`);
      console.log(`PASS ${engineName}/${viewportName}`);
    } catch (error) {
      failures.push(`${engineName}/${viewportName}: ${error.message}`);
      await page.screenshot({ path: `/tmp/bilitool-live/failure-${engineName}-${viewportName}.png`, fullPage: true });
      console.error(`FAIL ${engineName}/${viewportName}: ${error.message}`);
    } finally { await context.close(); }
  }
  await browser.close();
}
if (failures.length) throw new Error(`\n${failures.join('\n')}`);
