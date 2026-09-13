# 06 — Sabitler ve Kararlar

GDD'de "N" olarak bırakılmış değerlerin proje varsayılanları burada. **Kod içine sayı gömülmez**;
bu tablodaki değerler `GameConfig` (ScriptableObject) veya `LevelData` üzerinden okunur.
Değer değişirse önce bu dosya güncellenir.

## Gameplay

| Değer | Varsayılan | Kaynak |
|---|---|---|
| Kutu kapasitesi | 3 obje | `GameConfig` |
| Bantta aynı anda duran doldurulabilir kutu | 3 | `LevelData.conveyorCapacity` |
| Level süresi | 60 sn | `LevelData.duration` |
| Level kutu hedefi | Level'e özel | `LevelData.targetBoxCount` |
| Yığındaki toplam obje | `targetBoxCount * 3` | `LevelData` (kural, ihlal edilemez) |
| Hatalı hamle süre cezası | Kapalı (3 hatada -5 sn opsiyonu `GameConfig` bayrağı) | `GameConfig` |
| Objenin kutuya uçuş süresi | 0.35 sn | `GameConfig` |
| Bant slot sayısı | 12 (modele göre ayarlanır) | `ConveyorPath` |
| Bant yolu yumuşatma örneği | 12 (1 = köşeler keskin) | `ConveyorPath` |
| Düz kenar toleransı | 0.01 birim | `ConveyorPath` |
| Bant hızı | 0.8 slot/sn (12 slotlu turda ~15 sn/tur) | `GameConfig` |
| Kutu giriş gecikmesi | 0.5 sn | `GameConfig` |
| Kutu giriş animasyonu süresi | 0.4 sn | `GameConfig` |
| Kutu çıkış animasyonu süresi | 0.35 sn | `GameConfig` |
| Yığın doğma ızgarası | 3 sütun x 3 sıra | `ItemStack` |
| Doğma anında en az obje aralığı | 0.9 birim (obje çapı büyükse o kullanılır) | `ItemStack` |
| İlk katın doğma yüksekliği | 1.2 birim | `ItemStack` |
| Doğma noktası rastgele sapması | 0.05 birim | `ItemStack` |
| Yığının oturması için zaman aşımı | 5 sn | `ItemStack` |
| Yığın alanı iç ölçüsü | 3.0 x 3.0, iç yükseklik 3.0 (zemin + 4 duvar + tavan) | `StackBounds.prefab` |
| Objenin uçuş kavisi yüksekliği | 1.5 birim | `MatchResolver` |
| Hatalı hamlede kamera sarsıntısı | 0.2 sn / 0.15 şiddet | `MatchResolver` |
| Hedef FPS | 60 | Proje ayarı |

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

## Boosterlar

| Booster | Açılış | Değer |
|---|---|---|
| Time Freeze | Lvl 4 | 5 sn donma |
| Shuffle | Lvl 6 | — |
| Auto-Match | Lvl 8 | 1 obje eşleştirir |
| Joker Box | Lvl 10 | 1 kutu |

Boosterlar envanterden tüketilir, cooldown yoktur, level içinde kullanım limiti stok kadardır.

## Alınmış kararlar

| # | Karar | Gerekçe |
|---|---|---|
| 1 | Tek `GameScene.unity` + `LevelData` ile besleme | Bölüm başına sahne çoğaltmak merge ve boyut sorunu yaratır |
| 1b | Level geçişi "önce yükle, sonra sil" (kısa süre 2 level sahnesi yüklü) | Yükleme beklemesi oyuncuya siyah ekran olarak yansımasın |
| 1c | Kamera + `AudioListener` kalıcı olarak `MainScene`'de | Geçiş anında çift kamera/çift listener oluşmasın |
| 1d | Havuz kökü `DontDestroyOnLoad`, level sahnesine parent edilmez | Sahne unload olunca havuz objeleri yok olmasın |
| 2 | `ItemType` enum değil ScriptableObject | Yeni obje eklemek kod değişikliği gerektirmesin |
| 3 | Manager'lar basit singleton | 2-3 haftalık sürede DI/servis locator ek maliyet |
| 4 | Kayıt önce local JSON, Cloud Save Faz 4'te | Cloud bağımlılığı erken fazı bloklamasın |
| 5 | Reklam/IAP çağrıları arayüz arkasında (`IAdService`, `IPurchaseService`) | SDK seçimi (Unity Ads / AppLovin) sonra netleşecek |
| 6 | Assembly Definition kullanılmıyor | Küçük projede derleme kazancı, kurulum maliyetini karşılamıyor |
| 7 | Yığın fiziksel: objeler rigidbody taşır, alttaki çekilince üsttekiler çöker | Oyunun temel hissi; sabit ızgara yerleşimi bu mekaniği vermiyor |
| 7a | Yığın sınırı görünmez duvar + zemin collider'ı (`StackBounds.prefab`) | Model gerektirmeden objelerin dağılmasını engeller |
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

## Açık sorular

Cevaplanınca yukarıdaki tablolara taşı ve buradan sil.

- [ ] Level sayısı hedefi (yayın için kaç bölüm)?
- [ ] IAP paket fiyatları ve içerikleri.
- [ ] Reklam SDK'sı: Unity Ads mi AppLovin MAX mi?
- [ ] Kamera açısı sabit mi, level'e göre değişiyor mu?
- [ ] Bant hızı level'e göre değişecek mi (`LevelData`'ya alan mı, `GameConfig`'te sabit mi)?
- [ ] Dolmamış kutu turu tamamlayıp çıkış noktasından tekrar geçtiğinde bir geri bildirim
      verilecek mi (ses/parlama), yoksa sessizce mi devam edecek?
