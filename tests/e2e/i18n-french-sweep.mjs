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
  '/admin/login'
];

const vietnameseCharacters = /[ĂăĐđŨũƠơƯưẠ-ỹ]/;
const vietnameseWords = /\b(Thông tin|Đăng nhập|Đăng ký|Mật khẩu|Ngày sinh|Giờ sinh|Tuổi thai|Ngôn ngữ|Hướng dẫn|Tích hợp|Giới thiệu|Miễn trừ|Liên hệ|Chính sách|Quyền riêng tư|Bệnh viện|Bác sĩ|Công cụ|Tính toán|Lịch sử|Theo dõi|Vàng da|Sơ sinh|Nguy cơ|Chiếu đèn|Thay máu|Tài liệu|Câu hỏi|Gửi|Xác nhận|Quay lại|Dùng ẩn danh|Phiên bản)\b/i;
const englishWords = /\b(Information|Sign in|Sign In|Create Account|Password|Birth date|Birth time|Gestational age|Language|Guidelines|Integration|About|Disclaimer|Privacy Policy|Hospital|Doctor|Calculator|Calculation|History|Tracking|Jaundice|Newborn|Risk|Phototherapy|Exchange transfusion|Document|Question|Send|Confirm|Back|Anonymous|Forgot Password|Reset Password|Clinical Guidelines|Baby Tracking|Bilirubin Calculator)\b/i;
const allowedExactFragments = new Set([
  'BiliTool.Vn', 'AAP', 'NICE', 'GitHub', 'Google', 'HIS', 'EMR', 'API', 'JSON', 'URL',
  'G6PD', 'Coombs', 'DAT', 'IVIG', 'Sepsis', 'TcB', 'TSB', 'mg/dL', 'µmol/L', 'μmol/L', 'English', 'Français', 'Version', 'VERSION', 'Contact', 'Sepsis'
]);
const allowedLinePatterns = [
  /^Clinical Decision Support$/,
  /^AAP 2022$/,
  /^NICE CG98$/,
  /^X-API-Key/,
  /^Content-Type/,
  /^GET /,
  /^POST /,
  /^https?:\/\//,
  /^\{?\s*"?[A-Za-z0-9_]+"?\s*[:=]/,
  /^window\.open/,
  /^const /,
  /^url\./,
  /^\/api\//,
  /^\d+$/,
  /^\d+(\.\d+)?$/,
  /\bSepsis\b/,
  /\bContact\b/
];

function findLanguageLeaks(text) {
  return text
    .split(/\n+/)
    .map(line => line.replace(/\s+/g, ' ').trim())
    .filter(line => line && !allowedExactFragments.has(line))
    .filter(line => !allowedLinePatterns.some(pattern => pattern.test(line)))
    .filter(line => vietnameseCharacters.test(line) || vietnameseWords.test(line) || englishWords.test(line))
    .slice(0, 160);
}

async function switchToFrenchThroughUi(page, context) {
  await context.clearCookies();
  await page.goto(baseURL, { waitUntil: 'networkidle' });
  const acceptCookieButton = page.getByRole('button', { name: /Đồng ý|Accept|Accepter/i });
  if (await acceptCookieButton.isVisible().catch(() => false)) {
    await acceptCookieButton.click();
  }
  const languageButton = page.getByRole('button', { name: /Ngôn ngữ|Language|Langue/i });
  if (await languageButton.isVisible().catch(() => false)) {
    await languageButton.click();
    await page.getByRole('link', { name: /Français/i }).click();
    await page.waitForFunction(() => document.documentElement.lang === 'fr');
    await page.waitForLoadState('networkidle').catch(() => {});
    return;
  }

  await context.addCookies([{ name: '.AspNetCore.Culture', value: 'c=fr|uic=fr', url: baseURL, path: '/' }]);
  await page.reload({ waitUntil: 'networkidle' });
}

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  locale: 'fr-FR',
  extraHTTPHeaders: { 'Accept-Language': 'fr-FR,fr;q=0.9' }
});

const switchPage = await context.newPage();
await switchToFrenchThroughUi(switchPage, context);
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
  const bodyText = await page.locator('body').innerText().catch(() => '');
  const languageLeaks = findLanguageLeaks(`${title}\n${bodyText}`);

  results.push({ path, status: response?.status(), htmlLang, title, consoleErrors, languageLeaks });
  await page.close();
}

await browser.close();
const failed = results.filter(result =>
  (result.status || 0) >= 400 ||
  result.htmlLang !== 'fr' ||
  result.consoleErrors.length > 0 ||
  result.languageLeaks.length > 0
);

const report = { baseURL, passed: failed.length === 0, failed, results };
console.log(JSON.stringify(report, null, 2));
process.exit(report.passed ? 0 : 1);
