import { expect, test, type Page, type Route } from "@playwright/test";

test("report PDF response becomes a browser download", async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Deterministic download project");
  await mockWorkspace(page);
  await page.route("**/api/ekran/raporlar", (route) => json(route, {
    aktifIsletme: "Örnek İşletme",
    bugun: "2026-08-24",
    varsayilanDonem: "2026-08",
    formatlar: [{ deger: "zip", etiket: "ZIP", secili: true }],
    icerikler: [],
    yazdirmaSablonlari: [{ deger: "yoneticiOzeti", etiket: "Yönetici Özeti" }],
    tarihAraliklari: [{ deger: "monthly", etiket: "Aylık" }],
    sonPaket: null
  }));
  await page.route("**/api/ekran/raporlar/yazdir/pdf", (route) => route.fulfill({
    status: 200,
    contentType: "application/pdf",
    headers: { "Content-Disposition": "attachment; filename=systemcel-yonetici-ozeti.pdf" },
    body: "%PDF-1.4\n%%EOF\n"
  }));

  await page.goto("/app/raporlar");
  const downloadPromise = page.waitForEvent("download");
  await page.getByRole("button", { name: "PDF Kaydet" }).click();
  const download = await downloadPromise;

  expect(download.suggestedFilename()).toBe("systemcel-yonetici-ozeti.pdf");
});

async function mockWorkspace(page: Page) {
  await page.route("**/api/ekran/sube-kur/", route => json(route, {
    aktifSube: { id: 1, ad: "Merkez", aktif: true },
    subeler: [{ id: 1, ad: "Merkez", aktif: true }], cokluSubeAktif: false
  }));
  await page.route("**/api/public/config", (route) => json(route, {
    clerk: { enabled: true, publishableKey: "pk_test_ZXhhbXBsZS5jb20k", jsUrl: "/fake-clerk-report.js" }
  }));
  await page.route("**/fake-clerk-report.js", (route) => route.fulfill({
    status: 200,
    contentType: "text/javascript",
    body: `window.Clerk = {
      isSignedIn: true,
      user: { id: 'report-user', fullName: 'Rapor Kullanıcısı', primaryEmailAddress: { emailAddress: 'report@example.test' } },
      session: { getToken: async () => 'report-token' }, client: { signIn: {}, signUp: {} },
      load: async () => {}, setActive: async () => {}, addListener: () => () => {}, signOut: async () => {}
    };`
  }));
  await page.route("**/api/ekran/ust-bar", (route) => json(route, {
    aktifIsletmeId: 42, aktifIsletme: "Örnek İşletme", hesapTipi: "Isletme",
    muhasebeciMusteriBaglami: false, muhasebeciAdi: "", muhasebeciYetkiSeviyesi: "Tam",
    bildirimVar: false, bildirimSayisi: 0, sohbet: { okunmamisMesajSayisi: 0, sohbetler: [] },
    telegramAktif: false, isletmeler: [{ id: 42, ad: "Örnek İşletme", aktif: true }]
  }));
  await page.route("**/api/ekran/kolay-kurulum", (route) => json(route, {
    tamamlandi: true, isletmeId: 42, isletmeAdi: "Örnek İşletme", hesapTipi: "Isletme",
    isletmeTuru: "Genel", konum: "İstanbul", muhasebeciVarMi: false, mesaj: "", turler: []
  }));
}

async function json(route: Route, body: unknown) {
  await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(body) });
}

test("report dates use the local calendar and refresh errors recover", async ({ browser }, testInfo) => {
  test.skip(testInfo.project.name !== "desktop-chromium", "Local-calendar and recovery regression");
  const context = await browser.newContext({
    baseURL: "http://127.0.0.1:4173", timezoneId: "Europe/Istanbul",
    viewport: { width: 1366, height: 768 }
  });
  const page = await context.newPage();
  try {
    await page.clock.install({ time: new Date("2026-09-01T02:00:00+03:00") });
    await mockWorkspace(page);
    let failRefresh = false;
    await page.route("**/api/ekran/raporlar", route => failRefresh
      ? route.fulfill({ status: 503, contentType: "application/json", body: JSON.stringify({ mesaj: "Bağlantı kurulamadı." }) })
      : json(route, {
        aktifIsletme: "Örnek İşletme", bugun: "2026-09-01", varsayilanDonem: "2026-09",
        formatlar: [{ deger: "zip", etiket: "ZIP", secili: true }], icerikler: [],
        yazdirmaSablonlari: [{ deger: "yoneticiOzeti", etiket: "Yönetici Özeti" }],
        tarihAraliklari: [{ deger: "monthly", etiket: "Aylık" }], sonPaket: null
      }));
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.goto("/app/raporlar");
    const refresh = page.getByRole("button", { name: "Raporları yenile", exact: true });
    await expect(refresh).toBeEnabled();
    await expect(page.getByLabel("Başlangıç", { exact: true })).toHaveValue("2026-09-01");
    await expect(page.getByLabel("Bitiş", { exact: true })).toHaveValue("2026-09-01");
    await expect(page.getByRole("combobox", { name: "Dönem Ay" })).toHaveValue("09");
    for (const theme of ["light", "dark"]) {
      await page.evaluate(value => { document.documentElement.dataset.theme = value; }, theme);
      failRefresh = true;
      await refresh.click();
      await expect(page.locator(".reports-feedback[role=alert]")).not.toBeEmpty();
      await expect(page.getByText("Raporlar yükleniyor...", { exact: true })).toHaveCount(0);
      await expect(refresh).toBeEnabled();
      await page.screenshot({ path: testInfo.outputPath(`report-recovery-${theme}.png`), fullPage: true });
      failRefresh = false;
      await refresh.click();
      await expect(page.locator(".reports-feedback[role=alert]")).toHaveCount(0);
      await expect(refresh).toBeEnabled();
    }
    expect(errors).toEqual([]);
  } finally {
    await context.close();
  }
});
