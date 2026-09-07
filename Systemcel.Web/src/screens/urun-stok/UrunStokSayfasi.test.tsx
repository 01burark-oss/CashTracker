import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonOku } from "../../shared/json";
import { UrunStokSayfasi } from "./UrunStokSayfasi";
import type { UrunStokEkranVerisi } from "./types";

vi.mock("../../shared/json", () => ({ jsonOku: vi.fn() }));

const ekran: UrunStokEkranVerisi = {
  aktifIsletme: "Systemcel Test",
  urunler: [],
  sonHareketler: [],
  tipSecenekleri: [{ deger: "Urun", etiket: "Ürün" }],
  birimSecenekleri: [{ deger: "Adet", etiket: "Adet" }]
};

describe("UrunStokSayfasi form hataları", () => {
  beforeEach(() => {
    Object.defineProperty(HTMLElement.prototype, "scrollTo", { configurable: true, value: vi.fn() });
    Object.defineProperty(HTMLElement.prototype, "scrollIntoView", { configurable: true, value: vi.fn() });
    vi.stubGlobal("requestAnimationFrame", vi.fn());
    vi.mocked(jsonOku).mockResolvedValue(ekran);
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
    vi.clearAllMocks();
  });

  it("bilinen ürün adı doğrulama hatasını ürün formuna bağlayarak duyurur", async () => {
    const user = userEvent.setup();
    render(<UrunStokSayfasi onIsletmeDegistir={vi.fn()} ustBar={null} ustBarIslemde={false} yenileAnahtari={0} />);

    await screen.findByRole("button", { name: "Yeni ürün" });
    await user.click(screen.getByRole("button", { name: "Yeni ürün" }));
    const ad = await screen.findByRole("textbox", { name: "Ad" });
    await user.click(screen.getByRole("button", { name: "Kaydet" }));

    const hata = await screen.findByRole("alert");
    expect(hata).toHaveTextContent("Ad alanı zorunludur.");
    expect(hata).toHaveAttribute("id", "urun-stok-form-hata");
    expect(ad).toHaveAttribute("aria-describedby", "urun-stok-form-hata");
    expect(ad).toHaveAttribute("aria-invalid", "true");
    expect(screen.getByRole("textbox", { name: "Barkod" })).toHaveAttribute("aria-describedby", "urun-stok-form-hata");
  });
});
