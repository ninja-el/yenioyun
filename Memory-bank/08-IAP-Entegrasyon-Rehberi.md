# 08 — IAP Method Referansı

Oyun tarafı hazır; bağlanacağı nokta `IPurchaseService` arayüzü.
Bu doküman bir referanstır: **hangi method hangi sınıfta ve hangisi neyi zaten çağırıyor.**
Uygulama kararları sana ait.

---

## 1. Önerilen sınıf yapısı

Arayüz: `Assets/_Project/Scripts/Meta/IPurchaseService.cs`

Öneri: `Assets/_Project/Scripts/Meta/UnityIapPurchaseService.cs` — `IPurchaseService` uygulayan
tek sınıf. Başka bir kurgu tercih edersen tek şart, `ShopManager`'ın bu arayüzden bir örnek alması.

Arayüzün üyeleri:

| Üye | Tip |
|---|---|
| `bool IsInitialized` | property |
| `void Initialize(ShopCatalog catalog)` | method |
| `void Purchase(string productId)` | method |
| `void RestorePurchases()` | method |
| `string GetLocalizedPrice(string productId)` | method |
| `bool IsOwned(string productId)` | method |
| `event Action OnInitialized` | event |
| `event Action<PurchaseFailure> OnInitializeFailed` | event |
| `event Action<string> OnPurchaseSucceeded` | event — parametre: product id |
| `event Action<string, PurchaseFailure> OnPurchaseFailed` | event |
| `event Action<bool> OnRestoreCompleted` | event |

Devreye alma — `Assets/_Project/Scripts/Meta/ShopManager.cs`, `Start()` içinde tek satır:

```csharp
SetService(new UnityIapPurchaseService());   // eskisi: new StubPurchaseService()
```

---

## 2. Tekrar çağırmana gerek olmayanlar

`OnPurchaseSucceeded` yayınlandığı anda `ShopManager.GrantProduct` çalışır ve aşağıdakileri
**kendisi çağırır.** Bunları senin sınıfında tekrarlama.

| Method | Sınıf / Dosya | İçinde çağırdıkları |
|---|---|---|
| `GrantProduct(ShopProduct)` | `ShopManager` — `Meta/ShopManager.cs` | `EconomyManager.AddGold`, `AddLives`, `GrantInfiniteLives`, `AddBooster`, `SaveManager.Save`, `AudioManager.PlayPurchaseSucceeded`, `OnProductGranted` |
| `AddGold(int)` | `EconomyManager` — `Meta/EconomyManager.cs` | `SaveManager.Save`, `OnGoldChanged` |
| `AddLives(int)` | `EconomyManager` | `SaveManager.Save`, `OnLivesChanged` |
| `GrantInfiniteLives(float hours)` | `EconomyManager` | `SaveManager.Save`, `OnLivesChanged` |
| `AddBooster(int index, int amount)` | `EconomyManager` | `SaveManager.Save` |
| `Save()` | `SaveManager` — `Core/SaveManager.cs` | `PlayerPrefs.SetString` + `PlayerPrefs.Save` |

Yani: **gold / can / booster eklendiğinde kayıt zaten atılıyor.** `Save()` çağırma.

`PlayerData.HasRemovedAds` ve `PlayerData.OwnedProductIds` de `GrantProduct` içinde yazılıyor.

---

## 3. Kullanabileceğin methodlar

İhtiyaç duyarsan. Zorunlu değil.

### `ShopManager` — `Meta/ShopManager.cs`

| Üye | Notu |
|---|---|
| `static ShopManager Instance` | |
| `Purchase(string productId)` | Servise iletir, kayıt yapmaz |
| `RestorePurchases()` | Servise iletir |
| `GetLocalizedPrice(string productId)` | Servis boş dönerse ürünün yedek fiyat metni |
| `IsOwned(string productId)` | Servis + `PlayerData.OwnedProductIds` |
| `GrantProduct(ShopProduct)` | Ödül + kayıt. Hediye kodu / reklam ödülü de buraya bağlanır |
| `SetService(IPurchaseService)` | Mağaza katmanını değiştirir |
| `ShopCatalog Catalog` | |
| `bool IsStoreReady` | |
| `event Action<ShopProduct> OnProductGranted` | Ödül yazıldıktan sonra |
| `event Action<string, PurchaseFailure> OnPurchaseFailed` | |
| `event Action OnStoreReady` | |

### `EconomyManager` — `Meta/EconomyManager.cs`

Hepsi kendi içinde `SaveManager.Save()` çağırır.

| Üye | Yazdığı alan |
|---|---|
| `static EconomyManager Instance` | |
| `AddGold(int)` | `Gold` |
| `TrySpendGold(int)` → `bool` | `Gold` |
| `AddLives(int)` | `CurrentLives` |
| `RefillLives()` | `CurrentLives`, `LastLifeRegenTime` |
| `TrySpendLife()` → `bool` | `CurrentLives`, `LastLifeRegenTime` |
| `AddBooster(int, int)` | `BoosterCounts` |
| `GrantInfiniteLives(float hours)` | `InfiniteLivesUntilTime` |
| `ClearInfiniteLives()` | `InfiniteLivesUntilTime` |
| `int Gold` / `int Lives` / `int MaxLives` | okuma |
| `bool HasInfiniteLives` / `bool HasEnoughLives` | okuma |
| `float GetSecondsUntilNextLife()` | okuma |
| `event Action<int> OnGoldChanged` | |
| `event Action<int> OnLivesChanged` | |
| `event Action<float> OnLifeTimerTicked` | |

### `SaveManager` — `Core/SaveManager.cs`

| Üye | Notu |
|---|---|
| `static SaveManager Instance` | |
| `PlayerData Data` | Aktif veri |
| `Save()` | Ekonomi methodları zaten çağırıyor |
| `Load()` | `Awake`'te çağrılıyor |
| `bool HasSave` | |
| `ResetProgress()` | Test için |

`PlayerPrefs`'e başka hiçbir yerden yazılmaz.

### `ShopCatalog` / `ShopProduct` — `Meta/ShopCatalog.cs`, `Meta/ShopProduct.cs`

| Üye | Notu |
|---|---|
| `ShopCatalog.Products` | `ShopProduct[]` |
| `ShopCatalog.Find(string productId)` | Bulamazsa `null` |
| `ShopProduct.ProductId` | |
| `ShopProduct.ProductType` | `Consumable` / `NonConsumable` / `Subscription` |
| `ShopProduct.FallbackPriceText` | Mağazaya bağlanılamazsa gösterilen fiyat |
| `ShopProduct.GoldReward` / `LivesReward` / `InfiniteLivesHours` / `BoosterRewards` / `RemovesAds` | İçerik |

### `ShopProductButton` — `UI/ShopProductButton.cs`

Sahnedeki 16 satın alma butonunda bu bileşen var, `_productId` alanları dolu.

| Üye | Notu |
|---|---|
| `string ProductId` | |
| `Purchase()` | `ShopManager.Instance.Purchase(ProductId)` çağırır |
| `RefreshPrice()` | `OnStoreReady`'de kendi çağırıyor |

### `UIManager` — `Core/UIManager.cs`

| Üye | Notu |
|---|---|
| `ShowMarket()` / `HideMarket()` | |
| `ShowGoldPopup()` / `HideGoldPopup()` | Gold yetmediğinde açılıyor |
| `ShowHeartPopup()` / `HideHeartPopup()` | |
| `ShowBoosterPanel()` / `HideBoosterPanel()` | |
| `CloseAllPopups()` | |

### `AudioManager` — `Meta/AudioManager.cs`

| Üye | Notu |
|---|---|
| `PlayPurchaseSucceeded()` | `GrantProduct` zaten çağırıyor |
| `PlayRewardGranted()` | |
| `PlayButtonClick()` | `ShopProductButton` zaten çağırıyor |

---

## 4. Ürün kimlikleri

Asset'ler: `Assets/_Project/Data/Shop/`, katalog: `Data/Config/ShopCatalog.asset`.
Hepsi `Consumable`. İki mağazada da aynı id kullanılacak.

| Product Id | İçerik | Yedek fiyat |
|---|---|---|
| `coins_1000` | 1.000 gold | 99,99 TL |
| `coins_5000` | 5.000 gold | 399,99 TL |
| `coins_10000` | 10.000 gold | 799,99 TL |
| `coins_25000` | 25.000 gold | 1.499,99 TL |
| `coins_50000` | 50.000 gold | 2.999,99 TL |
| `coins_100000` | 100.000 gold | 4.999,99 TL |
| `box_starter` | 1 sa sınırsız can + her boosterdan 1 | 49,99 TL |
| `box_beginner` | 1.000 gold + 3 sa sınırsız can + her boosterdan 1 | 249,99 TL |
| `box_mega` | 4.000 gold + 6 sa sınırsız can + her boosterdan 5 | 499,99 TL |
| `box_golden` | 60.000 gold + 48 sa sınırsız can + her boosterdan 50 | 4.999,99 TL |

Yeni ürün: `Assets > Create > MatchPack > Shop Product` → `ShopCatalog.asset` listesine ekle.
Kod değişikliği gerekmez.

---

## 5. Notlar

- Unity IAP paketi projede **yok**. Eklemeden önce proje sahibine haber ver (CLAUDE.md kural 3).
- `ProcessPurchase` içinde ödül verme; `OnPurchaseSucceeded` yayınla, `Complete` dön.
  Sunucu doğrulaması varsa `Pending` dön, `ConfirmPendingPurchase` sonrası event'i yayınla.
- `PurchaseFailure.UserCancelled` UI tarafında sessizce yutuluyor, hata gösterilmiyor.
- İş bitince `Meta/StubPurchaseService.cs` silinir.
- Test tuşları (`GamePlay > DebugManager`): F1 kazan, F2 kaybet, F3 sınırsız can, F4 +1000 gold, F5 +1 can.
