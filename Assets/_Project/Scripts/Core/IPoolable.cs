namespace MatchPack.Core
{
    /// <summary>
    /// Havuza giren ve havuzdan çıkan objelerin kendi durumunu sıfırlaması için uygulanan arayüz.
    /// PoolManager bu referansı ilk üretimde bir kez cache'ler, her Get/Release'te GetComponent çağırmaz.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Obje havuzdan alınıp aktifleştirildikten sonra çağrılır; durum burada sıfırlanır.</summary>
        void OnSpawned();

        /// <summary>Obje havuza iade edilmeden önce çağrılır; çalışan tween'ler burada DOKill ile durdurulur.</summary>
        void OnDespawned();
    }
}
