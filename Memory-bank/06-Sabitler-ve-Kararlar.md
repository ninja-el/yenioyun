# 06 — Sabitler ve Kararlar

GDD'de "N" olarak bırakılmış değerlerin proje varsayılanları burada. **Kod içine sayı gömülmez**;
bu tablodaki değerler `GameConfig` (ScriptableObject) veya `LevelData` üzerinden okunur.
Değer değişirse önce bu dosya güncellenir.

## Gameplay

| Değer | Varsayılan | Kaynak |
|---|---|---|
| Kutu kapasitesi | 3 obje | `GameConfig` |
| Bantta aynı anda duran doldurulabilir kutu | 3 | `LevelData.conveyorCapacity` |
| Level süresi | 30–180 sn (bkz. Level dengesi) | `LevelData.duration` |
| Level kutu hedefi | Level'e özel, tahmin modelinden (bkz. Level dengesi) | `LevelData.targetBoxCount` |
| Yığındaki toplam obje | `targetBoxCount * 3` | `LevelData` (kural, ihlal edilemez) |
| Hatalı hamle süre cezası | Kapalı (3 hatada -5 sn opsiyonu `GameConfig` bayrağı) | `GameConfig` |
| Objenin kutuya uçuş süresi | 0.35 sn | `GameConfig` |
| Bant slot sayısı | 12 (modele göre ayarlanır) | `ConveyorPath` |
| Bant yolu yumuşatma örneği | 12 (1 = köşeler keskin) | `ConveyorPath` |
| Düz kenar toleransı | 0.01 birim | `ConveyorPath` |
| Bant hızı | 0.8 slot/sn (12 slotlu turda ~15 sn/tur) | `GameConfig` |
| Kutu giriş gecikmesi | 0.5 sn | `GameConfig` |
| Kutu giriş animasyonu süresi | 0.4 sn | `GameConfig` |
| Kutu çıkış animasyonu süresi (yalnızca joker yüzünden iptal edilen boş kutu) | 0.35 sn | `GameConfig` |
| Dolan kutunun kapak kapanma süresi | 0.25 sn (kısa kapaklar 0, uzun kapaklar 0.12 sn gecikmeyle) | `Box` prefab |
| Dolan kutunun yükselme mesafesi (sonunda 0 ölçek) | 2.5 birim | `Box` prefab |
| Dolan kutunun yükselip küçülme süresi | 0.6 sn (InOutSine; ölçek katedilen yükseklikten hesaplanır) | `Box` prefab |
| Yığın alanı iç ölçüsü | 6 x 6 x 6, merkez (0, 3, 0) | `StackArea` |
| Alan duvar kalınlığı | 1.0 birim (zemin + 4 duvar + tavan) | `StackArea` |
| Doğma noktaları arası ek boşluk | 0.05 birim (objenin kendi yarıçapına eklenir) | `StackArea` |
| Boş nokta aramasında aday sayısı | 24 | `StackArea` |
| Bekleyen objeler için tarama aralığı | 0.5 sn | `ItemStack` |
| Yığının oturması için zaman aşımı | 1 sn | `ItemStack` |
| Dokunuş probu | Kutu (BoxCast), 0.3 birim kenar | `InputManager` |
| Probun toplayacağı en fazla obje | 2 | `InputManager` |
| Obje tipinin bölüme özel boyut çarpanı | 1 (satır başına, 0.01 alt sınır) | `LevelData.Items[].ScaleMultiplier` |
| Objenin uçuş kavisi yüksekliği | 1.5 birim | `MatchResolver` |
| Kutu yuva hacmi (genişlik x yükseklik x derinlik) | 0.454 x 0.526 x 1.22 birim (kutu iç hacmi / 3; taban = ikon düzlemi) | `Box` prefab |
| Objenin yuva hacmini doldurma payı | 0.9 | `Box` prefab |
| Hatalı hamlede kamera sarsıntısı | 0.2 sn / 0.15 şiddet | `MatchResolver` |
| Hedef FPS | 60 | Proje ayarı |

## Level dengesi

Bölümler elle değil, `Data/Config/LevelBalanceConfig.asset` eğrilerinden `MatchPack/Generate Levels`
menüsüyle üretilir (`Level_001…050` + `LevelCatalog`). Değer değişince menü yeniden çalıştırılır;
var olan asset'lerin üzerine yazılır, GUID'ler korunur. Sayılar oynanış testiyle ayarlanacak tahminlerdir.

**Tahmin modeli:** ortalama oyuncunun kutu başına süresi
`kutu kapasitesi x (obje başı süre + ek tip başı süre x (tip sayısı - 1)) + kutu ek süresi`.
Kutu hedefi = `taban(süre / kutu başı süre x hedef zaman kullanımı)`. Zaman kullanımı 1 ise ortalama
oyuncu süreyi tam yetiştirir. Kaybetme oranı, oyuncu süresinin normal dağıldığı kabulüyle
`1 - Φ((1/kullanım - 1) / hız farkı)` olarak tahmin edilir.

| Değer | Varsayılan | Kaynak |
|---|---|---|
| Obje başı bulma + dokunma süresi (tek tip) | 0.9 sn | `LevelBalanceConfig` |
| Ek obje tipi başına süre | 0.08 sn | `LevelBalanceConfig` |
| Kutu başı ek süre (kutu girişi, hatalı hamle) | 0.4 sn | `LevelBalanceConfig` |
| Oyuncular arası hız farkı (std / ortalama) | 0.2 | `LevelBalanceConfig` |
| Bölüm sayısı | 50 | `LevelBalanceConfig` |
| Süre | 30 sn → 180 sn, 25. bölümde tavana ulaşır; 5 sn'ye yuvarlanır | `LevelBalanceConfig` |
| Tavandan sonra normal bölüm süresi | Zor bölüme kalan her bölüm için -10 sn (140/150/160/170, zor 180) | `LevelBalanceConfig` |
| Zor bölüm aralığı | Her 5 bölümde bir (5, 10, 15…) | `LevelBalanceConfig` |
| Obje tipi sayısı | 3 → 18, eğri üssü 0.7 (ilk bölümlerde daha hızlı artar), zor bölümde +2 | `LevelBalanceConfig` |
| Açık obje tipi | İlk bölümde 4, her bölümde +1 (havuz sırasıyla) | `LevelBalanceConfig.ItemPool` |
| Hedef zaman kullanımı, normal | 0.60 → 0.85 (tahmini kaybetme ~%0 → ~%15) | `LevelBalanceConfig` |
| Hedef zaman kullanımı, zor | 0.88 → 0.98 (tahmini kaybetme ~%20 → ~%43) | `LevelBalanceConfig` |
| Süreye göre obje sayısı | `30 x (süre / 30)^k`, k = log(45/30) / log(60/30) ≈ 0.585: 30 sn → 30, 60 sn → 45, 180 sn → 87 obje; 100 objede bir sonraki obje ~4 sn ekler. Kutu hedefi tahmin modeli ile bu eğriden büyük olanıdır | `LevelBalanceConfig` (iki nokta) |
| Normal bölümde en fazla obje | 150 (zor bölümde sınır yok) | `LevelBalanceConfig` |
| Rastgelelik tohumu | 1234 | `LevelBalanceConfig` |

Not: Bugünkü katsayılarla obje eğrisi bütün bölümlerde modelin önerdiği kutu sayısının üstünde kalıyor;
kutu hedefini eğri belirliyor ve model bölümleri zor (%35–88 kaybetme) görüyor. Oynanış testinden sonra
model katsayıları (obje başı süre) gerçek hıza göre ayarlanmalı.

## Yükleme ve level sonu

| Değer | Varsayılan | Kaynak |
|---|---|---|
| Sahte yükleme süresi | 1.5 sn | `LoadingScreen` |
| Yükleme yazısı | "Yükleniyor" | `LoadingScreen` |
| Yazıdaki en fazla nokta | 3 | `LoadingScreen` |
| Nokta ekleme aralığı | 0.35 sn | `LoadingScreen` |
| Restart butonu gecikmesi | 3 sn | `LevelResultScreen` |
| Sonraki level butonu gecikmesi | 3 sn | `LevelResultScreen` |
| Kazanma şartı | Dolan kutu = `LevelData.targetBoxCount` | `LevelManager` |
| Kaybetme şartı | Süre sıfıra iner | `LevelTimer` |
| Restart / sonraki level maliyeti | 1 can | `LevelResultScreen` |

Bant sayıları (slot sayısı hariç) sahnedeki modele bakılarak değil, oyun hissine göre konmuş
varsayılanlardır; ilk oynanabilir sürümde Inspector'dan ayarlanıp bu tablo güncellenecek.

## Meta / Ekonomi

| Değer | Varsayılan |
|---|---|
| Maks. can | 5 |
| Can yenilenme süresi | 15 dk |
| Level girişi maliyeti | 1 can |
| Level tamamlama ödülü | 50 gold (rewarded reklamla x2) |
| Interstitial aralığı | Her 2 level geçişinde 1 |
| Kaybedilen levele gold ile devam | 800 gold (`GameConfig.ContinueCostGold`) |
| Devam edince eklenen süre | 15 sn (`GameConfig.ContinueExtraSeconds`) |
| Canları gold ile doldurma | 2000 gold (`GameConfig.LifeRefillCostGold`) |
| Rewarded ödül çarpanı | x2 (`GameConfig.RewardedRewardMultiplier`) |

Gold değerleri sahnedeki market ve level sonu görsellerinden okundu; değişirse `GameConfig`
güncellenir, ekrandaki yazı koddan beslendiği için elle düzeltme gerekmez.

## Ayarlar ve geri bildirim

| Değer | Varsayılan | Kaynak |
|---|---|---|
| Ses efektleri | Açık | `PlayerData.IsSoundEnabled` |
| Müzik | Açık | `PlayerData.IsMusicEnabled` |
| Titreşim (taptic) | Açık | `PlayerData.IsHapticsEnabled` |
| SFX ses düzeyi | 1.0 | `AudioManager` |
| Müzik ses düzeyi | 0.4 | `AudioManager` |
| Hatalı hamlede titreşim süresi | 60 ms | `HapticManager` |
| Hatalı hamlede titreşim şiddeti | 160 / 255 (Android 8+) | `HapticManager` |
| Titreşim tekrar bekleme süresi | 0.08 sn | `HapticManager` |
| Ekran kenarı parlamasının tepe yoğunluğu | 0.85 | `ScreenEdgeFlash` |
| Parlamanın açılma / kapanma süresi | 0.06 sn / 0.35 sn | `ScreenEdgeFlash` |
| Kenar bandının kalınlığı | Ekran genişliğinin 0.14'ü (dikeyde en-boy oranıyla eşitlenir) | `M_UI_ScreenEdgeFlash` materyali |
| Kenar bandının sertliği | 3 | `M_UI_ScreenEdgeFlash` materyali |
| Kenar bandının rengi | RGB 0.58 / 0.02 / 0.02 | `M_UI_ScreenEdgeFlash` materyali |

## IAP ürünleri

Ürün kimlikleri, içerikleri ve fiyatları `08-IAP-Entegrasyon-Rehberi.md` içindedir.
Asset'ler: `Assets/_Project/Data/Shop/`, katalog `Data/Config/ShopCatalog.asset`.

## Boosterlar

Booster değerleri `GameConfig`'te değil, booster başına bir `BoosterData` asset'indedir
(`Assets/_Project/Data/Boosters/`); liste `BoosterCatalog.asset` içindedir.

| Değer | Varsayılan | Kaynak |
|---|---|---|
| Ek Süre açılışı / eklenen süre | Lvl 4 / 5 sn | `BoosterData.BonusSeconds` |
| Ek Süre "+X sn" yazısının uçma süresi (süre, yazı varınca eklenir) | 0.5 sn | `BoosterData.BonusFlyDuration` |
| Ek Süre sonrası süre yazısının büyüme ölçeği / süresi | 1.3x / 0.3 sn | `TimeBonusView` |
| Shuffle açılışı | Lvl 6 | `BoosterData` |
| Shuffle'da objelerin yeni yerlerine kayma süresi | 0.5 sn | `BoosterData.ShuffleDuration` |
| Auto-Match açılışı | Lvl 8 | `BoosterData` |
| Auto-Match modu | Items (obje bazlı) | `BoosterData.AutoMatchMode` |
| Auto-Match adedi | 1 (moda göre obje veya kutu) | `BoosterData.AutoMatchCount` |
| Auto-Match hamleleri arası bekleme | 0.12 sn | `BoosterData.AutoMatchInterval` |
| Joker Box açılışı / adedi | Lvl 10 / 1 kutu | `BoosterData` |
| Joker kutunun alacağı obje adedi | Prefab'taki yuva sayısı (3) | Joker kutu prefab'ı |
| Booster efektinin ekranda kalma süresi | 1 sn | `BoosterData.EffectDuration` |
| Booster paketi fiyatı | 40 gold | `BoosterData.GoldPrice` |
| Booster paketindeki adet | 3 | `BoosterData.PackAmount` |
| Booster adı / açıklaması | `ui.booster.<ad>.title` / `.info` | `BoosterData` localization key'i |

Oyun içi buton sırası (ikonlara göre): `Booster_1` Auto-Match, `Booster_2` Ek Süre,
`Booster_3` Shuffle, `Booster_4` Joker Box.

Boosterlar envanterden tüketilir, cooldown yoktur, level içinde kullanım limiti stok kadardır.
Envanter index'i `BoosterType` enum sırasıdır: 0 TimeBonus, 1 Shuffle, 2 AutoMatch, 3 JokerBox.

## Alınmış kararlar

| # | Karar | Gerekçe |
|---|---|---|
| 1 | Tek `GameScene.unity` + `LevelData` ile besleme | Bölüm başına sahne çoğaltmak merge ve boyut sorunu yaratır |
| 1b | ~~Level geçişi "önce yükle, sonra sil"~~ — 27 numaralı kararla kaldırıldı; level geçişinde sahne hiç değişmiyor | — |
| 1c | ~~Kamera + `AudioListener` `MainScene`'de~~ — 27a numaralı kararla `GameScene`'e taşındı | — |
| 1d | Havuz kökü `DontDestroyOnLoad`, level sahnesine parent edilmez | Sahne unload olunca havuz objeleri yok olmasın |
| 2 | `ItemType` enum değil ScriptableObject | Yeni obje eklemek kod değişikliği gerektirmesin |
| 3 | Manager'lar basit singleton | 2-3 haftalık sürede DI/servis locator ek maliyet |
| 4 | Kayıt önce local JSON, Cloud Save Faz 4'te | Cloud bağımlılığı erken fazı bloklamasın |
| 5 | Reklam/IAP çağrıları arayüz arkasında (`IAdService`, `IPurchaseService`) | SDK seçimi (Unity Ads / AppLovin) sonra netleşecek |
| 6 | Assembly Definition kullanılmıyor | Küçük projede derleme kazancı, kurulum maliyetini karşılamıyor |
| 7 | Yığın fiziksel: objeler rigidbody taşır, alttaki çekilince üsttekiler çöker | Oyunun temel hissi; sabit ızgara yerleşimi bu mekaniği vermiyor |
| 7a | Yığın sınırı görünmez duvar + zemin collider'ı; 25 numaralı kararla `StackBounds.prefab` yerine `StackArea` üretiyor | Model gerektirmeden objelerin dağılmasını engeller |
| 7b | Level açılışında objeler yukarıdan dökülür; yığın oturunca (rigidbody sleep) süre başlar | Oyuncu sayaç işlerken yerleşmeyi beklemesin |
| 8 | Bant kapalı devre bir turdur, kutular üzerinde sürekli döner | GDD "levelin kutu havuzundan yeni bir kutu banta gelir" diyor ama kutunun bantta ne yaptığını açmıyor; sahnedeki model kapalı devre konveyör olduğu için cümle bu yönde açıldı |
| 9 | Kutu taşıma sistemi: waypoint yolu + sanal slot karuseli | Kutular arası mesafe kendiliğinden sabit kalır; hız, duraklatma ve Time Freeze tek değişkenle yönetilir. Elenen alternatifler: kutu başına serbest ilerleme (çakışma mantığı gerekir), DOTween `DOPath` (kutuyu tur ortasında çıkarmak kırılgan), animasyon klibi (hız/gecikme runtime'da ayarlanamaz) |
| 9a | Slotlar sanaldır; `ConveyorSlot` MonoBehaviour'ı ve prefabı kaldırıldı | Slot bir GameObject olmayınca havuzdan alınmasına ve transform senkronuna gerek kalmıyor; konumu `ConveyorPath` veriyor |
| 10 | Kutu banta tek bir giriş noktasından, bant dışından belirli bir yönden katılır | Kutunun boşlukta belirmesi yerine banda giriyormuş gibi görünmesi için |
| 10a | Tamamlanan kutu turu beklemez; bulunduğu yerde banttan ayrılıp çıkış noktasına gider ve orada kaybolur | Oyuncuyu kutunun turu tamamlamasını beklemeye zorlamamak için |
| 10b | Kapasiteye yalnızca doldurulabilir kutular sayılır; kutu üçüncü objesini aldığı anda kapasiteden düşer | Yeni kutunun yolu anında açılır, bantta daima `conveyorCapacity` kadar oynanabilir kutu bulunur |
| 11 | Yeni kutu, yığında kalan obje sayısı banttaki boş yuvalardan fazlaysa gönderilir | Level sonunda gereksiz boş kutu gelmesini engeller; toplam obje = `targetBoxCount * 3` kuralıyla birlikte artık kutu da artık obje de kalmaz |
| 12 | Objenin uçuş hedefi hareketlidir (kutunun o anki yuvası) | Kutu uçuş sırasında ilerlediği için sabit noktaya uçan obje ıskalıyordu |
| 13 | Bant modeli `LevelContext` kökünün altında durur | Kök pasif yüklendiği için dışarıda kalan model level geçişinde iki sahnede birden görünüyordu |
| 14 | Bant yolu waypoint'ler arasında centripetal Catmull-Rom ile yumuşatılır | Düz çizgi bağlamada kutunun rotasyonu her waypoint'te sıçrıyordu; köşeye waypoint eklemek sıçrama sayısını artırıyor, yumuşatmıyordu. Düzgün (uniform) parametreleme, uzun kenar ile kısa köşe parçası yan yana gelince köşede taşma yapıyor; centripetal bunu engelliyor |
| 15 | Kutunun duruşu ve yüksekliği prefab'ın kendi transform'undan gelir, config alanından değil | Tek kaynak prefab; farklı kutu prefab'ları (Joker Box) kendi duruşunu ve yüksekliğini getirebiliyor. Oyun içinde ikisi de değişmiyor, `Box` bunları `Awake`'te bir kez okuyor |
| 15a | Eksene hizalı kenarlar (x'i veya z'si aynı iki waypoint) eğriye sokulmaz, aralarından dümdüz geçilir | Kenarların hafifçe yaylanması bandın düz kısımlarını eğri gösteriyordu; yalnızca köşe parçaları yumuşatılınca düz kenarlarda sapma sıfır oluyor ve yol raydan hiç taşmıyor |
| 16 | Kutular tur boyunca dönmez, sabit yöne bakar | Dikdörtgen turda kutu 360° dönüyor ve üzerindeki ikon turun yarısında kameraya arkasını dönüyordu. Sabit duruş bunu tek yüzlü ikonla çözüyor, dört yüze ikon koymaya veya billboard'a gerek kalmıyor |
| 17 | Ekran kenarı kırmızı geri bildirimi URP Fullscreen Pass yerine tam ekran UI Image + kendi shader'ı ile yapıldı | Renderer asset'ine dokunmayı, dolayısıyla Mobile_Renderer üzerinde merge riski almayı gerektirmiyor; Image zaten var olan Canvas'ta çiziliyor ve ek render pass açmıyor |
| 18 | Titreşim için NiceVibrations yerine doğrudan Android `Vibrator` / iOS `Handheld.Vibrate` | Paket listesinde "planlı" olan NiceVibrations henüz alınmadı; hatalı hamle için tek bir kısa titreşim yetiyor ve bu ek bağımlılık gerektirmiyor |
| 19 | Ses, müzik ve titreşim tercihleri `PlayerPrefs`'e ayrı anahtar olarak değil `PlayerData` içine yazıldı | Kayıt tek noktadan (SaveManager) yürüsün; Cloud Save'e geçince ayarlar da kendiliğinden taşınır |
| 20 | Sınırsız can `PlayerData.InfiniteLivesUntilTime` (UTC tick) ile tutulur | Market paketleri saat bazlı sınırsız can veriyor; kalan süre yerine bitiş anı tutulunca oyun kapalıyken geçen süre ayrıca hesaplanmıyor |
| 21 | Can bittiğinde Start butonu pasifleşmez, HeartPopUp açılır | Sahnede zaten bir "OUT OF HEART" popup'ı var; pasif buton oyuncuya ne yapacağını söylemiyor |
| 22 | Ayarlar paneli ve butonu `MainMenu`'nün altından `UI_Canvas` köküne taşındı | Menüde de oyun içinde de açılabilmesi gerekiyordu; `MainMenu` level oynanırken kapandığı için panel altında kalınca erişilemiyordu. Can ve gold popup'ları zaten kökte duruyor |
| 23 | Ayar anahtarında tıklamayı `Opn`/`Cls` değil, ikisini de kaplayan `BtnHolder` alır; `Opn`, `Cls` ve yazıların `raycastTarget`'i kapatılır | Görünen tarafa basmayı zorunlu kılmak yerine anahtarın herhangi bir yerine basmak yetiyor. Ayrıca yazılar butonlardan sonra çizilip buton alanını tamamen kapladığı için tıklamayı yutuyordu |
| 24 | Yığın oturma zaman aşımı 5 sn'den 1 sn'ye indirildi | Bu süre boyunca dokunuşlar hamle üretmiyor; 5 sn oyuncuya oyun kilitlendi hissi veriyordu |
| 25 | Yığın alanı tek kaynak: `StackArea` hem doğma hacmini hem de görünmez duvarları tanımlar, `StackBounds.prefab` sahneden çıkarıldı | Duvarlar ayrı bir prefabdayken doğma ızgarası ile duvarlar birbirinden habersizdi; obje ölçeği büyüyünce yığın alanın dışına taşıyordu. Tek kutu ikisini birden besleyince taşma imkânsız hale geliyor |
| 25a | Objeler sabit ızgara yerine alan içinde çakışmasız rastgele noktalara doğar; nokta objenin kendi yarıçapıyla aranır | Izgara aralığı tek bir sayıydı ve obje ölçeği değişince elle güncellenmesi gerekiyordu |
| 25b | Alana sığmayan objeler kuyrukta bekler, yığından obje eksildikçe doğar | Küçük bir alana büyük bir level'in tüm objelerini tıkmak yerine alanın kapasitesi kadarı tutuluyor; oyuncu açısından yığın sürekli dolu görünüyor |
| 25c | `OnStackSettled` yalnızca ilk dolum durulunca yayınlanır | Her yeni doğum `IsSettled`'ı sıfırlasaydı input level boyunca aralıklı kilitlenirdi |
| 26 | Dokunuş probu ince ışın yerine ışına dik duran bir kutu (`Physics.BoxCast`); kutu genişliği ve ışına geri dönüş Inspector'dan ayarlanır | Mobilde parmak objenin kenarından birkaç piksel kaçtığında dokunuş boşa gidiyordu. Ayar olarak bırakılması, payın oyun hissine göre ayarlanabilmesi ve gerekirse eski davranışa dönülebilmesi için |
| 27 | Levellar arası geçişte sahne değiştirilmez; `GameScene` açılışta bir kez yüklenir ve hiç unload edilmez, aynı sahne yeni `LevelData` ile yeniden kurulur | Her level için sahne yükleyip boşaltmak hem bekleme hem de "önce yükle sonra sil" karmaşası getiriyordu. Tek sahne kalınca geçiş, havuzu boşaltıp içeriği yeniden üretmeye indi |
| 27a | Oyunun tek kamerası ve `AudioListener`'ı `GameScene`'e taşındı; `MainScene`'deki Main Camera silindi | 27 ile sahne hiç kapanmadığı için kamera orada güvenle durabiliyor. Menüyü de aynı kamera render ediyor, çift kamera/çift listener riski kalmıyor |
| 27b | `InputManager` ve `MatchResolver` kamerayı Inspector yerine `LevelContext` üzerinden okur | Kamera artık başka bir sahnede; Unity sahneler arası serialize edilmiş referans tutamıyor |
| 27c | `LevelContext.SetContentActive` yalnızca bant ve yığın alanını gizler, kamerayı kapatmaz | Menüde 3B level içeriği görünmemeli ama ekranı render edecek bir kamera kalmalı |
| 28 | Level kurulurken ayarlanabilir sahte süreli bir yükleme ekranı açılır; ekran, hazırlık **ve** sahte süre birlikte bitmeden kapanmaz | Anlık geçiş oyuncuya "bir şey olmadı" hissi veriyordu; sahte süre geçişe ritim veriyor. Hazırlık uzarsa beklemeye devam etmek, yarım kurulmuş bir level göstermekten iyi |
| 28a | Sayaç, yığın oturduğunda değil, yığın oturduğunda **ve** level başladığında başlar | Yığın yükleme ekranının arkasında oturuyor; eski kural sayacı ekran kapanmadan başlatıp oyuncudan süre çalıyordu |
| 29 | Kazanma şartı `LevelData.targetBoxCount` sayısı kadar kutu dolması | Eski şart "bandın bütün kutuları bitsin"di; hedef sayısı level verisinde yazdığı halde kullanılmıyordu |
| 30 | Bölüm sırası `LevelCatalog` ScriptableObject'inde tutulur, `PlayerData.CurrentLevel` bu listedeki sıra numarasıdır | "Sonraki level" için bir sıra kaynağı gerekiyordu; katalog, yeni bölüm eklemeyi listeye satır eklemeye indiriyor ve ilerleme kaydı kendiliğinden çalışıyor |
| 31 | Restart ve sonraki level butonları ayarlanabilir bir gecikmeden sonra iş yapar; gecikme boyunca panel açık kalır ve başka butonlar cevap vermez | Buton efektlerinin oynayacak zamanı olsun diye. Diğer butonların kilitlenmesi, bekleme sırasında ikinci bir geçiş başlatılmasını engelliyor |
| 32 | Booster sayıları `GameConfig`'e değil, booster başına bir `BoosterData` asset'ine yazıldı; liste `BoosterCatalog`'ta | Dört booster'ın hiç ortak alanı yok; hepsini `GameConfig`'e koymak onu booster alanlarıyla şişiriyordu. Mağaza tarafında `ShopProduct` + `ShopCatalog` kalıbı zaten aynı |
| 33 | `PlayerData.BoosterCounts` index eşlemesi `BoosterType` enum sırasıdır (0 Freeze, 1 Shuffle, 2 AutoMatch, 3 JokerBox) | Envanter dizisi zaten index bazlıydı ve `ShopProduct.BoosterRewards` de aynı sırayı kullanıyor. Araya yeni booster eklenirse eski kayıtlardaki adetler kayar; yeni booster daima listenin sonuna eklenir |
| 34 | Joker kutu bir tipe kilitlenirken aynı tipten doldurulmamış bir kutu iptal edilir: önce kuyruktaki kutu, o yoksa banttaki boş kutu (banttan ayrılıp çıkışa gider) | Level'in obje sayısı kutu hedefinin tam katıdır. Joker kutu fazladan 3 yuva açtığı için karşılığında bir kutu iptal edilmezse level sonunda objesi kalmayan, asla dolmayacak bir kutu bantta dönerdi. İptal edilecek kutu yoksa joker o tipi kabul etmez |
| 34a | Joker kutu, aynı tipten normal kutu varken seçilmez; yalnızca uygun normal kutu yokken aday olur | Oyuncunun elindeki joker, zaten yapılabilen bir hamlede harcanmasın |
| 35 | ~~Kaldırıldı (bkz. #50).~~ Time Freeze sayacı `Stop`/`Resume` ile değil yeni `LevelTimer.SetPaused` ile durdurur | `Resume(extraSeconds)` kalan süreyi devam bedelinin süresine çekiyor; donma kalan süreyi olduğu gibi korumalı |
| 36 | Her booster kendi `BoosterBehaviour` bileşenidir; `BoosterManager` yalnızca kilit/stok kontrolü yapıp etkiyi devreder | Dört etkinin tek sınıfta toplanması 200 satır kuralını aşıyordu ve bant/yığın/sayaç referanslarının hepsini tek sınıfa bağlıyordu |
| 37 | Etki uygulanamazsa (uygun hedef yok, booster zaten çalışıyor) booster envanterden düşülmez | Oyuncu hiçbir şey olmadan booster kaybetmesin |
| 38 | Oyun içi HUD (`InGame/GamePanel`) `UIManager` tarafından yalnızca `GameState.Playing` iken açılır | Panel sahnede kapalı duruyordu ve kimse açmıyordu; booster butonları hiç aktif olmuyordu. Görünürlük kuralı 07-MVP K-07 kartındaki HUD şartıyla aynı |
| 39 | Stoğu biten booster'a basılınca `BoosterPurchasePanel` o booster'ın verisiyle (ad, açıklama, ikon, adet, fiyat) açılır | Panel sahnede tek bir booster için sabit metinle duruyordu; dört booster için tek panel kullanılıyor |
| 40 | Satın alma paneli açıkken oyun yerinde durur: sayaç, bant ve dokunuş kapanır (`BoosterManager.SetGameplayPaused`) | Oyuncu satın alma yaparken süre işlemeye devam etmemeli. `Time.timeScale` yerine mevcut duraklatma yolları kullanıldı; timeScale tween'leri ve yükleme ekranını da dondururdu |
| 40a | ~~Kaldırıldı (bkz. #50).~~ Donma (Time Freeze) sürerken panel açılıp kapanırsa sayaç ve bant donmuş kalır | İki duraklatma sebebi üst üste geldiğinde panelin kapanması donmayı erken bitiriyordu |
| 41 | Satın alma bitince panel kapanır ve booster kendiliğinden kullanılır | Oyuncu zaten kullanmak için satın aldı; ikinci bir tıklama istemiyor |
| 42 | Gold yetmezse satın alma butonu gold popup'ını açar, panel açık kalır | Market paneli `MainMenu` altında olduğu için oyun içinde açılamıyor (karar #22'deki ayarlar paneliyle aynı sorun). `LevelResultScreen.ContinueWithGold` de aynı durumda gold popup'ı açıyor |
| 43 | Kazanma panelinde `Gold_Btn` sonraki bölümü başlatır (`_winNextLevelButton`), `Close_Btn` ödülü alıp menüye döner | Buton hem sahnedeki OnClick'ten `NextLevel`'ı hem koddan `ClaimAndReturnToMenu`'yu çağırıyordu; iki iş tek butona bağlıydı. Artık tek kaynak koddaki listener, sahnedeki kopya çağrı kaldırıldı |
| 47 | ~~Kaldırıldı (bkz. #50).~~ Time Freeze yalnızca sayacı durdurur; bant ve kutular dönmeye devam eder (`IsBeltFrozen` kapalı) | GDD'den türetilen ilk kural bandı da durduruyordu, oynanışta bandın da donması booster'ı "her şeyi dondur"a çevirip hamle yapılacak zamanı da öldürüyordu. Alan yerinde bırakıldı, istenirse asset'ten tekrar açılabilir |
| 47a | ~~Kaldırıldı (bkz. #50).~~ Sayacın ve bandın durma kararı tek noktada hesaplanır (`BoosterManager.ApplyHolds`) | İki kaynak var: satın alma paneli ve Time Freeze. Her biri kendi başına sayaca/banda yazınca, biri bittiğinde diğerinin kısıtını da kaldırıyordu (panel kapanınca bant donmadığı halde duruyordu). Freeze artık yalnızca kendi durumunu tutuyor |
| 46 | Bandın doku offseti hem açılışta hem çıkışta sıfırlanır (`trail.Awake` / `trail.OnDestroy`) | Offset paylaşılan materyal asset'ine yazıldığı için editörde kalıcı oluyordu: her oturum bir öncekinin bıraktığı yerden başlıyor, `.mat` dosyası da her oynayışta değişiyordu. Çıkışta da sıfırlanınca asset daima 0'da duruyor. Kalıcı çözüm materyali runtime'da kopyalamak veya `MaterialPropertyBlock` kullanmaktır; o, materyali kullanan renderer'ların da değiştirilmesini gerektirdiği için yapılmadı |
| 45 | HUD'daki kalan süre `GameplayHUD` bileşeniyle `LevelTimer.OnTimerTicked`'ten beslenir; biçim `mm:ss` | Yazı sahnede elle girilmiş sabit bir metindi ("02:00") ve sayacı kimse dinlemiyordu. Bileşen `GamePanel` üzerinde durur, panel kapanınca aboneliğini bırakır |
| 44 | Joker kutu prefab'ı `BoxJoker.prefab`, `Assets/Atakan/Testing/Box (1) Variant.prefab`'ın varyantıdır | Bant o klasördeki kutu varyantlarını kullanıyor ve yükseklik/ölçü değerleri onlarda; temel `Box.prefab`'tan türetilen kutu bandın 0.4 birim altında kalıyordu. O klasör temizlenirse varyantın temeli yeniden bağlanmalı |
| 38 | Can sayacı sınırsız can aktifken "FULL" yerine sınırsız canın bitişine kalan süreyi gösterir; süre 1 saati aşınca `s:dd:ss`, altında `dd:ss` biçimi kullanılır | Sınırsız can paketleri saat bazlı (1h/3h/6h/48h); `dd:ss` ile 48 saat "2880:00" görünüyordu. Değeri `EconomyManager.LifeTimerSeconds` üretir, UI yalnızca biçimlendirir |
| 39 | Localization script'leri `MatchPack.Localization` namespace'ine alındı; `LanguageCode` enum'u `MatchPack.Data` altında | Script'ler `_Game.Scripts.*` namespace'iyle geldi ve projede olmayan iki tipe (`LanguageCode`, `PrefKeys`) bağlıydı; derlenmiyordu. 02-Mimari'nin "namespace klasörü izler" kuralına çekildi |
| 40 | Dil seçimi tek ayar olarak `PlayerPrefs`'te tutulur, `PlayerData`'da değil | `Loc` static ve ilk `Get` çağrısında kendini kurar; o an `SaveManager` henüz ayakta olmayabilir. Diğer ayarlar (ses/müzik/titreşim) `PlayerData`'da kalır |
| 41 | Metinler tabloda key ile durur (`Resources/LocalizationTable.asset`); statik yazılar `LocalizedText` bileşeniyle, kodun yazdığı yazılar `Loc.Get` ile çözülür | Tek kaynak; eksik çeviri ekranda key olarak görünür, sessizce boş kalmaz. Fallback zinciri: seçili dil -> English -> key |
| 48 | Dokunuş probu ilk objede durmaz: ışın boyunca en fazla `_maxProbeHits` (varsayılan 2) obje toplanır, `MatchResolver` yakından uzağa deneyip gidecek kutusu olan ilk objeyi oynar | Tombul parmak: oyuncu doğru objeye bassa bile kenarından geçen komşu obje probu kapatıp hamleyi yakıyordu. Adayları sıralı denemek, nokta atışı dokunuşu bozmadan (doğru obje zaten en yakın olduğu için önce denenir) yanlış seçimi engelliyor. Adet Inspector'dan ayarlanır ki pay oyun hissine göre değiştirilebilsin |
| 49 | Obje ölçeği uçuş boyunca değişmez; yuvasına oturduğu an ezil-yaylan animasyonu (`StackItem.PlayBoxLandingScale`) oynar. Hedef boyut `ItemType.SelectedScale` çarpanıyla tipe göre ayarlanır, varsayılan 1 | Uçuş sırasında küçülen obje kutuya girmeden önce gözden kayboluyor gibi duruyordu. Animasyonu uçuşun son anlarına hizalayan bir deneme de yapıldı; oynanışta aceleci durduğu için temas anında başlatmaya geri dönüldü, abartı (0.35) ve süre (0.35 sn) yükseltilerek iniş okunur kılındı. Çarpan hedefi o anki yerel ölçek üzerinden hesaplar, yani 1'de bugünkü davranış aynen korunur ve yalnızca kutuya sığmayan tipler için düşürülür. `StackItem` havuza dönerken kendi ölçeğine geri getirilir |
| 50 | Time Freeze kaldırıldı, yerine Ek Süre (`TimeBonusBooster`, `BoosterType.TimeBonus` = 0) geldi: sayacı durdurmak yerine süreye 5 sn ekler. Butondan "+5 sn" yazısı süre yazısına uçar (`TimeBonusView` + havuzlu `FloatingText`), süre yazı varınca eklenir ve süre yazısı büyüyüp küçülür. Sayacı artık yalnızca satın alma paneli durdurur, `ApplyHolds` kaldırıldı | Oyuncu isteği. Enum değeri aynı index'te kaldığı için kayıtlardaki stok korunur. Süreyi zamanlayan booster'dır (mantık), UI yalnızca aynı süreyle yazıyı uçurur; böylece UI kural işletmez. Uçuş sırasında süre biterse süre eklenmez ve zıplama oynamaz |
| 51 | Shuffle objeleri ışınlamaz; hepsi fizik dışına alınıp (collider kapalı) yeni noktalarına tween ile kayar, hepsi varınca fizik birlikte açılır (`ItemStack.Reshuffle`, `StackItem.MoveTo`) | Oyuncu isteği. Collider kapalı olduğu için yolda birbirlerine çarpıp itişmezler; varış noktaları yine çakışmasız ayrıldığı için vardıklarında da iç içe olmazlar. Kayma sürerken yeni obje doğmaz (boş yer araması kayan objeleri göremez) ve ikinci Shuffle reddedilir. Auto-Match kayan bir objeyi alırsa o objenin kayması kesilip varmış sayılır. Rastgele boş nokta bulamayan obje (yığın sıkken 24 deneme yetmiyor) yerinde kalmaz; başka bir objenin boşalttığı yere gider (`StackArea.TryReserveAt`), ayrılabilen yer kalmazsa yine eski bir yere gider ve olası küçük çakışmayı fizik çözer |
| 52 | Bölümler `LevelBalanceConfig` eğrilerinden editör aracıyla (`LevelGenerator`) üretilir; üretici her kutuyu `Items` listesine ayrı ve karışık sırada yazar, bölümde yeni açılan tip her zaman yer alır | 50 bölümü elle dengelemek yerine tek bir tahmin modeli ayarlanıyor. Bant kutuları `Items` sırasıyla gönderdiği için tip başına tek satır, aynı tipin kutularını art arda getiriyordu |
| 53 | Banta gelen kutunun tipi `Items` sırasından değil, yığında o an duran objelerden rastgele seçilir (`Conveyor.TakeNextBoxType`); banttaki kutuların boş yuvalarına düşen objeler sayılmaz. `Conveyor.Build` yığını `LevelManager`'dan parametre olarak alır | Oyuncu isteği. Parametre olarak alınması sahneye/prefab'a yeni referans bağlamayı gerektirmiyor. Karşılanmış objeler sayılmazsa yığında 3 elma varken ikinci elma kutusu gelip doldurulamadan dönmüyor. 52 numaralı karardaki üretici karıştırması artık kutu sırasını belirlemiyor, yalnızca zararsız kaldı |
| 54 | Karar 49 geri alındı: kutuya iniş ezil-yaylan animasyonu ve `ItemType.SelectedScale` çarpanı kaldırıldı. Yerine obje yuvasına oturduktan sonra `StackItem.PlaySettleWobble` oynar; dikey (Y) ve yatay (X-Z) eksen farklı genlik ve sürelerle, ters fazda, sönümlenen sinüsle büyüyüp küçülür ve obje sonunda kendi boyutuna döner | İki eksenin birbirinden kayması tek eğrili ezilmeden daha canlı bir jöle hissi veriyor. Obje boyutu artık kutuda kalıcı olarak değişmediği için çarpana gerek kalmadı; tüm tiplerde değeri zaten 1 idi |
| 54a | Karar 54'teki kutuya oturma yaylanması (`StackItem.PlaySettleWobble`) da kaldırıldı; obje yuvasına ölçek animasyonu olmadan oturur | Oyuncu isteği: kutuya yerleşmede hiçbir boyut animasyonu olmayacak |
| 55 | Karar 10a güncellendi: dolan kutu çıkış noktasına gitmez; bulunduğu yerde banttan ayrılır, kapakları kapanır (`Box.PlayDeparture`), ardından Y ekseninde yükselirken küçülür (ölçek = 1 - katedilen yükseklik / yükselme mesafesi) ve 0 ölçeğe varınca havuza döner. Joker yüzünden iptal edilen boş kutu eski yoldan çıkışa gider. Kapaklar menteşe etrafında tek eksende açı ilerletilerek döner; kapalı açılar `kutu.fbx` geometrisinden hesaplanıp prefab'a yazıldı. Uzun kapaklar (kuzey/güney) 2° eksik kapanır ve 0.12 sn geç başlar ki kısa kapakların üstüne binsin | Quaternion ara değeri 180°'yi aşan dönüşte kısa yolu seçip kapağı kutunun içinden geçirir. Ölçek yükseklikten türetildiği için 0 ölçeğe tam yükselme mesafesinde varılır. İlk denemedeki InQuad eğrisi hareketi sona yığıp kutunun yerinde durup birden kaybolması gibi görünüyordu; InOutSine ve 0.6 sn ile küçülme baştan itibaren okunur |
| 56 | `LevelData`'daki her obje satırına bölüme özel boyut çarpanı eklendi; obje prefab ölçeği x çarpan boyutunda doğar ve yığında yer ayırma yarıçapı da aynı çarpanla büyür/küçülür. Level'larda her tip tek satırdadır (üretici tipleri tek satırda toplar; 50 level buna göre birleştirildi, tip başına adetler değişmedi). Elle aynı tip iki satıra yazılırsa ilk satırın çarpanı geçerlidir. `MatchPack/Generate Levels` yeniden çalışınca çarpanlar tipe göre korunur | Tip bazlı boyut (karar 49'daki `ItemType.SelectedScale`) tüm bölümleri etkiliyordu; boyut ayarı bölüm dengesinin parçası olduğu için `LevelData`'da tutulur. Obje kutuya dünya ölçeğini koruyarak girdiği için kutuda da çarpanlı boyutuyla durur |
| 57 | Obje kutuya yığın boyutuyla değil, yuva hacmine sığdırılarak girer (`Box.GetSlotPose`): renderer bounds'u oranı bozulmadan hacmin en dar ekseninin %90'ına ölçeklenir, hacmin ortasına hizalanıp tabanına oturtulur. Uzun ekseni hacmin uzun eksenine çevirmek daha büyük sığdırıyorsa obje döndürülür, yoksa düz kalır. Ölçek uçuş boyunca geçer, inişte ayrı animasyon yoktur. Kutudaki boyut bölüm çarpanından (karar 56) bağımsızdır. Yuvalar kutu iç hacmini üçe bölen hücrelerin merkezine taşındı; hacim `kutu` modelinin prefab'taki ölçeği (0.3 / 0.5 / 0.375) ve eğimi (−50°) üzerinden hesaplandı, model ölçeği değişirse yeniden hesaplanmalı | 50 objenin mesh boyutları çok farklı; yığın boyutuyla giren objeler kutudan taşıyor ya da içinde kayboluyordu. Eski yuva pivotları kutu kenarına çok yakın olduğu için objeler dışarıda duruyormuş gibi görünüyordu. Hücre tabanı ikon düzlemine alındı, yoksa tabandaki tip ikonu objelerin içinden geçerdi |

## Açık sorular

Cevaplanınca yukarıdaki tablolara taşı ve buradan sil.

- [ ] Level sayısı hedefi (yayın için kaç bölüm)?
- [ ] IAP paket fiyatları ve içerikleri.
- [ ] Reklam SDK'sı: Unity Ads mi AppLovin MAX mi?
- [ ] Kamera açısı sabit mi, level'e göre değişiyor mu?
- [ ] Bant hızı level'e göre değişecek mi (`LevelData`'ya alan mı, `GameConfig`'te sabit mi)?
- [ ] Dolmamış kutu turu tamamlayıp çıkış noktasından tekrar geçtiğinde bir geri bildirim
      verilecek mi (ses/parlama), yoksa sessizce mi devam edecek?
