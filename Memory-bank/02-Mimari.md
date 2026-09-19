# 02 — Mimari

Bu yapı sabittir. Değiştirmek isteyen önce sorar.

## Sahne yapısı (asenkron / additive)

| Sahne | Rol |
|---|---|
| `BootScene` | Yalnızca açılışta çalışır. SDK/Save yüklenir, ardından `MainScene` yüklenip kendini boşaltır. |
| `MainScene` | Kalıcı sahne. Tüm Core Manager'lar ve UI Canvas'ları buradadır, **asla unload edilmez**. |
| `GameScene` | Açılışta bir kez **additive** yüklenir ve **hiç unload edilmez**. 3D içerik (bant modeli ve yolu, yığın alanı, ışık) ve oyunun **tek kamerası** buradadır. |

Oyun açıldığı andan itibaren ekranda **iki sahne birden yüklüdür**: `MainScene` (UI + manager'lar)
ve `GameScene`. İkisi de hiç kapatılmaz. Menüye dönmek demek, level içeriğini söküp menü
panellerini açmak demektir; sahne yerinde durur.

### Sahne yaşam döngüsü

| Geçiş | Akış |
|---|---|
| Açılış | `BootScene` → `MainScene` (Single) → `BootScene` kendini boşaltır → `SceneLoader` `GameScene`'i additive yükler, level içeriğini gizler |
| Level başlatma | Yükleme ekranı açılır → varsa eski level sökülür → aynı sahne yeni `LevelData` ile kurulur → sahte süre dolunca ekran kapanır |
| Sonraki level / restart | **Sahne değişmez.** Aynı akış işler; yalnızca `LevelData` değişir |
| Menüye dönüş | Level içeriği sökülür ve gizlenir, `GameScene` yüklü kalır |

Level geçişi bir **sahne geçişi değildir**. `SceneLoader.BuildLevelRoutine` sırasıyla: eski leveli
sök → sahnenin yüklü olduğunu doğrula → 3B içeriği aç → `OnLevelSceneReady`. Bu sırada:

- Level sökülmeden önce tüm havuz objeleri `PoolManager`'a iade edilir, DOTween tween'leri
  `DOKill()` ile durdurulur, event abonelikleri bırakılır (`OnBeforeLevelTeardown`).
- Geçiş boyunca input kapalıdır (`InputManager` devre dışı).
- Tüm hazırlık yükleme ekranının arkasında yaşanır; oyuncu obje dökülmesini görmez.

### Kurallar

- **Kamera ve `AudioListener` tektir ve `GameScene`'dedir.** Menüyü de o kamera render eder;
  `MainScene`'de kamera bulunmaz. Kamera `LevelContext._camera` alanına bağlanır ve
  `LevelContext.SetContentActive` onu bilerek dışarıda bırakır — level içeriği gizlenirken kamera
  kapanmaz. Sahneler arası referans tutulamadığı için `InputManager` ve `MatchResolver` kamerayı
  Inspector'dan değil `LevelContext` üzerinden okur.
- **Havuz objeleri level sahnesine parent edilmez.** `PoolManager` kökü `DontDestroyOnLoad`
  altındadır; aksi halde sahne unload edilince havuz objeleri de yok olur.
- Game sahnesinde manager yok; sahne referanslarını taşıyan tek bileşen `LevelContext`'tir.
  Manager'lar sahneye `LevelContext` üzerinden erişir.
- **Sahnedeki her görsel obje `LevelContext` kökünün altında olmak zorundadır.** Bant modeli
  dahil hiçbir şey sahne kökünde duramaz; kök pasif yüklendiği için dışarıda kalan obje geçiş
  anında iki sahnede birden görünür.
- Bölümler ayrı sahne dosyası olarak çoğaltılmaz: tek `GameScene.unity` dosyası vardır, içerik
  `LevelData`'dan gelir. Aynı sahne dosyası geçiş anında iki kez yüklü olabileceğinden
  `SceneLoader` sahneleri **isimle değil, `Scene` handle'ı ile** takip eder ve unload eder.
- Sahne yükleme/boşaltma yalnızca `SceneLoader` üzerinden yapılır; `SceneManager` çağrıları
  başka hiçbir sınıfta geçmez. `SceneLoader` aktif level handle'ını tutar ve aynı anda birden
  fazla yükleme isteğini reddeder.
- Yükleme `allowSceneActivation` ile kontrol edilir; UI tarafında yükleme göstergesi/fade
  `UIManager`'a aittir.

### Kod tarafı

| Dosya | Rol |
|---|---|
| `Scripts/Core/SceneIndices.cs` | Build Settings index sabitleri |
| `Scripts/Core/AppBootstrap.cs` | `BootScene`; ayarları uygular, `MainScene`'i Single yükler |
| `Scripts/Core/SceneLoader.cs` | `GameScene`'i bir kez yükler, level kurma/sökme sırası |
| `Scripts/Core/LevelContext.cs` | Game sahnesinin referans noktası (`ConveyorRoot`, `StackArea`, `Camera`, `CameraAnchor`) |
| `Scripts/UI/MainMenuScreen.cs` | Start butonunu `SceneLoader.LoadLevel` çağrısına bağlar |
| `Scripts/Gameplay/ConveyorPath.cs` | Bandın kapalı turu; waypoint'lerden mesafe→poz çözümü |
| `Scripts/Gameplay/Conveyor.cs` | Slot karuseli, kutu gönderme kuralı, giriş/çıkış |

Kullanım:

```csharp
GameManager.Instance.StartLevel(level);   // yükleme ekranı + kurulum
GameManager.Instance.StartNextLevel();    // katalogdaki sıradaki bölüm
GameManager.Instance.RetryLevel();        // aynı bölümü baştan kur
GameManager.Instance.ReturnToMenu();      // leveli sök, sahne yüklü kalsın
```

Abone olunacak event'ler: `OnSceneTransitionChanged` (input kapatma), `OnBeforeLevelTeardown`
(havuz iadesi, `DOKill`), `OnLevelSceneReady`, `OnLevelTornDown`.

Sahneler **build index** ile yüklenir; index sabitleri `SceneIndices` içindedir. Build Settings
sırası sabittir: `0 BootScene`, `1 MainScene`, `2 GameScene`. Sıra değişirse `SceneIndices` de
değişir; sahne dosyasını yeniden adlandırmak kodu etkilemez.

## Manager'lar (MainScene'de, singleton)

| Manager | Sorumluluk |
|---|---|
| `GameManager` | Oyun state'i (Menu/Playing/Win/Lose), level başlatma-bitirme |
| `LevelManager` | Aktif `LevelData`'yı işler, dolan kutuyu sayar, hedefe ulaşınca leveli kazandırır |
| `SceneLoader` | `GameScene`'i bir kez yükler, level içeriğini kurar/söker, geçiş sırası |
| `InputManager` | Kutu prob (BoxCast) ile dokunuş algılar, `StackItem` bildirir |
| `PoolManager` | Tüm runtime instance'ları |
| `SaveManager` | `PlayerData` yükle/kaydet |
| `EconomyManager` | Gold, can, booster envanteri |
| `AudioManager` | SFX / BGM |
| `UIManager` | Panel aç/kapa |

Singleton kalıbı tek tip: `public static X Instance` + `Awake` içinde atama ve
`DontDestroyOnLoad`. Servis locator, DI framework, `FindObjectOfType` kullanılmaz.

## Event modeli (Observer)

- Veriyi kim tutuyorsa event'i o yayınlar: `public event Action<int> OnGoldChanged;`
- Dinleyici `OnEnable`'da abone olur, `OnDisable`'da mutlaka aboneliği bırakır.
- UI hiçbir zaman manager'ın state'ini `Update` içinde yoklamaz.
- Event isimleri `OnXChanged` / `OnXCompleted` biçiminde, geçmiş zaman anlatır.

Gameplay için ana event'ler: `OnLevelStarted`, `OnLevelCompleted`, `OnLevelFailed`,
`OnTimerTicked`, `OnBoxFilled`, `OnBoxSpawned`, `OnItemMatched`, `OnItemMissed`,
`OnGoldChanged`, `OnLivesChanged`, `OnBoosterUsed`.

## Bant (Conveyor) yapısı

Bant kapalı devre bir turdur ve kutular üzerinde sürekli hareket eder. Kural metni
`01-Oyun-Ozeti.md` "Bant kuralları" bölümündedir; burada yalnızca yapı anlatılır.

### Sahne tarafı

`GameScene` içinde, `LevelContext.ConveyorRoot` altında:

```
ConveyorRoot
  BeltModel            bant modelinin kökü (mevcut "world" objesi buraya taşınır)
  Path
    Waypoint_00 ... Waypoint_NN   turu çizen sıralı noktalar; köşe başına bir apeks +
                                  kenar uçları yeter, arasını eğri yumuşatır. Üst üste
                                  binen nokta konmaz
  EntryStart           kutunun bant dışında doğduğu nokta
  EntryPoint           kutunun banta katıldığı, tur üzerindeki nokta
  ExitPoint            tamamlanan kutunun gidip kaybolduğu, tur dışındaki nokta
```

`EntryPoint` turun üzerinde durur; `ConveyorPath` onun yol üzerindeki en yakın mesafe değerini
bir kez hesaplar ve boş bir slot o mesafeyi geçtiğinde kutu gönderilir. `EntryStart` turun
dışındadır ve kutunun hangi yönden geldiğini belirler. `ExitPoint` de tur dışındadır; tamamlanan
kutu bulunduğu yerden doğrudan oraya gider.

### Kod tarafı

| Sınıf | Sorumluluk |
|---|---|
| `ConveyorPath` | Waypoint'lerden kapalı tur kurar. Eksene hizalı kenarlardan dümdüz geçer, yalnızca köşe parçalarını centripetal Catmull-Rom ile yumuşatır ve örneklere böler. `Evaluate(distance)` ile poz ve rotasyon verir, toplam uzunluğu ve slot sayısını tutar. Oyun mantığı bilmez. |
| `Conveyor` | Tek bir `_beltOffset` değerini zamanla ilerletir. Slot doluluğunu, kutu gönderme kuralını, giriş ve çıkış geçişlerini yönetir. Kapasiteye yalnızca doldurulabilir kutuları sayar. |
| `Box` | Yalnızca kendi tipini, yuvalarını ve prefab'tan gelen sabit duruşunu bilir. Konumunu `Conveyor` verir; kutu kendi hareketini hesaplamaz ve tur boyunca dönmez. |

- `Conveyor` kutu prefab'ını tek tek değil **liste** olarak tutar ve kutuları sırayla bu
  listeden alır. `PoolManager` prefab başına ayrı havuz açtığı için ek iş gerekmez.
- Slotlar **sanaldır**: GameObject değildir, havuzdan alınmaz. Slot `i`'nin yol üzerindeki
  mesafesi `(_beltOffset + i / slotCount) * pathLength`'tir.
- Kutular tur boyunca dönmez: `Conveyor` yalnızca `transform.position` yazar, rotasyon prefab'tan
  gelir ve hiç değişmez. `ConveyorPath.Evaluate` yine de yolun teğet rotasyonunu döner; onu şu an
  sadece gizmo çizimi kullanır.
- Bandın görsel dönüşü (doku kayması, tahrik silindirleri) `Conveyor`'ın hız değerinden beslenir.
- Kutu uçuş hedefi hareketlidir; `MatchResolver` sabit bir noktaya değil, kutunun o anki yuvasına
  uçurur.

## Dokunuş probu

`InputManager` ekran noktasını kameradan çıkan bir prob ile objeye çevirir. Varsayılan prob ince
ışın değil, ışına dik duran bir **kutudur** (`Physics.BoxCast`): parmak objenin kenarından biraz
kaçtığında da seçim oluşur. Kutunun kenar uzunluğu Inspector'dan ayarlanır; `_isBoxCastEnabled`
kapatılınca eski ince ışın (`Physics.Raycast`) davranışına dönülür. Prob kutusu ve isabet
noktası `DebugManager` gizmo'larında çizilir.

## Yığın alanı (StackArea)

Objelerin doğduğu ve içinde kaldığı kutu alan **tek kaynaktır**: sahnede `LevelContext` altındaki
`StackArea` objesinin merkez + ölçü alanları hem doğma noktalarını hem de objeleri içeride tutan
görünmez duvarları belirler. Duvarlar (zemin + 4 duvar + tavan) `Awake`'te alanın ölçüsünden
`BoxCollider` olarak üretilir; alanın kendi görseli yoktur, Scene view'da yalnızca gizmo olarak
çizilir. Alanın transform ölçeği 1 kalmalıdır; boyut `_size` ile verilir.

`ItemStack` objeleri karıştırıp alanın içinde **birbirine değmeyen** noktalara doğurur; nokta
araması objenin kendi yarıçapını kullandığı için obje ölçeği değişince yerleşim kendiliğinden
uyar. Objelerin tamamı bir anda sığmazsa kalanlar kuyrukta bekler ve yerleşim taraması belirli
aralıklarla tekrarlanır; yığından obje eksildikçe açılan boşluklara doğarlar. `OnStackSettled`
yalnızca **ilk dolum** durulunca bir kez yayınlanır, sonraki doğumlar süreyi ve input'u etkilemez.

## Yükleme ekranı

Level kurulurken `LoadingScreen` açılır. Ekran **ayarlanabilir bir sahte bekleme süresi** işletir
ve yazının sonundaki noktaları (`Yükleniyor` → `Yükleniyor...`) döngüyle artırıp sıfırlar.

Kapanma koşulu **iki şartın birden** sağlanmasıdır: hazırlık bitmiş **ve** sahte süre dolmuş
olmalı. Hazırlık erken biterse ekran süre dolana kadar açık kalır; hazırlık uzun sürerse süre
dolsa da ekran kapanmaz. Sıra `GameManager.BuildLevelRoutine` içinde yürür:

```
Loading durumuna geç → ekranı aç → SceneLoader.BuildLevelRoutine → sahte sürenin kalanını bekle
→ ekranı kapat → Playing durumuna geç → OnLevelStarted
```

Sayacın yükleme ekranının arkasında işlemeye başlamaması için `LevelManager` sayacı yalnızca
yığın oturduğu**nda ve** level gerçekten başladığında başlatır.

## Level sonu ve ilerleme

- Kazanma şartı: dolan kutu sayısı `LevelData.TargetBoxCount` değerine ulaşır
  (`LevelManager.HandleBoxFilled`). Bandın kutuları hedefe ulaşılmadan biterse level yine kapanır.
- Kaybetme şartı: `LevelTimer` sıfıra iner.
- Panelleri `UIManager` açar; butonları `LevelResultScreen` işletir.
- Bölüm sırası `LevelCatalog` asset'indedir; `PlayerData.CurrentLevel` bu listedeki 1'den başlayan
  sıra numarasıdır ve kazanılınca bir arttırılıp kaydedilir.
- Restart ve sonraki level butonları basılır basılmaz iş yapmaz: `LevelResultScreen` üzerinde
  ayarlanan gecikme kadar beklenir (efektler bu aralıkta oynatılır), sonra level kurulur.

## Veri

- `PlayerData` — düz `[Serializable]` C# sınıfı, JSON'a serialize edilir.
  Alanlar: `currentLevel`, `gold`, `currentLives`, `lastLifeRegenTime`, `boosterCounts`,
  `hasRemovedAds`.
- Kayıt: Local `PlayerPrefs` (tek JSON string) → sonraki fazda Unity Cloud Save.
- `LevelData` (ScriptableObject): `levelIndex`, `duration`, `targetBoxCount`,
  `conveyorCapacity`, `items[]` (`ItemType` + adet).
- Level'ler `Assets/_Project/Data/Levels/` altında, In-House Level Editor penceresi ile üretilir.
- `ItemType` bir `enum` değil, `ScriptableObject`'tir (yeni obje eklemek kod değişikliği
  gerektirmesin diye). Eşleşme referans karşılaştırmasıyla yapılır.

## Zorunlu teknik kurallar

- **Pooling:** Obje, kutu, VFX ve uçuş efektleri `PoolManager.Get(prefab)` /
  `PoolManager.Release(instance)`. Gameplay içinde `Instantiate`/`Destroy` yasak.
- `Update` içinde `GetComponent`, `Find`, LINQ, string birleştirme yok.
- Tween'ler DOTween ile; obje yok edilirken/pool'a dönerken `DOKill()` çağrılır.
- Hedef: 60 FPS, portre, referans çözünürlük 1080x1920 (Canvas Scaler match 0.5).

## İzinli paketler

Mevcut: URP, Input System, Timeline, uGUI, TextMeshPro, AI Navigation.
Planlı: DOTween, Unity IAP, Unity Ads (veya AppLovin MAX), Facebook SDK,
Unity Mobile Notifications, Cloud Save, NiceVibrations/Haptics, Game Center & Play Games.
Bu listede olmayan hiçbir paket sorulmadan eklenmez.

## Klasör yapısı

```
Assets/_Project/
  Art/       Models, Materials, Textures, VFX, UI
  Audio/     SFX, Music
  Data/      Levels, Items, Config (ScriptableObject)
  Prefabs/   Gameplay, UI, VFX
  Scenes/    BootScene, MainScene, GameScene
  Scripts/
    Core/      GameManager, SceneLoader, PoolManager, SaveManager
    Gameplay/  Conveyor, ConveyorPath, Box, StackItem, StackArea, InputManager, Timer, Boosters
    Meta/      Economy, Lives, IAP, Ads, Notifications
    UI/        Paneller ve HUD
    Localization/  Loc, LocalizationTableSo, LocalizedText (+ Editor/ araçları)
    Data/      PlayerData, LevelData, ItemType
    Editor/    LevelEditorWindow (Editor klasöründe kalır)
  Settings/
```
Namespace klasörü izler: `MatchPack.Core`, `MatchPack.Gameplay`, `MatchPack.UI`, ...
