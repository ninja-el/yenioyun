# 07 — MVP Görev Listesi

Oyunun oynanabilir hale gelmesi için kalan işler. Her başlık bir Trello kartıdır; kartlar
`05-Gorev-Sablonu.md` biçimindedir ve sonunda AI asistanına doğrudan yapıştırılacak prompt vardır.
Kartlar birbirini beklemez; "Bağımlılık" satırındaki not yalnızca "bu bitmeden uçtan uca test
edilemez" anlamına gelir.

Sıra önerisi: F0 kartları oyunun şu an oynanamamasının sebebidir, önce onlar biter.

| # | Faz | Kart | Sahne dosyasına dokunur mu |
|---|---|---|---|
| K-01 | F0 | [Gameplay] Level sahnesi yerleşimi ve bant modelinin bağlanması | Evet (`GameScene`) |
| K-11 | F0 | [Gameplay] Dönen bant sistemi (ConveyorPath + slot karuseli) | Evet (`GameScene`) |
| K-13 | F0 | [Gameplay] Hareketli kutuya uçuş | Hayır |
| K-02 | F0 | [Art] Kutu ve obje görselleştirmesi | Hayır |
| K-03 | F0 | [UI] Level sonu akışı ve UIManager | Evet (`MainScene`) |
| K-04 | F0 | [UI] Oyun içi HUD | Evet (`MainScene`) |
| K-05 | F0 | [Gameplay] Hatalı hamle geri bildirimi | Hayır |
| K-06 | F0 | [Gameplay] Kutu gönderme kuralının doğrulanması | Hayır |
| K-07 | F1 | [Meta] EconomyManager: gold ve can | Evet (`MainScene`) |
| K-08 | F1 | [Meta] Level ilerlemesi ve LevelCatalog | Hayır |
| K-09 | F1 | [Meta] AudioManager ve SFX bağlantıları | Evet (`MainScene`) |
| K-10 | F1 | [Gameplay] Kameranın CameraAnchor'a yerleşmesi | Evet (`MainScene`) |
| K-12 | F2 | [Tooling] StackBounds ölçüsünün dokümanla eşitlenmesi | Hayır |

**K-01 ve K-11 birlikte yürür ve ikisi de `GameScene`'e dokunur; aynı anda iki kişiye verme.**
K-11 bittiğinde `ConveyorSlot.cs` ve `ConveyorSlot.prefab` silinmiş olur (karar 9a).

Aynı sahne dosyasına dokunan kartları (K-03, K-04, K-07, K-09, K-10) paralel verme; merge
çakışması çıkar. Sırayla tek kişiye/tek oturuma ver.

---

---

## K-01 — [Gameplay] Level sahnesi yerleşimi ve bant modelinin bağlanması

    Başlık: [Gameplay] Level sahnesi yerleşimi ve bant modelinin bağlanması

    Amaç
    - GameScene'de ConveyorRoot, StackRoot, StackBounds ve CameraAnchor'ın hepsi (0,0,0)'da;
      ayrıca bant modeli ("world" objesi) LevelContext kökünün dışında, sahne kökünde duruyor.
      Bu iş bitince yığın bandın turunun ortasında, bant modeli kökün altında ve kamera ikisini
      birlikte çerçeveliyor.

    Kapsam
    - Assets/_Project/Scenes/GameScene.unity (tek sahne, tek kart)
    - Assets/_Project/Prefabs/Gameplay/StackBounds.prefab (yalnızca konum gerekiyorsa)
    - Kapsam DIŞI: kod değişikliği, bant yolu noktaları (K-11), kamera hareketi (K-10),
      kutu görseli (K-02).

    Kabul kriterleri
    - [ ] "world" objesi ConveyorRoot altına taşınmış; sahnede Level'dan başka kök obje yok.
    - [ ] StackRoot bandın turunun ortasında, StackBounds zemininin üstünde.
    - [ ] StackBounds collider'ları bant modeliyle ve turun geçtiği şeritle kesişmiyor.
    - [ ] CameraAnchor turun tamamını ve yığını birlikte çerçeveliyor; bandın hiçbir kenarı
          ekran dışında kalmıyor (kutular tur boyunca hep görünür olmak zorunda).
    - [ ] Play Mode'da 18 obje yığın alanına düşüyor, hiçbiri duvarlardan taşmıyor.
    - [ ] Level root objesi sahnede hâlâ pasif (m_IsActive: 0) kayıtlı.

    Bağımlılık
    - Yok. K-11 bu yerleşimin üstüne kurulur.

    Notlar
    - "world" objesinin ölçeği 100; model 1/100 boyutunda çizilmiş. Taşırken dünya ölçeğini
      bozma, ConveyorRoot ölçeği 1 kalmalı.
    - Bant modelinin kök altına alınma gerekçesi 06-Sabitler-ve-Kararlar.md karar 13.
    - Kutular tur boyunca görünür kalmalı; kamera çerçevesi bu kartta buna göre seçilir.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: GameScene'de level yerleşimini kur. Şu an ConveyorRoot, StackRoot, StackBounds ve
    CameraAnchor'ın hepsi (0,0,0)'da, ayrıca bant modeli olan "world" objesi LevelContext
    kökünün (Level) dışında sahne kökünde duruyor. "world"ü ConveyorRoot altına taşı,
    StackRoot'u bandın turunun ortasına yerleştir, StackBounds'u yığının altına oturt ve
    CameraAnchor'ı turun tamamını + yığını çerçeveleyen izometrik açıya al.

    Kısıtlar:
    - Sadece bu kapsam. Kod değişikliği yok, bant yolu noktalarını koyma (o K-11), yeni paket yok.
    - Yalnızca Assets/_Project/Scenes/GameScene.unity dosyasına dokun; MainScene'e dokunma.
    - Level root objesi sahnede pasif kayıtlı kalmalı.
    - "world" objesinin ölçeği 100; taşırken dünya ölçeğini bozma.
    - Sayısal değerler 06-Sabitler-ve-Kararlar.md'den gelir, sayı uydurma.

    Teslim: değiştirilen transform'ların yeni değerleri + Unity'de elle test adımları +
    06-Sabitler'e eklenmesi gereken yeni konum kararları.

---

## K-02 — [Art] Kutu ve obje görselleştirmesi

    Başlık: [Art] Kutu ve obje görselleştirmesi

    Amaç
    - Box.prefab'ın hiç mesh'i yok ve üç ItemType asset'inin de _icon alanı boş; oyuncu hangi
      kutunun hangi objeyi istediğini göremiyor, bu yüzden oyun oynanamıyor. Bu iş bitince her
      kutu istediği objenin ikonunu ve doluluğunu gösterir.

    Kapsam
    - Assets/_Project/Prefabs/Gameplay/Box.prefab
    - Assets/_Project/Data/Items/Item_Apple.asset, Item_Ball.asset, Item_Car.asset (_icon alanı)
    - Assets/_Project/Scripts/Gameplay/Box.cs (yalnızca görsel bağlama)
    - Yeni: Assets/_Project/Art/UI/Icons/ altına 3 ikon
    - Kapsam DIŞI: kutu giriş/çıkış hareketi (K-11), ses (K-09), yeni obje tipi eklemek.

    Kabul kriterleri
    - [ ] Box.prefab'ın gövde mesh'i ve ikon gösteren bir yüzeyi var.
    - [ ] İkon izometrik kameradan okunuyor. Kutu tur boyunca dönmediği (karar 16) için tek bir
          yüze konan ikon yeterlidir; dört yüz veya billboard gerekmez.
    - [ ] Box.Setup(type) çağrıldığında ikon o tipin ItemType.Icon'una geçiyor.
    - [ ] Kutuya obje oturdukça doluluk görünüyor (3 yuvanın hangisinin dolduğu ayırt ediliyor).
    - [ ] Üç ItemType asset'inin de _icon alanı dolu.
    - [ ] Kutu havuza iade edilip tekrar alındığında eski ikon/doluluk kalıntısı kalmıyor
          (Box.OnSpawned görseli de sıfırlıyor).

    Bağımlılık
    - Yok. Test için K-01 yerleşimi kolaylık sağlar.

    Notlar
    - ItemType bir ScriptableObject, ikon oradan okunur; Box içinde tip başına switch/if yazma.
    - Kutu kapasitesi GameConfig.BoxCapacity'den gelir, 3 sayısı koda gömülmez.
    - Obje prefabları (Apple_Item, Basketball_Item, Car_Item) gerçek modellerle değişti;
      ikonların bu modellerle eşleşmesi gerekiyor.
    - Bant modeli zaten sahnede; ayrı bir bant görseli ya da slot zemini üretilmeyecek.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Box.prefab'ı görünür hale getir ve kutunun istediği obje tipini oyuncuya göster.
    Şu anda Box.prefab'da sadece üç boş ItemSlot transform'u var, mesh ve ikon yok; ayrıca
    Item_Apple/Item_Ball/Item_Car asset'lerinin _icon alanları boş. Kutuya gövde mesh'i ve
    ItemType.Icon'u gösteren bir yüzey ekle, Box.Setup çağrıldığında ikonu o tipe geçir,
    kutunun doluluğunu görsel olarak belli et. Üç ItemType asset'ine de ikon bağla.
    Kutu bant turunda dönmüyor, sabit yöne bakıyor; ikon için tek yüz yeterli.

    Kısıtlar:
    - Sadece bu kapsam. Kutu giriş/çıkış hareketi, ses ve yeni obje tipi bu kartta yok.
    - Sahne dosyalarına dokunma; iş prefab ve asset üzerinde yapılır.
    - Kutu kapasitesi GameConfig.BoxCapacity'den okunur, koda 3 yazma.
    - Box içinde ItemType'a göre switch/if yazma; görsel veriyi ItemType asset'inden al.
    - Kutu havuzdan tekrar alındığında görsel durumu sıfırlanmalı (OnSpawned).
    - Yeni paket yok.

    Teslim: değişen dosya listesi + ikon yerleşimi için seçtiğin çözüm ve gerekçesi +
    Inspector'da yapılacak bağlamalar + elle test adımları + varsayımların.

---

## K-03 — [UI] Level sonu akışı ve UIManager

    Başlık: [UI] Level sonu akışı ve UIManager

    Amaç
    - GameManager.OnLevelCompleted ve OnLevelFailed event'lerini şu an kimse dinlemiyor; level
      bitince ekranda hiçbir şey olmuyor ve menüye dönüş yok, oyun kilitleniyor. Bu iş bitince
      kazanma/kaybetme paneli açılıyor ve oyuncu menüye ya da yeniden denemeye gidebiliyor.

    Kapsam
    - Yeni: Assets/_Project/Scripts/Core/UIManager.cs
    - Yeni: Assets/_Project/Scripts/UI/LevelResultScreen.cs
    - Assets/_Project/Scenes/MainScene.unity (UI_Canvas altına paneller + UIManager objesi)
    - Assets/_Project/Scripts/UI/MainMenuScreen.cs (panel yönetimi UIManager'a taşınıyorsa)
    - Kapsam DIŞI: gold ödülü ve can tüketimi (K-07), reklam, HUD (K-04).

    Kabul kriterleri
    - [ ] Tüm kutular dolunca Win paneli açılıyor, süre bitince Lose paneli açılıyor.
    - [ ] Win panelindeki "Devam" ve Lose panelindeki "Menü" butonları GameManager.ReturnToMenu()
          çağırıyor; level sahnesi boşalıyor ve ana menü paneli geri geliyor.
    - [ ] Lose panelinde "Tekrar Dene" aynı LevelData ile yeni level başlatıyor.
    - [ ] Panel açıkken oyun alanına dokunmak yeni hamle üretmiyor.
    - [ ] Menüye dönüldükten sonra tekrar Start'a basınca level baştan, kalıntısız kuruluyor
          (yığın objeleri, kutular, süre sıfırdan).
    - [ ] Console'da error/warning yok; abonelikler OnDisable/OnDestroy'da bırakılıyor.

    Bağımlılık
    - Yok.

    Notlar
    - UIManager yalnızca panel aç/kapa yapar; kural işletmez (CLAUDE.md kural 6).
    - Paneller GameManager.OnGameStateChanged'e abone olur, Update'te state yoklamaz.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Level sonu akışını kur. Şu an GameManager.OnLevelCompleted / OnLevelFailed
    event'lerini kimse dinlemiyor, level bitince oyun kilitleniyor. 02-Mimari.md'de tanımlı
    UIManager'ı (MainScene'de singleton, sadece panel aç/kapa) ve bir LevelResultScreen
    panelini ekle. Kazanınca Win, süre bitince Lose paneli açılsın; Win'de "Devam", Lose'da
    "Menü" ve "Tekrar Dene" butonları olsun. Menüye dönüş GameManager.ReturnToMenu()
    üzerinden yapılsın.

    Kısıtlar:
    - Sadece bu kapsam. Gold ödülü, can tüketimi, reklam ve HUD bu kartta yok.
    - UIManager oyun mantığı içermez; event dinler, panel gösterir.
    - Paneller state'i Update içinde yoklamaz; OnGameStateChanged'e abone olur ve
      OnDisable/OnDestroy'da aboneliği bırakır.
    - Yeni paket yok. Yalnızca MainScene.unity'ye dokun, GameScene'e dokunma.
    - Klasör ve namespace 02-Mimari.md'deki yapıya uymalı
      (Core/UIManager.cs, UI/LevelResultScreen.cs).

    Teslim: değişen dosya listesi + MainScene'de kurulan hiyerarşi ve Inspector bağlamaları +
    elle test adımları (kazanma, kaybetme, tekrar deneme, menüye dönüp yeniden başlatma).

---

## K-04 — [UI] Oyun içi HUD

    Başlık: [UI] Oyun içi HUD

    Amaç
    - LevelTimer.OnTimerTicked'i dinleyen kimse yok; oyuncu kalan süreyi ve kaç kutu kaldığını
      göremiyor. Bu iş bitince level boyunca süre ve kalan kutu sayısı ekranda görünür.

    Kapsam
    - Yeni: Assets/_Project/Scripts/UI/GameplayHUD.cs
    - Assets/_Project/Scenes/MainScene.unity (UI_Canvas altına HUD paneli)
    - Assets/_Project/Scripts/Gameplay/Conveyor.cs (kalan kutu sayısını dışa açmak gerekirse)
    - Kapsam DIŞI: booster butonları, gold/can göstergesi (K-07), pause menüsü.

    Kabul kriterleri
    - [ ] Kalan süre saniye olarak görünüyor ve her saniye güncelleniyor.
    - [ ] Süre kritik eşiğin altına inince göstergenin rengi değişiyor.
    - [ ] Kalan kutu sayısı (bantta duran + kuyruktaki) görünüyor ve kutu doldukça azalıyor.
    - [ ] HUD yalnızca GameState.Playing iken görünüyor; menüde ve level sonu panelinde kapalı.
    - [ ] HUD hiçbir değeri Update içinde yoklamıyor; event'lerle güncelleniyor.
    - [ ] Level bitip yeniden başlatıldığında HUD kalıntısız sıfırlanıyor.

    Bağımlılık
    - Yok. K-03 bittiyse panel görünürlüğü UIManager üzerinden yönetilir.

    Notlar
    - Süre uyarı eşiği yeni bir sayısal değer: GameConfig'e alan olarak ekle ve
      06-Sabitler-ve-Kararlar.md'ye yaz.
    - 1080x1920 portre, Canvas Scaler match 0.5.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Oyun içi HUD ekle. Şu an LevelTimer.OnTimerTicked ve Conveyor'ın kutu event'lerini
    dinleyen bir UI yok. Kalan süreyi ve kalan kutu sayısını gösteren bir GameplayHUD yaz,
    MainScene'deki UI_Canvas altına kur. HUD yalnızca GameState.Playing iken görünsün.

    Kısıtlar:
    - Sadece bu kapsam. Booster butonu, gold/can göstergesi ve pause bu kartta yok.
    - UI oyun mantığı içermez; event dinler, sonuç gösterir. Update içinde state yoklama yok.
    - Süre uyarı eşiği gibi yeni bir sayı gerekiyorsa GameConfig'e alan olarak ekle ve
      06-Sabitler-ve-Kararlar.md'yi güncelle; koda gömme.
    - Yalnızca MainScene.unity'ye dokun.
    - Conveyor'a yeni bir public üye eklemen gerekiyorsa en küçük yüzeyi seç ve gerekçesini yaz.

    Teslim: değişen dosya listesi + MainScene hiyerarşisi ve Inspector bağlamaları +
    elle test adımları + 06-Sabitler'e eklenen değerler.

---

## K-05 — [Gameplay] Hatalı hamle geri bildirimi

    Başlık: [Gameplay] Hatalı hamle geri bildirimi

    Amaç
    - MatchResolver.Miss() şu an yalnızca kamerayı sarsıyor; GDD'de tarif edilen kırmızı outline
      geri bildirimi ve opsiyonel süre cezası yok. Bu iş bitince oyuncu hamlesinin neden
      tutmadığını objenin üstünden anlar.

    Kapsam
    - Assets/_Project/Scripts/Gameplay/MatchResolver.cs
    - Assets/_Project/Scripts/Gameplay/StackItem.cs (yalnızca parlama arayüzü)
    - Assets/_Project/Scripts/Gameplay/LevelTimer.cs (yalnızca süre cezası API'si)
    - Assets/_Project/Prefabs/Gameplay/Items/*.prefab (outline materyali)
    - Kapsam DIŞI: ses (K-09), haptics, yeni VFX paketi.

    Kabul kriterleri
    - [ ] Uygun kutusu olmayan bir objeye dokununca obje kırmızı parlıyor ve kısa sürede
          normale dönüyor.
    - [ ] Aynı objeye üst üste dokunmak parlamayı üst üste bindirmiyor
          (tween DOKill ile yeniden başlıyor).
    - [ ] Kamera sarsıntısı 06-Sabitler'deki 0.2 sn / 0.15 şiddet değerleriyle çalışıyor.
    - [ ] GameConfig.IsMissPenaltyEnabled açıkken MissesBeforePenalty kadar hatadan sonra
          süreden MissPenaltySeconds düşülüyor; bayrak kapalıyken süre hiç etkilenmiyor.
    - [ ] Hata sayacı her level başında sıfırlanıyor.
    - [ ] Obje havuza dönerken parlama tween'i DOKill ile durduruluyor, bir sonraki kullanımda
          kırmızı kalmıyor.

    Bağımlılık
    - Yok.

    Notlar
    - Outline için projede MK Toon var; yeni paket ekleme, mevcut materyal/shader ile çöz
      ya da renk tween'i kullan.
    - Ceza değerleri GameConfig'te zaten tanımlı, yeni alan açma.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Hatalı hamle geri bildirimini tamamla. MatchResolver.Miss() şu an yalnızca kamerayı
    sarsıyor. Dokunulan objenin kırmızı parlayıp normale dönmesini ekle ve GameConfig'te zaten
    tanımlı olan miss penalty alanlarını (IsMissPenaltyEnabled, MissesBeforePenalty,
    MissPenaltySeconds) işlet: bayrak açıkken belirlenen hata sayısından sonra LevelTimer'dan
    süre düşülsün. Hata sayacı her level başında sıfırlansın.

    Kısıtlar:
    - Sadece bu kapsam. Ses, haptics ve yeni VFX bu kartta yok.
    - Yeni paket ekleme; outline için projedeki MK Toon materyallerini veya renk tween'ini kullan.
    - GameConfig'e yeni alan açma; değerler orada mevcut.
    - Sahne dosyalarına dokunma.
    - Tween'ler obje havuza dönerken DOKill ile durdurulmalı; parlama bir sonraki kullanıma
      taşmamalı.

    Teslim: değişen dosya listesi + prefablarda gereken materyal bağlamaları + elle test
    adımları (ceza açık ve kapalı iki senaryo) + varsayımların.

---

## K-06 — [Gameplay] Kutu gönderme kuralının doğrulanması

    Başlık: [Gameplay] Kutu gönderme kuralının doğrulanması

    Amaç
    - Level boyunca gönderilen toplam kutu sayısı LevelData.targetBoxCount olmak zorunda ve
      artık obje kalmamalı. Tutarsız bir LevelData bunu sessizce bozabilir; level ya erken
      biter ya hiç bitmez. Bu iş bitince tutarsızlık runtime'da net bir hatayla yakalanır.

    Kapsam
    - Assets/_Project/Scripts/Gameplay/Conveyor.cs
    - Assets/_Project/Scripts/Data/LevelData.cs (yalnızca doğrulama yardımcıları)
    - Kapsam DIŞI: Level Editor penceresi, yeni level asset'leri, bant hareketi (K-11).

    Kabul kriterleri
    - [ ] Level kurulurken üretilecek toplam kutu sayısı LevelData.TargetBoxCount ile
          karşılaştırılıyor; uyuşmazsa tek ve anlaşılır bir Debug.LogError basılıyor.
    - [ ] Bir obje tipinin adedi BoxCapacity'nin tam katı değilse hata basılıyor.
    - [ ] Level bittiğinde gönderilmiş kutu sayısı TargetBoxCount'a eşit; fazla kutu gelmemiş.
    - [ ] Level bittiğinde yığında obje kalmamış ve bantta dolmamış kutu kalmamış.
    - [ ] Level_001 (3 tip x 6 adet, hedef 6 kutu) hiçbir uyarı üretmeden kuruluyor.
    - [ ] Gameplay döngüsünde Debug.Log kalmıyor; log yalnızca kurulum anında ve hata için.

    Bağımlılık
    - K-11 bitmeden "fazla kutu gelmedi" kriteri uçtan uca test edilemez.

    Notlar
    - Gönderme kuralı: yığında kalan obje sayısı, bantta olan kutuların toplam boş yuvasından
      fazlaysa yeni kutu gönderilir (01-Oyun-Ozeti.md "Bant kuralları").
    - Toplam obje = targetBoxCount * BoxCapacity kuralı ihlal edilemez.
    - LevelData.OnValidate zaten editörde uyarıyor; bu kart runtime tarafını kapatıyor.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Conveyor'ın kutu gönderme kuralını doğrula. Level kurulurken üretilecek toplam kutu
    sayısını LevelData.TargetBoxCount ile karşılaştır, her tipin adedinin
    GameConfig.BoxCapacity'nin tam katı olduğunu kontrol et, uyuşmazlıkta tek ve anlaşılır bir
    Debug.LogError bas. Ayrıca level bittiğinde gönderilen kutu sayısının TargetBoxCount'a eşit
    olduğunu, yığında obje ve bantta dolmamış kutu kalmadığını doğrulayan kontrolü ekle.

    Kısıtlar:
    - Sadece bu kapsam. Level Editor penceresi, yeni level asset'i ve bant hareketi bu kartta yok.
    - Sahne ve prefablara dokunma.
    - Gameplay döngüsüne Debug.Log ekleme; log yalnızca level kurulumunda ve yalnızca hata için.
    - Mevcut event ve public API imzalarını değiştirme.

    Teslim: değişen dosya listesi + elle test adımları (doğru Level_001 ve kasten bozulmuş bir
    LevelData ile) + varsayımların.

---

## K-07 — [Meta] EconomyManager: gold ve can

    Başlık: [Meta] EconomyManager: gold ve can

    Amaç
    - 02-Mimari.md'de tanımlı EconomyManager henüz yok; level girişi can tüketmiyor, kazanınca
      gold verilmiyor. Bu iş bitince can ve gold PlayerData üzerinden işliyor ve oyun kapalıyken
      de can yenileniyor.

    Kapsam
    - Yeni: Assets/_Project/Scripts/Meta/EconomyManager.cs
    - Assets/_Project/Scenes/MainScene.unity (Core altına manager objesi)
    - Assets/_Project/Scripts/UI/MainMenuScreen.cs (Start butonunun can kontrolü)
    - Kapsam DIŞI: IAP, reklam, booster envanteri, gold harcama ekranı.

    Kabul kriterleri
    - [ ] İlk açılışta can GameConfig.MaxLives değerine çekiliyor ve kaydediliyor.
    - [ ] Start'a basınca 1 can düşüyor; can 0 iken Start butonu pasif.
    - [ ] Level kazanılınca GameConfig.LevelCompleteGold kadar gold ekleniyor ve kaydediliyor.
    - [ ] Can, GameConfig.LifeRegenSeconds'ta bir yenileniyor; oyun kapatılıp açıldığında
          geçen süre lastLifeRegenTime üzerinden hesaplanıp eksik canlar toplu veriliyor.
    - [ ] Can MaxLives'ı geçmiyor ve dolu iken lastLifeRegenTime ileri kaymıyor.
    - [ ] OnGoldChanged ve OnLivesChanged event'leri yayınlanıyor; UI bunları dinleyebiliyor.
    - [ ] Cihaz saati geri alındığında can sayısı artmıyor.

    Bağımlılık
    - Yok. Kazanma anını yakalamak için GameManager.OnLevelCompleted kullanılır.

    Notlar
    - Değerler GameConfig'te mevcut (MaxLives, LifeRegenSeconds, LevelCompleteGold);
      yeni sayı açma.
    - PlayerData alanları hazır: currentLives, lastLifeRegenTime, gold.
    - Kayıt SaveManager üzerinden; PlayerPrefs'e doğrudan yazma.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: 02-Mimari.md'de tanımlı ama henüz yazılmamış EconomyManager'ı ekle. Gold ve canı
    PlayerData üzerinden yönetsin: level girişinde 1 can tüketilsin, can 0 iken level
    başlatılamasın, level kazanılınca GameConfig.LevelCompleteGold eklensin, can
    GameConfig.LifeRegenSeconds'ta bir yenilensin ve oyun kapalıyken geçen süre
    PlayerData.LastLifeRegenTime üzerinden offline hesaplansın. OnGoldChanged ve
    OnLivesChanged event'lerini yayınla.

    Kısıtlar:
    - Sadece bu kapsam. IAP, reklam, booster envanteri ve gold harcama ekranı bu kartta yok.
    - Değerler GameConfig'ten okunur; yeni sayısal alan açma, koda sayı gömme.
    - Kayıt/okuma yalnızca SaveManager üzerinden; PlayerPrefs'e doğrudan yazma.
    - Singleton kalıbı diğer manager'larla aynı olmalı
      (public static Instance + Awake + DontDestroyOnLoad).
    - Klasör ve namespace: Scripts/Meta, MatchPack.Meta.
    - Yalnızca MainScene.unity'ye dokun.
    - Cihaz saati geri alındığında can üretilmemeli; bu durumu nasıl ele aldığını yaz.

    Teslim: değişen dosya listesi + MainScene'de kurulan obje ve Inspector bağlamaları +
    elle test adımları (can tüketme, can 0 senaryosu, offline yenilenme) + varsayımların.

---

## K-08 — [Meta] Level ilerlemesi ve LevelCatalog

    Başlık: [Meta] Level ilerlemesi ve LevelCatalog

    Amaç
    - MainMenuScreen'de tek bir LevelData elle bağlı; PlayerData.CurrentLevel hiç kullanılmıyor
      ve ikinci bir bölüm yok. Bu iş bitince oyuncu bölümleri sırayla oynar ve ilerleme
      kaydedilir.

    Kapsam
    - Yeni: Assets/_Project/Scripts/Data/LevelCatalog.cs (ScriptableObject, sıralı liste)
    - Yeni: Assets/_Project/Data/Levels/LevelCatalog.asset, Level_002.asset, Level_003.asset
    - Assets/_Project/Scripts/UI/MainMenuScreen.cs
    - Assets/_Project/Scripts/Core/GameManager.cs (yalnızca sıradaki level seçimi)
    - Kapsam DIŞI: harita/level seçim ekranı, yıldız/puan sistemi, Level Editor penceresi.

    Kabul kriterleri
    - [ ] LevelCatalog sıralı bir LevelData listesi tutuyor ve index'e göre level veriyor.
    - [ ] Start, PlayerData.CurrentLevel'a karşılık gelen bölümü açıyor.
    - [ ] Level kazanılınca CurrentLevel artıyor ve kaydediliyor; kaybedilince artmıyor.
    - [ ] Son bölüm bitince oyun hata vermeden davranıyor (son bölüm tekrar oynanır veya
          "yakında" durumu gösterilir — hangisi seçildiyse kartta yazılı).
    - [ ] Level_002 ve Level_003 asset'leri obje sayısı = targetBoxCount * BoxCapacity kuralına
          uyuyor ve Console'da OnValidate uyarısı üretmiyor.
    - [ ] Katalogda boş (null) satır varsa kurulumda anlaşılır bir hata basılıyor.

    Bağımlılık
    - K-03 (level sonu akışı) bitmeden "Devam" butonuyla uçtan uca test edilemez.

    Notlar
    - Zorluk artışı için süre ve kutu hedefi değerleri 06-Sabitler'deki varsayılanlardan
      türetilir; yeni bir eğri kararı verilirse dokümana yazılır.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Level ilerlemesini kur. Şu an MainMenuScreen'de tek bir LevelData elle bağlı ve
    PlayerData.CurrentLevel hiç kullanılmıyor. Sıralı LevelData listesi tutan bir LevelCatalog
    ScriptableObject'i ekle, Start butonu PlayerData.CurrentLevel'a karşılık gelen bölümü açsın,
    level kazanılınca CurrentLevel artıp kaydedilsin. Level_002 ve Level_003 asset'lerini üret.

    Kısıtlar:
    - Sadece bu kapsam. Harita/level seçim ekranı, yıldız sistemi ve Level Editor penceresi
      bu kartta yok.
    - Sahne dosyalarına dokunma; MainMenuScreen'in Inspector bağlamasını teslimde tarif et.
    - Level asset'lerinde obje sayısı = targetBoxCount * GameConfig.BoxCapacity kuralı
      ihlal edilemez.
    - Zorluk eğrisi için yeni bir sayısal karar veriyorsan 06-Sabitler-ve-Kararlar.md'ye ekle.
    - Son bölüm bittiğinde ne olacağını sen seç, gerekçesini ve seçimini teslimde açıkça yaz.

    Teslim: değişen dosya listesi + yeni asset'lerin değerleri + Inspector bağlamaları +
    elle test adımları + 06-Sabitler'e eklenen kararlar.

---

## K-09 — [Meta] AudioManager ve SFX bağlantıları

    Başlık: [Meta] AudioManager ve SFX bağlantıları

    Amaç
    - 02-Mimari.md'de tanımlı AudioManager yok; oyunda hiç ses yok. Bu iş bitince eşleşme,
      hatalı hamle, kutu dolma ve level sonu sesleri çalıyor.

    Kapsam
    - Yeni: Assets/_Project/Scripts/Meta/AudioManager.cs
    - Yeni: Assets/_Project/Data/Config/AudioLibrary.asset (SFX referansları)
    - Assets/_Project/Audio/SFX/ altına ses dosyaları
    - Assets/_Project/Scenes/MainScene.unity (Core altına manager objesi)
    - Kapsam DIŞI: müzik (BGM) seçimi, ses ayar ekranı, haptics.

    Kabul kriterleri
    - [ ] Eşleşme, hatalı hamle, kutu dolma ve level kazanma/kaybetme seslerinin hepsi çalıyor.
    - [ ] AudioManager event'lere abone; ses çalma kararı gameplay sınıflarının içine dağılmıyor.
    - [ ] Arka arkaya hızlı dokunuşta ses kesilmiyor, üst üste biniyor.
    - [ ] AudioListener yalnızca MainScene'de, tek adet; Console'da çift listener uyarısı yok.
    - [ ] Ses dosyaları mobil için uygun import ayarlarıyla (Load Type / Compression) geliyor.
    - [ ] Sahne geçişinde çalan ses takılı kalmıyor.

    Bağımlılık
    - K-03 (level sonu event'leri) ile birlikte test edilir.

    Notlar
    - Yayınlanan event'ler hazır: OnItemMatched, OnItemMissed, OnBoxFilled, OnLevelCompleted,
      OnLevelFailed.
    - Ses dosyası bulunamıyorsa placeholder ile bağla ve kartta belirt.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: 02-Mimari.md'de tanımlı AudioManager'ı ekle. Mevcut event'lere abone olarak
    (OnItemMatched, OnItemMissed, OnBoxFilled, OnLevelCompleted, OnLevelFailed) SFX çalsın.
    Ses referanslarını bir AudioLibrary ScriptableObject'inde topla. Aynı anda birden fazla
    ses çalabilmeli.

    Kısıtlar:
    - Sadece bu kapsam. BGM, ses ayar ekranı ve haptics bu kartta yok.
    - Yeni paket yok; Unity'nin kendi AudioSource'u kullanılır.
    - Ses çalma kararı gameplay sınıflarının içine dağıtılmaz; AudioManager event dinler.
    - AudioListener tek ve MainScene'de kalmalı.
    - Singleton kalıbı diğer manager'larla aynı; klasör/namespace Scripts/Meta, MatchPack.Meta.
    - Yalnızca MainScene.unity'ye dokun.
    - Elinde ses dosyası yoksa placeholder bağla ve hangi seslerin gerçek asset beklediğini
      listele.

    Teslim: değişen dosya listesi + AudioLibrary alanları + Inspector bağlamaları +
    elle test adımları + eksik ses asset listesi.

---

## K-10 — [Gameplay] Kameranın CameraAnchor'a yerleşmesi

    Başlık: [Gameplay] Kameranın CameraAnchor'a yerleşmesi

    Amaç
    - LevelContext.CameraAnchor tanımlı ama kimse kullanmıyor; kalıcı kamera sabit duruyor ve
      level yerleşimi değişince çerçeve bozuluyor. Bu iş bitince kamera her level'de o levelin
      anchor'ına yerleşir.

    Kapsam
    - Yeni: Assets/_Project/Scripts/Gameplay/CameraRig.cs
    - Assets/_Project/Scenes/MainScene.unity (Main Camera'ya bileşen)
    - Kapsam DIŞI: kamera hareketi/takibi, level'e göre farklı açı kararı, cutscene.

    Kabul kriterleri
    - [ ] Level sahnesi hazır olunca kamera CameraAnchor'ın konum ve rotasyonuna geçiyor.
    - [ ] Menüye dönünce kamera menü için tanımlı varsayılan pozuna dönüyor.
    - [ ] Geçiş anında iki level sahnesi yüklüyken kamera yalnızca yeni anchor'ı hedefliyor.
    - [ ] Hatalı hamle kamera sarsıntısı (MatchResolver) bu yerleşimle çakışmıyor; sarsıntı
          bitince kamera anchor pozuna geri dönüyor.
    - [ ] Kamera ve AudioListener hâlâ tek ve MainScene'de.

    Bağımlılık
    - K-01 (anchor'ın doğru yere konması) bitmeden görsel olarak doğrulanamaz.

    Notlar
    - 02-Mimari.md kararı 1c: kamera kalıcı olarak MainScene'de kalır, level sahnesine kamera
      konmaz.
    - MatchResolver kamerayı DOShakePosition ile sarsıyor; CameraRig bu tween'le kavga etmemeli.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Kalıcı kamerayı LevelContext.CameraAnchor'a bağla. Anchor tanımlı ama kullanılmıyor.
    SceneLoader.OnLevelSceneReady'de kamera anchor'ın konum ve rotasyonuna geçsin, menüye
    dönünce menü için tanımlı varsayılan poza dönsün.

    Kısıtlar:
    - Sadece bu kapsam. Kamera takibi, level'e göre farklı açı ve cutscene bu kartta yok.
    - Kamera ve AudioListener MainScene'de tek kalmalı; GameScene'e kamera koyma.
    - MatchResolver kamerayı DOShakePosition ile sarsıyor; yeni kodun bu tween'le çakışmamalı,
      sarsıntı bitince kamera anchor pozunda olmalı. Bunu nasıl çözdüğünü yaz.
    - Yalnızca MainScene.unity'ye dokun.
    - Yeni sayısal değer (geçiş süresi vb.) gerekiyorsa [SerializeField] alan olarak aç ve
      06-Sabitler-ve-Kararlar.md'ye ekle.

    Teslim: değişen dosya listesi + Inspector bağlamaları + elle test adımları +
    kamera/sarsıntı çakışması için seçtiğin çözüm.

---

## K-11 — [Gameplay] Dönen bant sistemi (ConveyorPath + slot karuseli)

    Başlık: [Gameplay] Dönen bant sistemi (ConveyorPath + slot karuseli)

    Amaç
    - Oyunun ana mekaniği. Kutular şu an slot konumuna ışınlanıp sabit duruyor; olması gereken
      kapalı bir tur üzerinde sürekli dönmeleri. Bu iş bitince kutular banta girer, tur atar,
      dolunca çıkış noktasına ilerleyip banttan ayrılır ve gerektiğinde yenisi gelir.

    Kapsam
    - Yeni: Assets/_Project/Scripts/Gameplay/ConveyorPath.cs
    - Assets/_Project/Scripts/Gameplay/Conveyor.cs (yeniden yazılır)
    - Assets/_Project/Scripts/Data/GameConfig.cs (bant hızı ve süre alanları)
    - Assets/_Project/Scenes/GameScene.unity (yol noktaları + 4 çapa)
    - Silinir: Assets/_Project/Scripts/Gameplay/ConveyorSlot.cs,
      Assets/_Project/Prefabs/Gameplay/ConveyorSlot.prefab (karar 9a)
    - Kapsam DIŞI: hareketli hedefe uçuş (K-13), kutu görseli (K-02), ses (K-09),
      bandın doku kayması animasyonu.

    Kabul kriterleri
    - [ ] ConveyorPath, sıralı waypoint'lerden kapalı tur kuruyor; Evaluate(distance) poz ve
          rotasyon veriyor, toplam uzunluğu ve slot sayısını tutuyor. Oyun mantığı içermiyor.
    - [ ] Tek bir bant offset değeri zamanla ilerliyor; kutular slotlara bağlı, aralarındaki
          mesafe hiç bozulmuyor, hiçbir kutu bir diğerine binmiyor.
    - [ ] Kutu EntryStart'tan çıkıp EntryPoint'teki boş slota yerleşiyor; giriş animasyonu
          sürerken eşleşme kabul etmiyor.
    - [ ] Tamamlanan kutu turu beklemeden bulunduğu yerde banttan ayrılıyor, ExitPoint'e doğru
          hareket edip orada kayboluyor ve havuza iade ediliyor.
    - [ ] Kutu üçüncü objesini aldığı anda kapasiteden düşüyor; yeni kutu onun çıkışını beklemiyor.
    - [ ] Yeni kutu yalnızca "yığında kalan obje > banttaki toplam boş yuva" iken gönderiliyor;
          level sonunda fazladan boş kutu gelmiyor.
    - [ ] Bantta aynı anda en fazla LevelData.conveyorCapacity doldurulabilir kutu bulunuyor.
    - [ ] Bant hızı, giriş gecikmesi, giriş ve çıkış süreleri GameConfig'ten okunuyor.
    - [ ] Bant hızı 0 yapıldığında her şey donuyor (Time Freeze booster'ının dayanağı).
    - [ ] Sahne boşaltılırken kutular havuza iade ediliyor, çalışan tween kalmıyor.
    - [ ] Slotlar sanal; hiçbir slot GameObject'i üretilmiyor, ConveyorSlot.cs ve prefabı silinmiş.
    - [ ] Update içinde tahsis yok, GetComponent yok, LINQ yok.

    Bağımlılık
    - K-01 (bant modelinin kök altına taşınması ve kamera çerçevesi) bitmeden test edilemez.
    - K-13 bitmeden objeler hareketli kutuya doğru şekilde uçmaz.

    Notlar
    - Sistem seçimi ve gerekçesi 06-Sabitler-ve-Kararlar.md kararlar 8, 9, 9a, 10, 10a, 11.
    - Sahne hiyerarşisi ve çapa isimleri 02-Mimari.md "Bant (Conveyor) yapısı" bölümünde.
    - Sayısal varsayılanlar 06-Sabitler-ve-Kararlar.md Gameplay tablosunda; oynanışa göre
      ayarlandıktan sonra tablo güncellenir.
    - Bant modeli 1/100 ölçekte çizilip 100x büyütülmüş; yol noktaları ConveyorRoot altında,
      ölçek 1'de durmalı.
    - Eksene hizalı kenarlardan (x'i veya z'si aynı iki waypoint) dümdüz geçilir; yalnızca köşe
      parçaları centripetal Catmull-Rom ile yumuşatılır (`_smoothingSamples`). Köşe yarıçapını
      kontrol etmek için köşe başına bir apeks noktası + kenar uçları konur. Üst üste binen nokta
      konulmaz; kod onları elese de gereksizdir.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy. Önce 01-Oyun-Ozeti.md "Bant
    kuralları" ve 02-Mimari.md "Bant (Conveyor) yapısı" bölümlerini oku.

    Görev: Dönen bant sistemini kur. Kutular şu an slot konumuna ışınlanıp sabit duruyor;
    olması gereken kapalı bir tur üzerinde sürekli dönmeleri.
    1) ConveyorPath yaz: sıralı waypoint'lerden kapalı tur kurar, Evaluate(distance) ile poz ve
       rotasyon verir, toplam uzunluğu ve slot sayısını tutar, oyun mantığı bilmez.
    2) Conveyor'ı slot karuseli olarak yeniden yaz: tek bir bant offset değeri zamanla ilerler,
       kutular sanal slotlara bağlıdır ve konumlarını slot belirler.
    3) Giriş: kutu EntryStart'tan çıkıp EntryPoint'teki boş slota yerleşir, giriş bitene kadar
       eşleşme kabul etmez.
    4) Çıkış: tamamlanan kutu turu beklemez, bulunduğu yerde banttan ayrılıp ExitPoint'e gider
       ve orada kaybolur. Kutu üçüncü objesini aldığı anda kapasiteden düşer.
    5) Gönderme kuralı: yeni kutu yalnızca yığında kalan obje sayısı banttaki toplam boş
       yuvadan fazlayken gönderilir.
    GameScene'e yol noktalarını ve EntryStart / EntryPoint / ExitPoint çapalarını koy.

    Kısıtlar:
    - Sadece bu kapsam. Hareketli hedefe uçuş (K-13), kutu görseli, ses ve bandın doku
      animasyonu bu kartta yok.
    - Yeni paket yok. Tween gerekiyorsa yalnızca giriş/çıkış geçişlerinde DOTween kullan.
    - Instantiate/Destroy yok; kutular PoolManager üzerinden gelir, iade edilirken DOKill çağrılır.
    - Slotlar sanaldır, GameObject üretme. ConveyorSlot.cs ve ConveyorSlot.prefab silinir.
    - Bant hızı, giriş gecikmesi, giriş ve çıkış süreleri GameConfig'e alan olarak eklenir ve
      06-Sabitler-ve-Kararlar.md ile eşleşir; koda sayı gömme.
    - Slot sayısı ConveyorPath'in alanıdır, LevelData'dan gelmez.
    - Update içinde tahsis, GetComponent ve LINQ yok.
    - Yalnızca GameScene.unity'ye dokun; MainScene'e dokunma.

    Teslim: değişen ve silinen dosya listesi + GameScene'de kurulan hiyerarşi ve Inspector
    bağlamaları + elle test adımları (giriş, tur, dolma, çıkış, gönderme kuralının durması,
    hız 0) + 06-Sabitler'e yazılması gereken nihai değerler + varsayımların.

---

## K-12 — [Tooling] StackBounds ölçüsünün dokümanla eşitlenmesi

    Başlık: [Tooling] StackBounds ölçüsünün dokümanla eşitlenmesi

    Amaç
    - 06-Sabitler-ve-Kararlar.md "yığın alanı iç ölçüsü 3.0 x 3.0, iç yükseklik 3.0" diyor ama
      StackBounds.prefab kök ölçeği 2 olduğu için gerçek iç hacim 6 x 6 x 6. Doküman ile proje
      çelişiyor. Bu iş bitince ikisi aynı şeyi söylüyor.

    Kapsam
    - Assets/_Project/Prefabs/Gameplay/StackBounds.prefab
    - Memory-bank/06-Sabitler-ve-Kararlar.md
    - Kapsam DIŞI: kod değişikliği, yeni prefab.

    Kabul kriterleri
    - [ ] Prefab ölçeği ile doküman değeri birbirini tutuyor (ya prefab 1'e çekilir ya doküman
          6.0 yazar; hangisi seçildiyse gerekçesi kartta).
    - [ ] Seçilen iç ölçüde 18 obje yığın alanına sığıyor ve duvarlardan taşmıyor.
    - [ ] ItemStack'in ızgara ayarları (sütun/sıra/aralık) yeni iç ölçüyle uyumlu.
    - [ ] Play Mode'da hiçbir obje zeminin altına düşmüyor ya da tavana sıkışmıyor.

    Bağımlılık
    - K-01 ile birlikte yapılırsa tek testte doğrulanır.

    Notlar
    - ItemStack ızgarası şu an MainScene'de 4 sütun x 4 sıra x 0.6 aralık; obje çapı bundan
      büyük olduğu için efektif aralık ~0.87.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: StackBounds.prefab ile dokümanı eşitle. 06-Sabitler-ve-Kararlar.md yığın alanı iç
    ölçüsünü 3.0 x 3.0 x 3.0 olarak yazıyor, ama prefab kök ölçeği 2 olduğu için gerçek iç
    hacim 6 x 6 x 6. Hangisinin doğru olacağına 18 objenin rahat sığması kriterine göre karar
    ver, diğerini ona göre düzelt.

    Kısıtlar:
    - Sadece bu kapsam. Kod değişikliği yok; ItemStack ızgara değerlerini değiştirmen
      gerekiyorsa önce gerekçesini yaz.
    - Sahne dosyalarına dokunma.
    - Kararı 06-Sabitler-ve-Kararlar.md'ye işle; iki kaynak arasında çelişki bırakma.

    Teslim: seçtiğin ölçü ve gerekçesi + değişen dosyalar + elle test adımları.

---

## K-13 — [Gameplay] Hareketli kutuya uçuş

    Başlık: [Gameplay] Hareketli kutuya uçuş

    Amaç
    - MatchResolver objeyi DOJump ile sabit bir noktaya uçuruyor. Kutular bant üzerinde
      ilerlediği için obje, kutunun uçuş başladığı andaki konumuna gidiyor ve ıskalıyor.
      Bu iş bitince obje kutunun o anki yuvasına iniyor.

    Kapsam
    - Assets/_Project/Scripts/Gameplay/MatchResolver.cs
    - Assets/_Project/Scripts/Gameplay/Box.cs (yuva konumunun dışa açılması gerekiyorsa)
    - Kapsam DIŞI: bant sisteminin kendisi (K-11), hatalı hamle geri bildirimi (K-05).

    Kabul kriterleri
    - [ ] Bant hareket halindeyken uçan obje kutunun yuvasına tam oturuyor, yanına düşmüyor.
    - [ ] Uçuş süresi GameConfig.ItemFlyDuration'dan okunuyor; kavis yüksekliği korunuyor.
    - [ ] Uçuş sürerken ayrılan yuva başka objeye verilmiyor.
    - [ ] Kutu banttan ayrılıp çıkışa giderken içindeki objeler onunla birlikte gidiyor; kutu
          havuza dönerken objeler de dönüyor, hiçbiri ortada kalmıyor.
    - [ ] Obje havuza dönerken DOKill çağrılıyor, yarım kalan uçuş bir sonraki kullanıma taşmıyor.
    - [ ] Uçan objenin collider'ı kapalı; raycast onu hedeflemiyor.

    Bağımlılık
    - K-11 bitmeden test edilemez (kutu hareket etmiyorsa hata görünmez).

    Notlar
    - Hedefin hareketli olduğu kuralı 01-Oyun-Ozeti.md "Hamle kuralları" ve
      06-Sabitler-ve-Kararlar.md karar 12.
    - DOJump sabit bir hedefe uçar; hedefi her kare güncelleyen bir çözüm ya da objeyi yuvaya
      parent edip yerel uzayda tween'leyen bir çözüm gerekir. Hangisini seçtiğini gerekçesiyle yaz.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: MatchResolver objeyi DOJump ile sabit bir noktaya uçuruyor; kutular bant üzerinde
    ilerlediği için obje kutunun eski konumuna gidip ıskalıyor. Uçuşu, kutunun o anki yuvasına
    inecek şekilde düzelt. Kutu banttan ayrılıp çıkışa giderken obje ortada kalmamalı.

    Kısıtlar:
    - Sadece bu kapsam. Bant sisteminin kendisi ve hatalı hamle geri bildirimi bu kartta yok.
    - Yeni paket yok; DOTween zaten projede.
    - Uçuş süresi ve kavis yüksekliği mevcut alanlardan okunur, yeni sayı uydurma.
    - Obje havuza dönerken DOKill çağrılmalı.
    - Sahne ve prefablara dokunma.
    - Hedefi her kare güncelleyen çözümle, objeyi yuvaya parent edip yerel uzayda tween'leyen
      çözüm arasından seç ve gerekçeni teslimde yaz.

    Teslim: değişen dosya listesi + seçtiğin çözüm ve gerekçesi + elle test adımları
    (bant dönerken art arda eşleşme, uçuş sırasında kutunun çıkması) + varsayımların.
