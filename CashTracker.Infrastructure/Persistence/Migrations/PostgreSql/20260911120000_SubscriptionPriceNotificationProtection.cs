using System;
using CashTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CashTracker.Infrastructure.Persistence.Migrations.PostgreSql;

[DbContext(typeof(CashTrackerDbContext))]
[Migration("20260911120000_SubscriptionPriceNotificationProtection")]
public partial class SubscriptionPriceNotificationProtection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("AbonelikFiyatBildirimKaniti", table => new
        {
            Id = table.Column<long>("bigint").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            AbonelikId = table.Column<int>("integer"), IsletmeId = table.Column<int>("integer"),
            KullaniciRef = table.Column<string>("character varying(200)", maxLength: 200),
            AliciEposta = table.Column<string>("character varying(320)", maxLength: 320),
            EskiNetTutar = table.Column<decimal>("NUMERIC(18,2)"), YeniNetTutar = table.Column<decimal>("NUMERIC(18,2)"),
            ParaBirimi = table.Column<string>("character varying(3)", maxLength: 3),
            DonemBaslangicAt = table.Column<DateTime>("timestamp without time zone"), DonemBitisAt = table.Column<DateTime>("timestamp without time zone"),
            YururlukAt = table.Column<DateTime>("timestamp without time zone"),
            MetinSurumu = table.Column<string>("character varying(80)", maxLength: 80), MetinHash = table.Column<string>("character varying(64)", maxLength: 64),
            BildirimOlusturulduAt = table.Column<DateTime>("timestamp without time zone"),
            UygulamaOutboxId = table.Column<long>("bigint"), EpostaOutboxId = table.Column<long>("bigint"),
            CreatedAt = table.Column<DateTime>("timestamp without time zone")
        }, constraints: table => table.PrimaryKey("PK_AbonelikFiyatBildirimKaniti", x => x.Id));
        migrationBuilder.CreateIndex("IX_AbonelikFiyatBildirimKaniti_AbonelikId_YururlukAt_YeniNetTutar", "AbonelikFiyatBildirimKaniti", new[] { "AbonelikId", "YururlukAt", "YeniNetTutar" }, unique: true);
        migrationBuilder.CreateIndex("IX_AbonelikFiyatBildirimKaniti_IsletmeId_YururlukAt", "AbonelikFiyatBildirimKaniti", new[] { "IsletmeId", "YururlukAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("AbonelikFiyatBildirimKaniti");
}
