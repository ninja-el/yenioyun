# 06 — Sabitler ve Kararlar

GDD'de "N" olarak bırakılmış değerlerin proje varsayılanları burada. **Kod içine sayı gömülmez**;
bu tablodaki değerler `GameConfig` (ScriptableObject) veya `LevelData` üzerinden okunur.
Değer değişirse önce bu dosya güncellenir.

## Gameplay

| Değer | Varsayılan | Kaynak |
|---|---|---|
| Kutu kapasitesi | 3 obje | `GameConfig` |
| Bantta aynı anda duran kutu | 3 | `LevelData.conveyorCapacity` |
| Level süresi | 60 sn | `LevelData.duration` |
| Level kutu hedefi | Level'e özel | `LevelData.targetBoxCount` |
| Yığındaki toplam obje | `targetBoxCount * 3` | `LevelData` (kural, ihlal edilemez) |
| Hatalı hamle süre cezası | Kapalı (3 hatada -5 sn opsiyonu `GameConfig` bayrağı) | `GameConfig` |
| Objenin kutuya uçuş süresi | 0.35 sn | `GameConfig` |
| Bant slotları arası mesafe | 1.5 birim | `Conveyor` (prefab alanı) |
| Yığın doğma ızgarası | 4 sütun x 4 sıra | `ItemStack` |
| Doğma anında objeler arası mesafe | 0.6 birim | `ItemStack` |
| İlk katın doğma yüksekliği | 1.2 birim | `ItemStack` |
| Doğma noktası rastgele sapması | 0.05 birim | `ItemStack` |
| Yığının oturması için zaman aşımı | 5 sn | `ItemStack` |
| Yığın alanı iç ölçüsü | 2.0 x 2.0, duvar 2.5 yükseklik | `StackBounds.prefab` |
| Objenin uçuş kavisi yüksekliği | 1.5 birim | `MatchResolver` |
| Hatalı hamlede kamera sarsıntısı | 0.2 sn / 0.15 şiddet | `MatchResolver` |
| Hedef FPS | 60 | Proje ayarı |

## Meta / Ekonomi

| Değer | Varsayılan |
|---|---|
| Maks. can | 5 |
| Can yenilenme süresi | 15 dk |
| Level girişi maliyeti | 1 can |
| Level tamamlama ödülü | 50 gold (rewarded reklamla x2) |
| Interstitial aralığı | Her 2 level geçişinde 1 |

## Boosterlar

| Booster | Açılış | Değer |
|---|---|---|
| Time Freeze | Lvl 4 | 5 sn donma |
| Shuffle | Lvl 6 | — |
| Auto-Match | Lvl 8 | 1 obje eşleştirir |
| Joker Box | Lvl 10 | 1 kutu |

Boosterlar envanterden tüketilir, cooldown yoktur, level içinde kullanım limiti stok kadardır.

## Alınmış kararlar

| # | Karar | Gerekçe |
|---|---|---|
| 1 | Tek `GameScene.unity` + `LevelData` ile besleme | Bölüm başına sahne çoğaltmak merge ve boyut sorunu yaratır |
| 1b | Level geçişi "önce yükle, sonra sil" (kısa süre 2 level sahnesi yüklü) | Yükleme beklemesi oyuncuya siyah ekran olarak yansımasın |
| 1c | Kamera + `AudioListener` kalıcı olarak `MainScene`'de | Geçiş anında çift kamera/çift listener oluşmasın |
| 1d | Havuz kökü `DontDestroyOnLoad`, level sahnesine parent edilmez | Sahne unload olunca havuz objeleri yok olmasın |
| 2 | `ItemType` enum değil ScriptableObject | Yeni obje eklemek kod değişikliği gerektirmesin |
| 3 | Manager'lar basit singleton | 2-3 haftalık sürede DI/servis locator ek maliyet |
| 4 | Kayıt önce local JSON, Cloud Save Faz 4'te | Cloud bağımlılığı erken fazı bloklamasın |
| 5 | Reklam/IAP çağrıları arayüz arkasında (`IAdService`, `IPurchaseService`) | SDK seçimi (Unity Ads / AppLovin) sonra netleşecek |
| 6 | Assembly Definition kullanılmıyor | Küçük projede derleme kazancı, kurulum maliyetini karşılamıyor |
| 7 | Yığın fiziksel: objeler rigidbody taşır, alttaki çekilince üsttekiler çöker | Oyunun temel hissi; sabit ızgara yerleşimi bu mekaniği vermiyor |
| 7a | Yığın sınırı görünmez duvar + zemin collider'ı (`StackBounds.prefab`) | Model gerektirmeden objelerin dağılmasını engeller |
| 7b | Level açılışında objeler yukarıdan dökülür; yığın oturunca (rigidbody sleep) süre başlar | Oyuncu sayaç işlerken yerleşmeyi beklemesin |

## Açık sorular

Cevaplanınca yukarıdaki tablolara taşı ve buradan sil.

- [ ] Level sayısı hedefi (yayın için kaç bölüm)?
- [ ] IAP paket fiyatları ve içerikleri.
- [ ] Reklam SDK'sı: Unity Ads mi AppLovin MAX mi?
- [ ] Kamera açısı sabit mi, level'e göre değişiyor mu?
