using BoxGo.VFX;
using MatchPack.Core;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Kutu kapanma efektini, parçacıkları da söndükten sonra havuza iade eder. Efekt kutudan uzun
    /// yaşadığı için iadeyi kutu değil, efektin kendi prefab'ındaki bu bileşen yapar.
    /// </summary>
    [RequireComponent(typeof(BoxGoCloseVFX))]
    public class BoxCloseVfxRecycler : MonoBehaviour
    {
        private BoxGoCloseVFX _vfx;

        private void Awake()
        {
            _vfx = GetComponent<BoxGoCloseVFX>();
        }

        // Paket bitişi yalnızca Destroy ile sunuyor ve parçacık kuyruğu için event yayınlamıyor; durum bu yüzden yoklanır.
        private void Update()
        {
            if (!_vfx.IsPlaying) { PoolManager.Instance.Release(gameObject); }
        }
    }
}
