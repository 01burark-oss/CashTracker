namespace CashTracker.Core.Entities;

public sealed class AbonelikFiyatBildirimKaniti
{
    public long Id { get; set; }
    public int AbonelikId { get; set; }
    public int IsletmeId { get; set; }
    public string KullaniciRef { get; set; } = string.Empty;
    public string AliciEposta { get; set; } = string.Empty;
    public decimal EskiNetTutar { get; set; }
    public decimal YeniNetTutar { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public DateTime DonemBaslangicAt { get; set; }
    public DateTime DonemBitisAt { get; set; }
    public DateTime YururlukAt { get; set; }
    public string MetinSurumu { get; set; } = string.Empty;
    public string MetinHash { get; set; } = string.Empty;
    public DateTime BildirimOlusturulduAt { get; set; }
    public long UygulamaOutboxId { get; set; }
    public long EpostaOutboxId { get; set; }
    public DateTime CreatedAt { get; set; }
}
