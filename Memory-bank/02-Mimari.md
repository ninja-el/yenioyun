# 02 — Mimari

Bu yapı sabittir. Değiştirmek isteyen önce sorar.

## Sahne yapısı (asenkron / additive)

| Sahne | Rol |
|---|---|
| `BootScene` | Yalnızca açılışta çalışır. SDK/Save yüklenir, ardından `MainScene` yüklenip kendini boşaltır. |
| `MainScene` | Kalıcı sahne. Tüm Core Manager'lar ve UI Canvas'ları buradadır, **asla unload edilmez**. |
| `GameScene` | `MainScene` açıkken **additive** yüklenir. Sadece 3D içerik: bant, yığın kökü, ışık, kamera hedefleri. |

Oyun oynanırken ekranda **en az iki sahne birden aktiftir**: `MainScene` (UI + manager'lar) ve
aktif `GameScene`. Menü sahnesi hiçbir zaman kapatılmaz; menüye dönmek demek, game sahnesini
unload edip menü panellerini açmak demektir.

### Sahne yaşam döngüsü

| Geçiş | Akış |
|---|---|
| Açılış | `BootScene` → `MainScene` (Single) → `BootScene` kendini boşaltır |
| Level başlatma | `MainScene` durur, `GameScene` **additive** yüklenir → aktif sahne `GameScene` yapılır |
| Sonraki level | Yeni `GameScene` **additive yüklenir**, hazır olunca eski `GameScene` unload edilir |
| Menüye dönüş | Aktif `GameScene` unload edilir, aktif sahne `MainScene` olur |

Level geçişinde sıra **her zaman "önce yükle, sonra sil"**dir; eski sahne asla önce silinmez.
Bu yüzden geçiş anında kısa süreliğine üç sahne birden yüklü olabilir
(`MainScene` + eski `GameScene` + yeni `GameScene`). Bunun görsel/işitsel yan etkisi olmaması için:

- Yeni level'in `LevelContext` kök objesi **pasif (inactive)** olarak yüklenir; eski sahne
  unload edildikten sonra aktif edilir. Sıra: yükle → eskiyi boşalt → yeniyi aktifleştir →
  aktif sahneyi ayarla → `OnLevelStarted`.
- Sahne unload edilmeden önce eski level'in tüm havuz objeleri `PoolManager`'a iade edilir,
  DOTween tween'leri `DOKill()` ile durdurulur, event abonelikleri bırakılır.
- Geçiş boyunca input kapalıdır (`InputManager` devre dışı).

### Kurallar

- **Kamera ve `AudioListener` tektir ve `MainScene`'dedir.** Game sahnesinde kamera,
  `AudioListener` ve `EventSystem` bulunmaz; sahne yalnızca kameranın hedefleyeceği anchor
  transform'ları verir. (İki level sahnesi aynı anda yüklüyken çift kamera/çift listener
  uyarısı oluşmasın diye.)
- **Havuz objeleri level sahnesine parent edilmez.** `PoolManager` kökü `DontDestroyOnLoad`
  altındadır; aksi halde sahne unload edilince havuz objeleri de yok olur.
- Game sahnesinde manager yok; sahne referanslarını taşıyan tek bileşen `LevelContext`'tir.
  Manager'lar sahneye `LevelContext` üzerinden erişir.
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
| `Scripts/Core/SceneLoader.cs` | Additive yükleme/boşaltma, handle takibi, geçiş sırası |
| `Scripts/Core/LevelContext.cs` | Game sahnesinin referans noktası (`ConveyorRoot`, `StackRoot`, `CameraAnchor`) |
| `Scripts/UI/MainMenuScreen.cs` | Start butonunu `SceneLoader.LoadLevel` çağrısına bağlar |

Kullanım:

```csharp
SceneLoader.Instance.LoadLevel(context => { /* level hazır, LevelData uygula */ });
SceneLoader.Instance.UnloadLevel();
```

Abone olunacak event'ler: `OnSceneTransitionChanged` (input kapatma), `OnBeforeLevelUnload`
(havuz iadesi, `DOKill`), `OnLevelSceneReady`, `OnLevelSceneUnloaded`, `OnLoadProgressChanged`.

Sahneler **build index** ile yüklenir; index sabitleri `SceneIndices` içindedir. Build Settings
sırası sabittir: `0 BootScene`, `1 MainScene`, `2 GameScene`. Sıra değişirse `SceneIndices` de
değişir; sahne dosyasını yeniden adlandırmak kodu etkilemez.

## Manager'lar (MainScene'de, singleton)

| Manager | Sorumluluk |
|---|---|
| `GameManager` | Oyun state'i (Menu/Playing/Win/Lose), level başlatma-bitirme |
| `LevelManager` | Aktif `LevelData`'yı işler, kutu havuzunu ve hedef sayacını yönetir |
| `SceneLoader` | Additive yükleme/boşaltma, aktif level handle'ı, geçiş sırası |
| `InputManager` | Raycast ile dokunuş algılar, `StackItem` bildirir |
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
    Gameplay/  Conveyor, Box, StackItem, InputManager, Timer, Boosters
    Meta/      Economy, Lives, IAP, Ads, Notifications
    UI/        Paneller ve HUD
    Data/      PlayerData, LevelData, ItemType
    Editor/    LevelEditorWindow (Editor klasöründe kalır)
  Settings/
```
Namespace klasörü izler: `MatchPack.Core`, `MatchPack.Gameplay`, `MatchPack.UI`, ...
