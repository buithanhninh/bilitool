import { chromium } from 'playwright';

const baseUrl = process.env.BASE_URL || 'https://bilitool.vn';
const username = process.env.ADMIN_USERNAME;
const password = process.env.ADMIN_PASSWORD;
if (!username || !password) throw new Error('Set ADMIN_USERNAME and ADMIN_PASSWORD');

const browser = await chromium.launch({ headless: true });
try {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
  const page = await context.newPage();
  await page.goto(`${baseUrl}/admin/login`, { waitUntil: 'networkidle' });
  await page.locator('input[name="Username"]').fill(username);
  await page.locator('input[name="Password"]').fill(password);
  await Promise.all([
    page.waitForURL(/\/admin$/),
    page.locator('button[type="submit"], input[type="submit"]').click()
  ]);
  for (const viewport of [{ width: 1440, height: 1100 }, { width: 390, height: 844 }]) {
    await page.setViewportSize(viewport);
    await page.goto(`${baseUrl}/admin`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);

    const dashboard = page.locator('main, body').first();
    if (!(await dashboard.getByText('Trung tâm điều hành lâm sàng').count())) throw new Error('Dashboard title missing');
    if (!(await page.locator('canvas[role="img"]').count())) throw new Error('Chart text alternatives missing');
    if (!(await page.locator('h1').count())) throw new Error('Primary heading missing');

    await page.keyboard.press('Tab');
    const focusedTag = await page.evaluate(() => document.activeElement?.tagName);
    if (!focusedTag || focusedTag === 'BODY') throw new Error('Keyboard focus lost');
    console.log(`PASS accessibility viewport ${viewport.width}x${viewport.height}`);
  }
  await context.close();
} finally {
  await browser.close();
}
