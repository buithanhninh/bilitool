import { createRequire } from 'module';

const requireFromRunDirectory = createRequire(`${process.cwd()}/package.json`);
const { chromium } = requireFromRunDirectory('playwright');

const baseURL = process.env.BASE_URL || 'http://localhost:8888';
const publicPages = [
  '/',
  '/may-tinh',
  '/xu-huong',
  '/huong-dan',
  '/lich-su',
  '/tich-hop-his',
  '/gioi-thieu',
  '/mien-tru-trach-nhiem',
  '/cau-hoi-thuong-gap',
  '/lien-he',
  '/chinh-sach-bao-mat',
  '/dang-nhap',
  '/dang-ky',
  '/quen-mat-khau',
  '/dat-lai-mat-khau',
  '/admin/login',
  '/may-tinh?ngaysinh=09/07/2026&giosinh=01:25&ngaymau=10/07/2026&giomau=09:25&bili=12&donvi=UmolL&tuoithai=43&autocalc=true'
];

const vietnameseCharacters = /[ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯưẠ-ỹ]/;
const vietnameseWords = /\b(Thông tin|Đăng nhập|Đăng ký|Mật khẩu|Ngày sinh|Giờ sinh|Tuổi thai|Ngôn ngữ|Hướng dẫn|Tích hợp|Giới thiệu|Miễn trừ|Liên hệ|Chính sách|Quyền riêng tư|Bệnh viện|Bác sĩ|Công cụ|Tính toán|Lịch sử|Theo dõi|Vàng da|Sơ sinh|Nguy cơ|Chiếu đèn|Thay máu|Tài liệu|Câu hỏi|Gửi|Xác nhận|Quay lại|Dùng ẩn danh|Phiên bản)\b/i;
const allowedExactFragments = new Set([
  'BiliTool.Vn', 'AAP', 'NICE', 'GitHub', 'Google', 'HIS', 'EMR', 'API', 'JSON', 'URL',
  'G6PD', 'Coombs', 'DAT', 'IVIG', 'TcB', 'TSB', 'mg/dL', 'µmol/L', 'μmol/L'
]);

function findVietnameseLeaks(text) {
  return text
    .split(/\n+/)
    .map(line => line.replace(/\s+/g, ' ').trim())
    .filter(line => line && !allowedExactFragments.has(line))
    .filter(line => vietnameseCharacters.test(line) || vietnameseWords.test(line))
    .slice(0, 120);
}

async function switchToEnglishThroughUi(page, context) {
  await context.clearCookies();
  await page.goto(baseURL, { waitUntil: 'networkidle' });
  const acceptCookieButton = page.getByRole('button', { name: /Đồng ý|Accept/i });
  if (await acceptCookieButton.isVisible().catch(() => false)) {
    await acceptCookieButton.click();
  }
  const languageButton = page.getByRole('button', { name: /Ngôn ngữ|Language/i });
  if (await languageButton.isVisible().catch(() => false)) {
    await languageButton.click();
    await page.getByRole('link', { name: /English/i }).click();
    await page.waitForFunction(() => document.documentElement.lang === 'en');
    await page.waitForLoadState('networkidle').catch(() => {});
    return;
  }

  await context.addCookies([{ name: '.AspNetCore.Culture', value: 'c=en|uic=en', url: baseURL, path: '/' }]);
  await page.reload({ waitUntil: 'networkidle' });
}

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  locale: 'en-US',
  extraHTTPHeaders: { 'Accept-Language': 'en-US,en;q=0.9' }
});

const switchPage = await context.newPage();
await switchToEnglishThroughUi(switchPage, context);
await switchPage.close();

const results = [];
for (const path of publicPages) {
  const page = await context.newPage();
  const consoleErrors = [];
  page.on('console', message => {
    if (message.type() === 'error') consoleErrors.push(message.text());
  });
  page.on('pageerror', error => consoleErrors.push(error.message));

  const response = await page.goto(`${baseURL}${path}`, { waitUntil: 'networkidle' });
  const title = await page.title();
  const htmlLang = await page.locator('html').getAttribute('lang').catch(() => null);
  // Full visible text audit needs the rendered body text, not an implementation selector for interaction.
  const bodyText = await page.locator('body').innerText().catch(() => '');
  const languageLeaks = findVietnameseLeaks(`${title}\n${bodyText}`);

  results.push({ path, status: response?.status(), htmlLang, title, consoleErrors, languageLeaks });
  await page.close();
}

await browser.close();
const failed = results.filter(result =>
  (result.status || 0) >= 400 ||
  result.htmlLang !== 'en' ||
  result.consoleErrors.length > 0 ||
  result.languageLeaks.length > 0
);

const report = { baseURL, passed: failed.length === 0, failed, results };
console.log(JSON.stringify(report, null, 2));
process.exit(report.passed ? 0 : 1);
