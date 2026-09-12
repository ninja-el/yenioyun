# 07 — MVP Görev Listesi

Oyunun oynanabilir hale gelmesi için kalan işler. Her başlık bir Trello kartıdır; kartlar
`05-Gorev-Sablonu.md` biçimindedir ve sonunda AI asistanına doğrudan yapıştırılacak prompt vardır.
Kartlar birbirini beklemez; "Bağımlılık" satırındaki not yalnızca "bu bitmeden uçtan uca test
edilemez" anlamına gelir.

Sıra önerisi: F0 kartları oyunun şu an oynanamamasının sebebidir, önce onlar biter.

| # | Faz | Kart | Sahne dosyasına dokunur mu |
|---|---|---|---|
| K-01 | F0 | [Gameplay] Level sahnesi yerleşimi | Evet (`GameScene`) |
| K-02 | F0 | [Art] Kutu ve obje görselleştirmesi | Hayır |
| K-03 | F0 | [UI] Level sonu akışı ve UIManager | Evet (`MainScene`) |
| K-04 | F0 | [UI] Oyun içi HUD | Evet (`MainScene`) |
| K-05 | F0 | [Gameplay] Hatalı hamle geri bildirimi | Hayır |
| K-06 | F0 | [Gameplay] Kutu kuyruğu ile level hedefinin doğrulanması | Hayır |
| K-07 | F1 | [Meta] EconomyManager: gold ve can | Evet (`MainScene`) |
| K-08 | F1 | [Meta] Level ilerlemesi ve LevelCatalog | Hayır |
| K-09 | F1 | [Meta] AudioManager ve SFX bağlantıları | Evet (`MainScene`) |
| K-10 | F1 | [Gameplay] Kameranın CameraAnchor'a yerleşmesi | Evet (`MainScene`) |
| K-11 | F2 | [Gameplay] Kutu giriş/çıkış animasyonu | Hayır |
| K-12 | F2 | [Tooling] StackBounds ölçüsünün dokümanla eşitlenmesi | Hayır |

Aynı sahne dosyasına dokunan kartları (K-03, K-04, K-07, K-09, K-10) paralel verme; merge
çakışması çıkar. Sırayla tek kişiye/tek oturuma ver.

---

## K-01 — [Gameplay] Level sahnesi yerleşimi

    Başlık: [Gameplay] Level sahnesi yerleşimi

    Amaç
    - GameScene'de ConveyorRoot, StackRoot, StackBounds ve CameraAnchor şu an hepsi (0,0,0)'da
      duruyor; bant kutuları yığının tam içinde doğuyor ve oyun görsel olarak okunamıyor. Bu iş
      bitince yığın ile bant birbirinden ayrı, kameradan ikisi de görünür halde durur.

    Kapsam
    - Assets/_Project/Scenes/GameScene.unity (tek sahne, tek kart)
    - Assets/_Project/Prefabs/Gameplay/StackBounds.prefab (yalnızca konum gerekiyorsa)
    - Kapsam DIŞI: kod değişikliği, kamera hareketi (K-10), kutu görseli (K-02).

    Kabul kriterleri
    - [ ] StackRoot yığın alanının merkezinde, StackBounds zemininin üstünde.
    - [ ] ConveyorRoot yığının önünde ve StackBounds collider'larıyla kesişmiyor.
    - [ ] conveyorCapacity = 3 ve slot aralığı 1.5 iken üç slot da kamera görüş alanında.
    - [ ] CameraAnchor, yığın + bandı birlikte çerçeveleyen izometrik açıda.
    - [ ] Play Mode'da 18 obje yığın alanına düşüyor, hiçbiri duvarlardan taşmıyor.
    - [ ] Level root objesi sahnede hâlâ pasif (m_IsActive: 0) kayıtlı.

    Bağımlılık
    - Yok.

    Notlar
    - Slot aralığı 1.5 birim, yığın ızgarası ve doğma yüksekliği 06-Sabitler-ve-Kararlar.md'de.
    - Level root'u pasif bırakma kuralı 02-Mimari.md "Sahne yaşam döngüsü" bölümünde.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: GameScene'de level yerleşimini kur. Şu an ConveyorRoot, StackRoot, StackBounds ve
    CameraAnchor'ın hepsi (0,0,0)'da; bu yüzden bant kutuları yığın alanının içinde doğuyor.
    StackRoot'u yığın alanının merkezine, ConveyorRoot'u yığının önüne, CameraAnchor'ı ikisini
    birlikte çerçeveleyen izometrik açıya yerleştir. StackBounds'un zemini StackRoot'un altında
    kalmalı.

    Kısıtlar:
    - Sadece bu kapsam. Kod değişikliği yok, yeni prefab yok, yeni paket yok.
    - Yalnızca Assets/_Project/Scenes/GameScene.unity dosyasına dokun; MainScene'e dokunma.
    - Level root objesi sahnede pasif kayıtlı kalmalı.
    - Slot aralığı ve yığın ızgara değerleri 06-Sabitler-ve-Kararlar.md'den gelir, sayı uydurma.

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
    - Assets/_Project/Prefabs/Gameplay/ConveyorSlot.prefab
    - Assets/_Project/Data/Items/Item_Apple.asset, Item_Ball.asset, Item_Car.asset (_icon alanı)
    - Assets/_Project/Scripts/Gameplay/Box.cs (yalnızca görsel bağlama)
    - Yeni: Assets/_Project/Art/UI/Icons/ altına 3 ikon
    - Kapsam DIŞI: kutu animasyonu (K-11), ses (K-09), yeni obje tipi eklemek.

    Kabul kriterleri
    - [ ] Box.prefab'ın gövde mesh'i ve ikon gösteren bir yüzeyi var; izometrik kameradan
          ikon okunuyor.
    - [ ] Box.Setup(type) çağrıldığında ikon o tipin ItemType.Icon'una geçiyor.
    - [ ] Kutuya obje oturdukça doluluk görünüyor (3 yuvanın hangisinin dolduğu ayırt ediliyor).
    - [ ] Üç ItemType asset'inin de _icon alanı dolu.
    - [ ] Kutu havuza iade edilip tekrar alındığında eski ikon/doluluk kalıntısı kalmıyor
          (Box.OnSpawned görseli de sıfırlıyor).
    - [ ] ConveyorSlot.prefab'ın bant üstünde görünür bir zemini var.

    Bağımlılık
    - Yok. Test için K-01 yerleşimi kolaylık sağlar.

    Notlar
    - ItemType bir ScriptableObject, ikon oradan okunur; Box içinde tip başına switch/if yazma.
    - Kutu kapasitesi GameConfig.BoxCapacity'den gelir, 3 sayısı koda gömülmez.
    - Üç obje prefabı şu an aynı materyali kullanıyor, yalnızca mesh'leri farklı
      (küp / küre / silindir). Renk ayrımı da bu kartta verilebilir.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Box.prefab'ı görünür hale getir ve kutunun istediği obje tipini oyuncuya göster.
    Şu anda Box.prefab'da sadece üç boş ItemSlot transform'u var, mesh ve ikon yok; ayrıca
    Item_Apple/Item_Ball/Item_Car asset'lerinin _icon alanları boş. Kutuya gövde mesh'i ve
    ItemType.Icon'u gösteren bir yüzey ekle, Box.Setup çağrıldığında ikonu o tipe geçir,
    kutunun doluluğunu görsel olarak belli et. Üç ItemType asset'ine de ikon bağla.
    ConveyorSlot.prefab'a bant üstünde görünen bir zemin ver.

    Kısıtlar:
    - Sadece bu kapsam. Kutu animasyonu, ses ve yeni obje tipi bu kartta yok.
    - Sahne dosyalarına dokunma; iş prefab ve asset üzerinde yapılır.
    - Kutu kapasitesi GameConfig.BoxCapacity'den okunur, koda 3 yazma.
    - Box içinde ItemType'a göre switch/if yazma; görsel veriyi ItemType asset'inden al.
    - Kutu havuzdan tekrar alındığında görsel durumu sıfırlanmalı (OnSpawned).
    - Yeni paket yok.

    Teslim: değişen dosya listesi + Inspector'da yapılacak bağlamalar + elle test adımları +
    ikon üretimi için varsayımların.

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

## K-06 — [Gameplay] Kutu kuyruğu ile level hedefinin doğrulanması

    Başlık: [Gameplay] Kutu kuyruğu ile level hedefinin doğrulanması

    Amaç
    - Conveyor.BuildQueue kutu sayısını yalnızca items[].Count / BoxCapacity'den üretiyor ve
      LevelData.targetBoxCount'u hiç okumuyor; tutarsız bir LevelData sessizce yanlış sayıda
      kutu üretiyor, level ya erken bitiyor ya hiç bitmiyor. Bu iş bitince tutarsızlık
      runtime'da net bir hatayla yakalanıyor.

    Kapsam
    - Assets/_Project/Scripts/Gameplay/Conveyor.cs
    - Assets/_Project/Scripts/Data/LevelData.cs (yalnızca doğrulama yardımcıları)
    - Kapsam DIŞI: Level Editor penceresi, yeni level asset'leri.

    Kabul kriterleri
    - [ ] Level kurulurken kuyruk uzunluğu ile LevelData.TargetBoxCount karşılaştırılıyor;
          uyuşmazsa tek ve anlaşılır bir Debug.LogError basılıyor.
    - [ ] Bir obje tipinin adedi BoxCapacity'nin tam katı değilse hata basılıyor
          (artık obje kalamaz kuralı).
    - [ ] Level_001 (3 tip x 6 adet, hedef 6 kutu) hiçbir uyarı üretmeden kuruluyor.
    - [ ] Kasten bozulmuş bir LevelData ile Play'e basınca oyun sessizce yanlış davranmıyor,
          Console'da sebebi yazıyor.
    - [ ] Gameplay döngüsünde Debug.Log kalmıyor; log yalnızca kurulum anında ve hata için.

    Bağımlılık
    - Yok.

    Notlar
    - Kural: "Bir levelin obje sayısı, kutu hedefinin tam 3 katı olmak zorundadır"
      (01-Oyun-Ozeti.md). LevelData.OnValidate zaten editörde uyarıyor; bu kart runtime
      tarafını kapatıyor.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Conveyor.BuildQueue şu an LevelData.TargetBoxCount'u hiç okumuyor; kutu sayısını
    sadece items[].Count / BoxCapacity'den üretiyor, bu yüzden tutarsız bir LevelData sessizce
    yanlış sayıda kutu üretiyor. Level kurulurken kuyruk uzunluğunu TargetBoxCount ile
    karşılaştır, ayrıca her tipin adedinin BoxCapacity'nin tam katı olduğunu doğrula.
    Uyuşmazlıkta tek ve anlaşılır bir Debug.LogError bas.

    Kısıtlar:
    - Sadece bu kapsam. Level Editor penceresi ve yeni level asset'i bu kartta yok.
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

## K-11 — [Gameplay] Kutu giriş/çıkış animasyonu

    Başlık: [Gameplay] Kutu giriş/çıkış animasyonu

    Amaç
    - Kutular şu an slot konumuna anında ışınlanıyor; "taşıyıcı bant" hissi yok. Bu iş bitince
      dolan kutu banttan çıkar, yeni kutu banttan kayarak gelir.

    Kapsam
    - Assets/_Project/Scripts/Gameplay/Conveyor.cs
    - Assets/_Project/Scripts/Gameplay/ConveyorSlot.cs
    - Kapsam DIŞI: bant görseli/materyali (K-02), ses (K-09), kutu VFX.

    Kabul kriterleri
    - [ ] Yeni kutu bandın bir ucundan kayarak slotuna geliyor.
    - [ ] Dolan kutu bandın diğer ucundan çıkıp havuza iade ediliyor.
    - [ ] Kutu animasyonu sürerken o kutuya obje gönderilemiyor (yarı yolda eşleşme yok).
    - [ ] Animasyon süresi GameConfig'ten okunuyor, koda gömülü değil.
    - [ ] Kutu havuza iade edilirken DOKill çağrılıyor; sonraki kullanımda kutu yanlış yerde
          başlamıyor.
    - [ ] Sahne boşaltılırken çalışan kutu tween'i kalmıyor (OnBeforeLevelUnload).

    Bağımlılık
    - K-02 (kutu görseli) olmadan animasyon görünmez.

    Notlar
    - Kutu bir havuz objesi; animasyon transform üzerinde DOTween ile yapılır, rigidbody yok.
    - Yeni süre değeri GameConfig'e alan olarak eklenir ve 06-Sabitler'e yazılır.

**Prompt:**

    Bağlam: CLAUDE.md ve Memory-bank/ altındaki dokümanlara uy.

    Görev: Kutulara bant hissi ver. Şu an kutu slot konumuna anında ışınlanıyor. Yeni kutu
    bandın bir ucundan kayarak slotuna gelsin, dolan kutu diğer uçtan çıkıp havuza iade
    edilsin. Animasyon sürerken o kutu eşleşme kabul etmesin.

    Kısıtlar:
    - Sadece bu kapsam. Bant görseli, ses ve VFX bu kartta yok.
    - Sahne dosyalarına dokunma.
    - Animasyon süresi GameConfig'e alan olarak eklenir ve 06-Sabitler-ve-Kararlar.md'ye
      yazılır; koda sayı gömme.
    - Kutu bir havuz objesi: iade edilirken DOKill çağrılmalı, sahne boşaltılırken tween
      kalmamalı.
    - Instantiate/Destroy yok; kutu ve slot PoolManager üzerinden gelir.

    Teslim: değişen dosya listesi + GameConfig'e eklenen alan + elle test adımları +
    06-Sabitler'e eklenen değer.

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
