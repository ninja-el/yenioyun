# Box Go UFO v6 — Referans uçuş yolu

Özgün mor/mavi üç lambalı UFO sprite'ı korunmuştur. 3D modelle değiştirilmez. Büyük yakın giriş, yukarı kavis, sağa geniş dönüş, merkeze yerleşme ve sağa çıkış referans videodan uyarlanmıştır. Kısa yatışlar vardır, takla yoktur.

Referansın gerçek 3D açı değişimleri düz sprite ile birebir üretilemez. Bu sürüm uçuş yolu, ölçek ve zamanlama uyarlamasıdır; gerçek 3D model dönüşü içermez. Üç obje toplama mekaniği korunur. MP4 Unity dışında hazırlanmış hareket önizlemesidir.

## Kurulum
Eski Assets/BoxSorterUFO2D klasörünü yedekle. Bu paketin aynı klasöründeki dosyalarla değiştir; ikinci script kopyası bırakma, .meta dosyalarını koru.
Tools > Box Sorter UFO 2.5D > Create Demo and Prefab menüsünü çalıştır. Kurucu mevcut sahneyi kaydetmeni ister ve demo sahnesine geçer; Generated demo/prefabını yeniler.
Yeni oluşturulan prefabı kullan. Play Mode'da PLAY UFO / REPLAY ile dene.

Enter Duration=1.35; Intro Scale Multiplier=2.6; Pull Duration=.45; Stagger=.24; Exit Duration=.65.
Effect Camera=oyun kamerası. Saucer Scale normal boyutu, Hover Height çekim yüksekliğini belirler. Rota kamera sağ/yukarı yönlerinde dünya birimleridir; dar/geniş kamerada konumu ölçeği kontrol et.

Flash Strength=.28. Giriş boyunca hafif kararma, objeleri çekmeye başlarken .18 saniyede açılma. Screen Space Overlay HUD karartılmaz.
API: Play(Transform[] targets, Vector3 boardCenter), tam 3 ayrı hedef gerekir. Oyun seçim, rezervasyon, puan ve havuz yönetimini yapar.
Unity derlemesi/cihaz testi burada yapılamadı. Önizleme ve ZIP bütünlüğü kontrol edildi.
