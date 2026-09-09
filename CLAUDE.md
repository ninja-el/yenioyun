# Match & Pack 3D — AI Asistan Talimatları

Unity 6 / URP, mobil (iOS + Android), low-poly 3D, izometrik kamera. Toplam süre 2-3 hafta.
Görevler Trello'da **birbirinden bağımsız kartlar** halinde ilerler. Her kart, başka bir kartın
sonucunu beklemeden bu dokümanlardaki kurallara uyularak tamamlanır.

## Çalışmaya başlamadan önce oku

| Konu | Dosya |
|---|---|
| Oyun ne, hangi mekanikler var, terimler | `Memory-bank/01-Oyun-Ozeti.md` |
| Sahne/manager/event/save mimarisi, klasör yapısı | `Memory-bank/02-Mimari.md` |
| Kod yazım ve yorum kuralları | `Memory-bank/03-Kodlama-Standartlari.md` |
| Dosya, prefab, asset isimlendirme + git kuralları | `Memory-bank/04-Varlik-ve-Isimlendirme.md` |
| Bir Trello kartını tamamlama şablonu ve DoD | `Memory-bank/05-Gorev-Sablonu.md` |
| Sayısal değerler ve alınmış kararlar | `Memory-bank/06-Sabitler-ve-Kararlar.md` |
| Kaynak GDD | `Memory-bank/GDD ve Pipeline.pdf` |

## Değişmez kurallar

1. **Kapsam kilitli.** Sadece kartta yazan işi yap. "Bunu da ekleyeyim" yok; eksik gördüğün şeyi
   kod yerine cevabında bir satırla bildir.
2. **Sayı uydurma.** Süre, ödül, kapasite, cooldown gibi her değer `06-Sabitler-ve-Kararlar.md`
   veya `LevelData` üzerinden gelir. Orada yoksa varsayılan öner, dokümana ekle, kartta belirt.
3. **Yeni bağımlılık yok.** Paket, plugin, tasarım deseni veya mimari değişikliği önce sorulur.
   İzinli paket listesi `02-Mimari.md` içindedir.
4. **Sahne dosyasına dokunma.** İş prefab üzerinde yapılır. Sahne değişikliği zorunluysa kartta
   yaz ve tek bir sahneyle sınırlı tut (merge çakışması riski).
5. **Gameplay'de `Instantiate` / `Destroy` yok.** Obje, kutu ve efektler `PoolManager` üzerinden.
6. **UI oyun mantığı içermez.** UI event dinler, sonuç gösterir; kural işletmez.
7. **Yorum politikası:** İsimlendirme açıklayıcıysa yorum yazma. İzinli olanlar: public API'de
   `<summary>`, `[SerializeField]` alanlarda `[Tooltip]`, ve "neden böyle" açıklayan tek satırlık
   notlar. Detay: `03-Kodlama-Standartlari.md`.
8. **Dil:** Kod, identifier, log ve commit mesajları İngilizce. Doküman ve `[Tooltip]` Türkçe.
9. **Emin değilsen dur ve sor.** Tahminle mimari kararı verme; yanlış varsayım iki haftalık
   projede en pahalı hatadır.
