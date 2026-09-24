# Orijinal UFO – 2.5D animasyon

Bu paket gönderdiğiniz UFO JPEG'ini değiştirmeden kullanır. Yeniden çizilmiş / modellenmiş UFO içermez. UFO kameraya dönük bir görsel yüzey olarak çalışır; oyun objeleri üç boyutlu olarak hareket edebilir.

## Deneme
1. `Assets/BoxSorterUFO2D` klasörünü projenizin Assets klasörüne kopyalayın.
2. Unity derlemesi bitince `Tools > Box Sorter UFO 2.5D > Create Demo and Prefab` seçin.
3. Play'e basın ve `PLAY UFO / REPLAY` düğmesini kullanın.
4. `Generated/UFOOriginalSprite.prefab` kendi sahnenizde kullanılabilir.

Eski BoxSorterUFO paketiyle farklı klasör ve namespace kullanır. Eski booster'ı sahnenizde kapatın. Menü, üretilen demo/prefabı tekrar oluşturur; önce özel değişikliklerinizi yedekleyin.

## Işık sırası
Başlangıçta üç lamba sönük görünür. Birinci obje tamamen çekilince sol, ikinci çekilince orta, üçüncü çekilince sağ lamba yanar. Yanan lambalar açık kalır. Üçüncü ışık ve kutu tamamlanma olayı aynı anda gerçekleşir. Tekrar oynatıldığında ışıklar sıfırlanır.

## Oyun entegrasyonu
- `UFOBooster` bileşenine referans alın (`BoxSorterUFO2D` namespace).
- Aynı kutuya uygun üç objeyi oyun kodunuzda seçip rezerve edin.
- `ufo.Play(new[]{ obje1, obje2, obje3 }, boardCenter)` çağırın; parametreler üç Transform'dur. false dönerse başlatılmadı.
- `onThreeCollected` olayına mevcut kutuyu 3/3 yapan ve kapatan metodunuzu bağlayın. Bu olay tek sefer çalışır; UFO kutuya gidip bırakmaz.
- Objeler toplanınca pasif olur. Oyun havuzuna dönüş, skor, kutu kategorisi, booster harcama ve rezervasyonlar oyun kodunun sorumluluğundadır.
- `Cancel()` tamamlanmamış toplama işlemini geri alır. `Finished(bool)` başarı/iptal bilgisini verir.
- Kök nesnenin ölçeğini 1, rotasyonunu 0 tutun. `saucerScale` ve `hoverHeight` ayarlanabilir.
- Y ekseni yukarı, XZ düzlemi oyun zemini varsayılır. MainCamera etiketli kamera gereklidir; UFO yüzeyi her kare kameraya döner. Bu yüzden farklı açılardan üç boyutlu gövde görünümü sunmaz.

## Görüntü nasıl korunuyor?
`Textures/UFO_Original.jpeg`, yüklediğiniz dosyayla byte byte aynıdır. Gövde, kubbe, halkalar ve göz yuvaları yeniden çizilmedi. OriginalSprite.shader yalnızca ayrı siluet maskesiyle beyaz arka planı görüntüleme anında saydamlaştırır ve belirlenmiş üç lamba alanını karartır. Yanık durumda lambalar orijinal görseldeki renk ve parlamasına döner. Kaynak dosya üzerinde değişiklik yapılmaz. Beyaz kubbe yansımaları ve halkalardaki parlaklıklar siluet maskesinin içinde korunur. JPEG kenarlarında çok hafif açık kenar kalabilir.

## Sınırlar
Unity editörü bu ortamda yok: paket Unity'de derlenip test edilmedi. Önizleme Unity kaydı değildir; aynı görüntüleme/maskeyle üretilen 2.5D hareket önizlemesidir. Gerçek oyun sahnesinin ekran kaydı değildir; elma/kutu temsilleri basitleştirilmiştir. Kutu doldurma koduna bağlantı hâlâ gereklidir.

Built-in/URP için basit bağımlılıksız shader'lar içerir, kesin sürüm uyumluluğu test edilmedi. HDRP hedeflenmedi. Ses yoktur. `Source/render_preview.py`, NumPy, SciPy ve Pillow ile önizleme videosunun karelerini üretir; source JPEG'i değiştirmez.


## Unity 6000.3.9 – klasör yolu düzeltmesi
Kurulum artık `Assets/BoxSorterUFO2D` konumunu zorunlu tutmaz. `BoxSorterUFO2D.UFOBooster` scriptinin konumundan paket kökünü AssetDatabase aracılığıyla bulur; Textures ve Generated yollarını bu köke göre çözer. Örneğin `Assets/Atakan/UFO_OriginalSprite/Assets/BoxSorterUFO2D` desteklenir. Klasörün iç yapısını koruyun.

Mevcut kurulumda yalnızca `Editor/UFOSetup.cs` dosyasını yenisiyle değiştirmeniz bu yol hatasını düzeltir. İkinci paket kopyası eklemeyin: aynı sınıflar iki yerde bulunursa derleme hatası oluşur. Ardından derleme bitince Tools > Box Sorter UFO 2.5D > Create Demo and Prefab menüsünü yeniden çalıştırın.

Eksik dosya veya shader hataları artık yeni sahne açılmadan kontrol edilir; hata mesajında aranan tam yol gösterilir. Unity 6000.3 API belgeleri kontrol edildi. Bu ortamda Unity 6000.3.9 editörü olmadığı için editör içinde çalıştırma/derleme doğrulaması yapılamadı. Render pipeline seçimi Unity sürümünden bağımsızdır; görsel sonuç Built-in/URP projenizde denenmelidir.


## ApplyLights NullReference düzeltmesi
MaterialPropertyBlock artık alan başlatıcısında oluşturulmaz. Awake içinde ve her ApplyLights öncesinde EnsureProperties ile oluşturulur/kontrol edilir. Böylece boş referans kullanımına karşı koruma sağlanır. Play, objelerin collider/fizik durumunu değiştirmeden önce renderer ve lamba materyalini doğrular. Geçerli renderer atanmamışsa model altından OriginalSprite materyaline sahip renderer bulunur; bulunamazsa açıklayıcı hata ile false döner.

Güncelleme: Play modunu durdurun, mevcut `BoxSorterUFO2D/UFOBooster.cs` dosyasını yenisiyle değiştirin, Unity derlemesi bitince tekrar Play'e basın. Bu düzeltme için sahneyi yeniden oluşturmak gerekmez. Editör içinde yürütme doğrulaması bu ortamda yapılamadı.
