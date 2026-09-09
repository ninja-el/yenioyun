# 05 — Trello Kartı ve AI'ya İş Verme Şablonu

Kartlar bağımsızdır; bu şablonun amacı her kartın aynı bağlamla başlaması ve aynı kalitede
bitmesidir.

## Trello kart şablonu

```
Başlık: [Alan] Kısa iş tanımı        (Alan: Gameplay | Meta | UI | Tooling | Art)

Amaç
- Bu iş bittiğinde oyunda ne değişmiş oluyor (1-2 cümle).

Kapsam
- Dokunulacak dosya/prefab/klasörler.
- Kapsam DIŞI olanlar (açıkça yaz).

Kabul kriterleri
- [ ] Gözle doğrulanabilir madde
- [ ] Gözle doğrulanabilir madde

Bağımlılık
- Yok / Kart #X'in ürettiği event

Notlar
- Kullanılacak sabitler, referans doküman bölümü.
```

## Definition of Done

Kart ancak hepsi sağlanınca "Done" olur:

- [ ] Unity'de Console'da error/warning üretmeden çalışıyor.
- [ ] Play Mode'da kabul kriterleri elle test edildi.
- [ ] `02-Mimari.md` yapısına uygun (doğru klasör, namespace, event, pooling).
- [ ] `03-Kodlama-Standartlari.md` uygulanmış; gereksiz yorum ve ölü kod yok.
- [ ] Yeni asset'ler `04-Varlik-ve-Isimlendirme.md` kalıbındaki isimlerle.
- [ ] Yeni sayısal değer eklendiyse `06-Sabitler-ve-Kararlar.md` güncellendi.
- [ ] Sahne dosyası değiştiyse kartta yazıyor.
- [ ] `.meta` dosyalarıyla birlikte commit edildi.

## AI asistanına iş verirken kullanılacak prompt

```
Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

Görev: <Trello kartının Amaç + Kapsam bölümü>

Kısıtlar:
- Sadece bu kapsam. Ek özellik, refactor, yeni paket yok.
- Değer gerekiyorsa 06-Sabitler-ve-Kararlar.md'den al; yoksa öner ve bekle.
- Sahne dosyasına dokunma / <dokunulabilecek sahne>.

Teslim: değişen dosya listesi + Unity'de elle test adımları +
varsa dokümana eklenmesi gereken karar.
```

## AI'nın cevabında beklenenler

1. Yaptığı değişikliklerin dosya bazlı özeti.
2. Inspector'da yapılması gereken bağlamalar (hangi alan neye atanacak).
3. Test adımları.
4. Belirsiz bıraktığı veya varsaydığı noktalar — koda gömülü değil, açık liste halinde.
