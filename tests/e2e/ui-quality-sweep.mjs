import { createRequire } from 'module';

const requireFromRunDirectory = createRequire(`${process.cwd()}/package.json`);
const { chromium } = requireFromRunDirectory('playwright');
const AxeBuilder = requireFromRunDirectory('@axe-core/playwright').default;

const baseURL = process.env.BASE_URL || 'http://localhost:8888';
const pages = [
  '/',
  '/xu-huong',
  '/huong-dan',
  '/lich-su',
  '/tich-hop-his',
  '/gioi-thieu',
  '/mien-tru-trach-nhiem',
  '/cau-hoi-thuong-gap',
  '/lien-he',
  '/chinh-sach-bao-mat',
  '/dang-nhap'
];
const viewports = [
  { name: 'mobile-small', width: 320, height: 720 },
  { name: 'mobile', width: 390, height: 844 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'laptop', width: 1024, height: 900 },
  { name: 'desktop', width: 1440, height: 1000 }
];

const browser = await chromium.launch({ headless: true });
const results = [];

for (const viewport of viewports) {
  const context = await browser.newContext({
    viewport,
    locale: 'vi-VN',
    extraHTTPHeaders: { 'Accept-Language': 'vi-VN,vi;q=0.9' }
  });

  for (const path of pages) {
    const page = await context.newPage();
    const consoleErrors = [];
    page.on('console', message => {
      if (message.type() === 'error') consoleErrors.push(message.text());
    });
    page.on('pageerror', error => consoleErrors.push(error.message));

    const response = await page.goto(`${baseURL}${path}`, { waitUntil: 'networkidle' });
    const dimensions = await page.evaluate(() => ({
      clientWidth: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth
    }));
    const violations = (await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag22aa'])
      .analyze()).violations.map(violation => ({
        id: violation.id,
        impact: violation.impact,
        targets: violation.nodes.map(node => node.target)
      }));

    results.push({
      viewport: viewport.name,
      path,
      status: response?.status(),
      overflow: dimensions.scrollWidth > dimensions.clientWidth + 1,
      dimensions,
      consoleErrors,
      violations
    });
    await page.close();
  }

  await context.close();
}

await browser.close();

const failed = results.filter(result =>
  (result.status || 0) >= 400 ||
  result.overflow ||
  result.consoleErrors.length > 0 ||
  result.violations.some(violation => ['critical', 'serious'].includes(violation.impact))
);
const report = { baseURL, passed: failed.length === 0, failed, results };
console.log(JSON.stringify(report, null, 2));
process.exit(report.passed ? 0 : 1);
