import React from "react";
import { act, render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TelegramBaglantisiSayfasi } from "./TelegramBaglantisiSayfasi";
import type { TelegramEkranVerisi } from "./types";

const jsonOkuMock = vi.hoisted(() => vi.fn());

vi.mock("../../shared/json", () => ({ jsonOku: jsonOkuMock }));

const ekran: TelegramEkranVerisi = {
  bagli: false,
  durum: "Bağlı değil",
  botKullaniciAdi: "SystemcelBot",
  eslestirmeKodu: "SC-123456",
  baglantiLinki: "https://t.me/SystemcelBot",
  qrUrl: "",
  gecerlilikDakika: 10,
  mesaj: "Bekleniyor"
};

describe("TelegramBaglantisiSayfasi polling", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["setTimeout", "setInterval", "clearTimeout", "clearInterval", "Date"] });
    jsonOkuMock.mockReset().mockResolvedValue(ekran);
    Object.defineProperty(document, "visibilityState", { configurable: true, value: "visible" });
  });

  afterEach(() => {
    vi.useRealTimers();
    Object.defineProperty(document, "visibilityState", { configurable: true, value: "visible" });
  });

  it("does not poll while hidden and refreshes when visible again", async () => {
    jsonOkuMock.mockResolvedValue(ekran);
    const view = render(<TelegramBaglantisiSayfasi />);
    await act(async () => undefined);
    expect(jsonOkuMock).toHaveBeenCalledTimes(1);

    Object.defineProperty(document, "visibilityState", { configurable: true, value: "hidden" });
    await act(async () => {
      vi.advanceTimersByTime(9_000);
    });
    expect(jsonOkuMock).toHaveBeenCalledTimes(1);

    Object.defineProperty(document, "visibilityState", { configurable: true, value: "visible" });
    await act(async () => {
      document.dispatchEvent(new Event("visibilitychange"));
    });
    expect(jsonOkuMock).toHaveBeenCalledTimes(2);
    view.unmount();
  });

  it("coalesces slow polling requests and cleans up on unmount", async () => {
    let resolveRequest: ((value: TelegramEkranVerisi) => void) | undefined;
    jsonOkuMock.mockImplementationOnce(() => new Promise<TelegramEkranVerisi>((resolve) => {
      resolveRequest = resolve;
    }));

    const view = render(<TelegramBaglantisiSayfasi />);
    await act(async () => undefined);

    await act(async () => {
      vi.advanceTimersByTime(9_000);
    });
    expect(jsonOkuMock).toHaveBeenCalledTimes(1);

    resolveRequest?.(ekran);
    await act(async () => undefined);
    await act(async () => {
      vi.advanceTimersByTime(3_000);
    });
    expect(jsonOkuMock).toHaveBeenCalledTimes(2);

    view.unmount();
    await act(async () => {
      vi.advanceTimersByTime(6_000);
    });
    expect(jsonOkuMock).toHaveBeenCalledTimes(2);
  });

  it("stops polling after the connection becomes active", async () => {
    jsonOkuMock.mockResolvedValue({ ...ekran, bagli: true, durum: "Bağlı" });
    const view = render(<TelegramBaglantisiSayfasi />);
    await act(async () => undefined);

    await act(async () => {
      vi.advanceTimersByTime(9_000);
    });
    expect(jsonOkuMock).toHaveBeenCalledTimes(1);
    view.unmount();
  });
});
