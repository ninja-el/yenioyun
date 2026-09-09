# 03 — Kodlama Standartları

## İsimlendirme

| Öğe | Kural | Örnek |
|---|---|---|
| Sınıf, method, property, enum | PascalCase | `ConveyorSlot`, `TryPlaceItem` |
| Private alan | `_camelCase` | `_currentBoxCount` |
| `[SerializeField]` private alan | `_camelCase` | `[SerializeField] private Transform _spawnPoint;` |
| Local değişken, parametre | camelCase | `remainingTime` |
| Sabit | PascalCase | `MaxLives` |
| Interface | `I` + PascalCase | `IPoolable` |
| Bool | `is/has/can/should` öneki | `isFilled`, `hasFreeSlot` |
| Event | `On` + geçmiş zaman | `OnBoxFilled` |
| Coroutine | `...Routine` | `FlyToBoxRoutine` |

- Kısaltma yok (`btn`, `mgr`, `tmp` değil; `button`, `manager`, `temporary`).
- Public alan yok; dışarıya `public int Gold { get; private set; }` ile açılır.

## Yorum politikası

Kural: **kod kendini anlatıyorsa yorum yazma.** Anlaşılmayan kodu yorumla değil, isimlendirmeyi
veya method'a ayırarak düzelt.

İzinli:
- Public API ve manager'ların dışa açık method'larında `/// <summary>`.
- `[SerializeField]` alanlarda Inspector için `[Tooltip("...")]` (Türkçe).
- "Neden" açıklayan tek satırlık not — yalnızca sebep koddan anlaşılmıyorsa.

Yasak:
- Kodu tekrar eden yorum (`// gold'u artır` → `AddGold(...)`).
- `#region` blokları, ASCII banner/ayraç yorumları.
- Yorum satırına alınmış ölü kod, `TODO` yığını (TODO yerine Trello kartı).
- Method içi adım adım anlatım (`// 1. önce ... // 2. sonra ...`).

```csharp
[Tooltip("Kutunun dolması için gereken obje sayısı.")]
[SerializeField] private int _capacity = 3;

/// <summary>Obje kutuya yerleşebiliyorsa true döner ve kutuyu doldurur.</summary>
public bool TryAddItem(StackItem item)
{
    if (IsFilled || item.Type != _type) { return false; }

    _items.Add(item);
    OnBoxFilled?.Invoke(this);
    return true;
}
```

## Unity kuralları

- Bileşen referansları Inspector'dan `[SerializeField]` ile bağlanır. `GetComponent` yalnızca
  `Awake`'te, `Find`/`FindObjectOfType` hiç kullanılmaz.
- `Awake`: kendi referansların. `Start`: başka sistemlere bağlanma. `OnEnable/OnDisable`:
  event abonelikleri (simetrik olmak zorunda).
- Boş `Update`, `Start` gibi metotlar silinir.
- `Update` içinde tahsis (allocation) yapılmaz; `foreach` yerine `for` tercih edilir.
- Magic number yok: değerler `[SerializeField]` alan, `LevelData` veya `GameConfig`'ten gelir.
- Log yalnızca hata/uyarı için; gameplay döngüsünde `Debug.Log` bırakılmaz.
- Yeni MonoBehaviour dosyası tek sınıf içerir, dosya adı sınıf adıyla birebir aynıdır.

## Sınıf boyutu

- Bir sınıf tek sorumluluk taşır. 200 satırı geçiyorsa bölünmesi konuşulur.
- Method 30 satırı geçmemeye çalışır; iç içe 3 seviyeden fazla blok yazılmaz.
- Gameplay mantığı UI script'ine, UI mantığı manager'a yazılmaz.

## Erken çıkış (guard clause)

Koşulları başta ele, `else` merdiveni kurma. Tek satırlık `if` bile süslü parantez kullanır.
