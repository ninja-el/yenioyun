# 01 — Oyun Özeti

GDD'nin geliştirme sırasında lazım olan kısmı. Çelişki halinde `GDD ve Pipeline.pdf` esastır;
çelişkiyi fark edersen bu dosyayı güncelle.

> **Bant hakkında:** GDD "levelin toplam kutu havuzundan yeni bir kutu banta gelir" diyor ama
> kutunun bant üzerinde ne yaptığını açmıyor. Sahnedeki bant modeli kapalı devre (dikdörtgen tur)
> bir konveyördür; bu dosyadaki bant kuralları o cümlenin açılmış halidir. Karar kaydı
> `06-Sabitler-ve-Kararlar.md` #8.

## Tek cümle

Oyuncu ekrandaki 3D obje yığınından objelere dokunarak, dönen taşıyıcı bant üzerindeki kutuları
süre bitmeden doldurur.

## Core Loop

1. 1 can harcanarak bölüme girilir.
2. Bant döner, ilk kutular banta girer; yığındaki objeler ortaya dökülür ve oturur.
3. Yığındaki 3D objeye dokunulur → obje, banttaki kendi resmini taşıyan dolmamış kutuya uçar.
4. Kutu 3/3 dolunca banttan ayrılır, çıkış noktasına doğru hareket edip orada yok olur.
5. Gerekliyse yeni kutu giriş noktasından banta katılır.
6. Süre dolmadan levelin tüm kutuları doldurulursa kazanılır → altın (reklamla x2) → menü.

## Bant kuralları

Bant **kapalı bir turdur**; kutular bant üzerinde sürekli hareket eder, sabit yerde durmaz.

- **Duruş:** Kutular tur boyunca **dönmez**; prefab'ta verilmiş sabit yönlerini korurlar, bant
  yalnızca konumlarını değiştirir. Böylece kutunun üzerindeki ikon turun her noktasından okunur.
- **Slot:** Tur, eşit aralıklı sanal slotlara bölünür. Slotlar bantla birlikte döner; bir kutu
  daima bir slota bağlıdır ve konumunu slot belirler. Kutular kendi konumunu hesaplamaz, bu
  yüzden birbirine giremez ve aralarındaki mesafe kendiliğinden sabit kalır. Slot sayısı bant
  modelinin uzunluğuna göre ayarlanır (`ConveyorPath`), level verisinden gelmez.
- **Kapasite:** Bantta aynı anda en fazla `LevelData.conveyorCapacity` **doldurulabilir** kutu
  bulunur. Bu sayı slot sayısından büyük olamaz. Kapasiteye yalnızca hâlâ obje kabul eden kutular
  sayılır: oyuncu bir kutunun üçüncü objesini gönderdiği anda o kutu kapasiteden düşer ve yeni
  kutunun yolu anında açılır. Ayrılıp çıkışa giden kutu kapasiteyi işgal etmez.
- **Giriş:** Kutular tek bir giriş noktasından, bant dışından belirli bir yönden gelerek banta
  katılır (`EntryStart` → `EntryPoint`). Giriş animasyonu süren kutu henüz eşleşme kabul etmez.
- **Çıkış:** Tamamlanan kutu turu beklemez; bulunduğu yerde banttan ayrılır, çıkış noktasına
  (`ExitPoint`) doğru hareket eder ve orada kaybolup havuza iade edilir. Çıkış noktası tur
  üzerinde değildir.
- **Yeni kutu ne zaman gelir:** Yığında kalan obje sayısı, o an bantta olan kutuların toplam boş
  yuva sayısından **fazlaysa** yeni kutu gönderilir. Kalan objeler banttaki kutulara sığıyorsa
  yeni kutu gelmez. Level boyunca gönderilen toplam kutu sayısı `LevelData.targetBoxCount`'tur.
- Bandın görsel dönüşü (doku kayması, tahrik silindirleri) kutuların hızıyla aynı değerden
  beslenir; ikisi ayrı ayrı ayarlanmaz.

## Hamle kuralları

- **Başarılı hamle:** Bantta o objenin tipine ait, dolmamış ve girişini tamamlamış bir kutu varsa
  obje kavisli şekilde kutuya uçar.
  **Hedef hareketlidir:** kutu uçuş boyunca ilerlemeye devam ettiği için uçuşun varış noktası
  sabit bir konum değil, kutunun o anki yuvasıdır.
- **Hatalı hamle:** Uygun kutu yoksa obje kırmızı outline ile parlar, hata sesi çalar, kamera
  titrer, obje yerine düşer. Süre cezası opsiyonu varsayılan olarak kapalıdır.
- Bir objeye dokunulduğu anda kutuda yuva ayrılır; uçuş sürerken o yuva başka objeye verilmez.
- Bir levelin obje sayısı, kutu hedefinin tam 3 katı olmak zorundadır (artık obje kalamaz).

## Meta

- **Can:** Maks. 5, girişte 1 tüketilir, süreyle yenilenir. Oyun kapalıyken de işler
  (`lastLifeRegenTime` ile offline hesap).
- **Para:** Gold (soft), IAP (hard).
- **Reklam:** Level sonu x2 gold (rewarded), bölüm geçişlerinde interstitial.

## Boosterlar

| Booster | Açılış | Etki |
|---|---|---|
| Ek Süre | Lvl 4 | Süreye belirli saniye ekler. Butondan "+X sn" yazısı süre göstergesine uçar, vardığında süre eklenir ve gösterge büyüyüp küçülür |
| Shuffle | Lvl 6 | Yığındaki objeleri alan içinde yeniden dağıtır; objeler birbirine çarpmadan yeni yerlerine kayar |
| Auto-Match | Lvl 8 | `BoosterData`'daki moda göre ya belirli sayıda objeyi kutulara gönderir (Items) ya da belirli sayıda kutuyu tamamen doldurur (Boxes). Aynı anda yalnızca bir mod çalışır |
| Joker Box | Lvl 10 | Banta tipsiz kutu girer; ilk objesini her türden kabul eder, o tipe kilitlenir ve kalan yuvalarını yalnızca aynı tipten doldurur |

## Terim sözlüğü (kod ve konuşmada aynı kelimeyi kullan)

| Türkçe | Kod karşılığı | Nedir |
|---|---|---|
| Obje / yığın objesi | `StackItem` | Oyuncunun tıkladığı 3D obje |
| Yığın | `ItemStack` | Objelerin durduğu küme |
| Kutu | `Box` | 3 obje alan, resimli kutu |
| Bant | `Conveyor` | Kutuları taşıyan, döndüren sistem |
| Bant yolu | `ConveyorPath` | Kutuların üzerinde döndüğü kapalı tur; waypoint'lerle tanımlanır |
| Slot | Sanal slot (`Conveyor` içinde) | Bantla birlikte dönen, kutunun bağlı olduğu pozisyon |
| Giriş noktası | `EntryPoint` / `EntryStart` | Kutunun banta katıldığı yer ve geldiği yön |
| Çıkış noktası | `ExitPoint` | Tamamlanan kutunun gidip kaybolduğu, tur dışındaki nokta |
| Obje tipi | `ItemType` | Elma, araba... eşleşme anahtarı |
| Can | `Life` | Enerji birimi |
| Bölüm verisi | `LevelData` | ScriptableObject level tanımı |
