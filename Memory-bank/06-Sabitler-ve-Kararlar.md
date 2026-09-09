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

## Açık sorular

Cevaplanınca yukarıdaki tablolara taşı ve buradan sil.

- [ ] Level sayısı hedefi (yayın için kaç bölüm)?
- [ ] IAP paket fiyatları ve içerikleri.
- [ ] Reklam SDK'sı: Unity Ads mi AppLovin MAX mi?
- [ ] Kamera açısı sabit mi, level'e göre değişiyor mu?
