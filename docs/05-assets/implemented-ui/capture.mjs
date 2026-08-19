/**
 * Captures implemented web screens into this folder.
 * Requires the Next.js app on http://localhost:3000.
 */
import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const outDir = process.env.CAPTURE_OUT_DIR ?? dirname(fileURLToPath(import.meta.url));
const base = process.env.CAPTURE_BASE_URL ?? "http://localhost:3000";

const product = {
  id: "p-ultra-soft",
  code: "PS-033",
  slug: "ultra-soft",
  name: "Ultra Soft Peçete",
  description: "2 katlı peçete. Public katalogda stok ve fiyat gösterilmez.",
  sizeLabel: "33x33",
  categoryCode: "napkin",
  categoryName: "Peçete",
  baseUom: { code: "ADT", displayName: "Adet", dimension: "count", decimalScale: 0 },
  packagings: [
    {
      id: "pkg-case",
      level: "Case",
      name: "Koli",
      quantityInBaseUom: 2000,
      isSellable: true,
      allowPartial: false,
      effectiveVersion: "v1",
    },
  ],
  primaryImageUrl: null,
};

const products = [
  product,
  {
    ...product,
    id: "p-dispenser",
    code: "DP-200",
    slug: "dispenser-pecete",
    name: "Dispenser Peçete",
    sizeLabel: "21x21",
  },
  {
    ...product,
    id: "p-havlu",
    code: "HV-150",
    slug: "kati-havlu",
    name: "Kâğıt Havlu",
    categoryCode: "towel",
    categoryName: "Havlu",
    sizeLabel: "23x23",
  },
];

const session = {
  user: {
    id: "u-admin",
    userName: "admin",
    displayName: "Yusuf Kaya",
    roles: ["admin"],
    permissions: ["quote-request.read", "system.read"],
  },
};

const quotes = [
  {
    id: "qr-1",
    requestNumber: "TLT-2026-0001",
    status: "Received",
    source: "Public",
    candidateName: "Acme / Ali Veli",
    candidateEmail: "a@b.com",
    candidatePhone: "555",
    createdAt: "2026-08-19T10:00:00Z",
    items: [{ id: "l1", productId: "p-ultra-soft", enteredQuantity: 5, enteredPackagingId: "pkg-case" }],
  },
  {
    id: "qr-2",
    requestNumber: "TLT-2026-0002",
    status: "InReview",
    source: "Public",
    candidateName: "Beta Gıda / Ayşe Demir",
    createdAt: "2026-08-18T14:32:00Z",
    items: [{ id: "l2" }, { id: "l3" }],
  },
];

async function fulfillJson(route, body, status = 200) {
  await route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

async function installMocks(page) {
  await page.route("**/api/**", async (route) => {
    const url = new URL(route.request().url());
    const path = url.pathname;
    const method = route.request().method();

    if (path === "/api/auth/me") {
      return fulfillJson(route, session);
    }
    if (path === "/api/v1/system/health") {
      return fulfillJson(route, { status: "operational", service: "FactoryErp.Api" });
    }
    if (path === "/api/v1/quote-requests" && method === "GET") {
      return fulfillJson(route, quotes);
    }
    if (path.startsWith("/api/v1/public/catalog/products/")) {
      const slug = decodeURIComponent(path.split("/").pop() ?? "");
      const found = products.find((item) => item.slug === slug) ?? product;
      return fulfillJson(route, found);
    }
    if (path.startsWith("/api/v1/public/catalog/products")) {
      return fulfillJson(route, {
        items: products,
        page: 1,
        pageSize: 24,
        totalCount: products.length,
        hasNextPage: false,
      });
    }
    if (path === "/api/v1/public/quote-requests" && method === "POST") {
      return fulfillJson(route, {
        id: "qr-new",
        requestNumber: "TLT-2026-0003",
        status: "Received",
        createdAt: new Date().toISOString(),
      });
    }
    if (path.startsWith("/api/auth/")) {
      return fulfillJson(route, { title: "Yakalandı" }, 401);
    }
    return fulfillJson(route, { title: "Mock dışı", detail: path }, 404);
  });
}

async function shot(page, name, options = {}) {
  const file = join(outDir, name);
  await page.addStyleTag({
    content: "nextjs-portal, #__next-build-watcher { display: none !important; }",
  }).catch(() => {});
  await page.waitForTimeout(400);
  await page.screenshot({ path: file, fullPage: true, animations: "disabled", ...options });
  console.log("wrote", name);
}

async function main() {
  await mkdir(outDir, { recursive: true });
  const browser = await chromium.launch();
  const desktop = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: "tr-TR",
  });
  const mobile = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true,
    hasTouch: true,
    locale: "tr-TR",
  });

  const desk = await desktop.newPage();
  await installMocks(desk);

  await desk.goto(`${base}/giris`, { waitUntil: "networkidle" });
  await desk.getByRole("heading", { name: "Giriş yap" }).waitFor();
  await shot(desk, "01-giris-desktop.png");

  await desk.goto(`${base}/katalog`, { waitUntil: "networkidle" });
  await desk.getByRole("heading", { name: "Ürünler" }).waitFor();
  await desk.getByText("Ultra Soft Peçete").waitFor();
  await shot(desk, "02-katalog-desktop.png");

  await desk.goto(`${base}/katalog/ultra-soft`, { waitUntil: "networkidle" });
  await desk.getByRole("heading", { name: "Ultra Soft Peçete" }).waitFor();
  await shot(desk, "03-katalog-urun-desktop.png");

  await desk.getByRole("button", { name: "Teklife ekle" }).click();
  await desk.getByText("Teklif sepetine eklendi").waitFor();
  await desk.getByRole("button", { name: "Sepete git" }).click();
  await desk.getByRole("heading", { name: "Teklif sepetiniz" }).waitFor();
  await shot(desk, "04-katalog-sepet-desktop.png");

  await desk.context().addCookies([
    { name: "fe_access", value: "capture-token", url: base },
  ]);
  await desk.goto(`${base}/dashboard`, { waitUntil: "networkidle" });
  await desk.getByRole("heading", { name: "Genel Bakış" }).waitFor();
  await desk.getByText("TLT-2026-0001").waitFor();
  await shot(desk, "05-dashboard-desktop.png");

  const phone = await mobile.newPage();
  await installMocks(phone);

  await phone.goto(`${base}/katalog`, { waitUntil: "networkidle" });
  await phone.getByRole("heading", { name: "Ürünler" }).waitFor();
  await shot(phone, "06-katalog-mobile.png");

  await phone.goto(`${base}/katalog/ultra-soft`, { waitUntil: "networkidle" });
  await phone.getByRole("heading", { name: "Ultra Soft Peçete" }).waitFor();
  await phone.getByRole("button", { name: "Teklife ekle" }).click();
  await phone.getByRole("button", { name: "Sepete git" }).click();
  await phone.getByRole("button", { name: "Bilgilerimi gir ve teklif iste" }).click();
  await phone.getByLabel(/Firma adı/).waitFor();
  await shot(phone, "07-katalog-teklif-form-mobile.png");

  await phone.context().addCookies([
    { name: "fe_access", value: "capture-token", url: base },
  ]);
  await phone.goto(`${base}/dashboard`, { waitUntil: "networkidle" });
  await phone.getByRole("heading", { name: "Genel Bakış" }).waitFor();
  await shot(phone, "08-dashboard-mobile.png");

  await browser.close();
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
