# 01 — Oyun Özeti

GDD'nin geliştirme sırasında lazım olan kısmı. Çelişki halinde `GDD ve Pipeline.pdf` esastır;
çelişkiyi fark edersen bu dosyayı güncelle.

## Tek cümle

Oyuncu ekrandaki 3D obje yığınından objelere dokunarak, taşıyıcı banttaki kutuları süre bitmeden
doldurur.

## Core Loop

1. 1 can harcanarak bölüme girilir.
2. Yığındaki 3D objeye dokunulur → objenin resmini taşıyan kutuya uçar.
3. Kutu 3/3 dolunca yok olur, level havuzundan yeni kutu banta gelir.
4. Süre dolmadan levelin tüm kutuları doldurulursa kazanılır → altın (reklamla x2) → menü.

## Kurallar

- Bantta aynı anda sabit sayıda kutu durur (`06-Sabitler`), her kutu 3 obje alır.
- **Başarılı hamle:** Bantta o objeye ait ve dolmamış kutu varsa obje kavisli şekilde kutuya uçar.
- **Hatalı hamle:** Uygun kutu yoksa obje kırmızı outline ile parlar, hata sesi çalar, kamera
  titrer, obje yerine düşer. Süre cezası opsiyonu varsayılan olarak kapalıdır.
- Bir levelin obje sayısı, kutu hedefinin tam 3 katı olmak zorundadır (artık obje kalamaz).

## Meta

- **Can:** Maks. 5, girişte 1 tüketilir, süreyle yenilenir. Oyun kapalıyken de işler
  (`lastLifeRegenTime` ile offline hesap).
- **Para:** Gold (soft), IAP (hard).
- **Reklam:** Level sonu x2 gold (rewarded), bölüm geçişlerinde interstitial.

## Boosterlar

| Booster | Açılış | Etki |
|---|---|---|
| Time Freeze | Lvl 4 | Süreyi belirli saniye durdurur, UI'da buzlanma efekti |
| Shuffle | Lvl 6 | Yığındaki objeleri zıplatıp yeniden karıştırır |
| Auto-Match | Lvl 8 | Banttaki rastgele bir kutuya yığından doğru objeyi fırlatır |
| Joker Box | Lvl 10 | Banta gri/resimsiz kutu iner; tıklanan ilk objenin resmini alır |

## Terim sözlüğü (kod ve konuşmada aynı kelimeyi kullan)

| Türkçe | Kod karşılığı | Nedir |
|---|---|---|
| Obje / yığın objesi | `StackItem` | Oyuncunun tıkladığı 3D obje |
| Yığın | `ItemStack` | Objelerin durduğu küme |
| Kutu | `Box` | 3 obje alan, resimli kutu |
| Bant | `Conveyor` | Kutuların dizildiği taşıyıcı |
| Slot | `ConveyorSlot` | Banttaki tek kutu pozisyonu |
| Obje tipi | `ItemType` | Elma, araba... eşleşme anahtarı |
| Can | `Life` | Enerji birimi |
| Bölüm verisi | `LevelData` | ScriptableObject level tanımı |
