import assert from "node:assert/strict";

const baseUrl = process.env.BASE_URL ?? "https://bilitool.vn";
const pages = [
  { path: "/", schema: "SoftwareApplication" },
  { path: "/huong-dan", schema: "MedicalWebPage" },
  { path: "/cau-hoi-thuong-gap", schema: "FAQPage" },
  { path: "/gioi-thieu", schema: "Physician" },
];

for (const language of ["vi", "en", "fr"]) {
  for (const page of pages) {
    const url = `${baseUrl}${page.path}?culture=${language}&ui-culture=${language}`;
    const response = await fetch(url, { headers: { "Accept-Language": language } });
    assert.equal(response.status, 200, `${url} must return 200`);

    const html = await response.text();
    assert.equal((html.match(/<title\b/gi) ?? []).length, 1, `${url} must have one title`);
    assert.match(html, new RegExp(`<html[^>]+lang="${language}"`, "i"), `${url} must expose matching html lang`);
    assert.equal((html.match(/rel="canonical"/gi) ?? []).length, 1, `${url} must have one canonical`);

    for (const hreflang of ["vi", "en", "fr", "x-default"]) {
      assert.match(html, new RegExp(`hreflang="${hreflang}"`, "i"), `${url} missing ${hreflang}`);
    }

    assert.match(html, new RegExp(page.schema, "i"), `${url} missing ${page.schema} schema`);
    assert.equal((html.match(/<h1\b/gi) ?? []).length, 1, `${url} must have one h1`);
  }
}

console.log(`SEO/AEO smoke passed for ${pages.length * 3} localized pages.`);
