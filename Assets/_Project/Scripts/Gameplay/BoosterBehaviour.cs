using MatchPack.Data;
using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Bir booster'ın etkisini uygulayan bileşenin ortak tabanı. Stok, kilit ve bedel kontrolü
    /// <see cref="BoosterManager"/>'a aittir; buradan türeyen sınıf yalnızca etkiyi uygular.
    /// </summary>
    public abstract class BoosterBehaviour : MonoBehaviour
    {
        /// <summary>Bu bileşenin uyguladığı booster.</summary>
        public abstract BoosterType Type { get; }

        /// <summary>
        /// Etkiyi uygular. Etki uygulanamadıysa (uygun hedef yok, booster zaten çalışıyor) false
        /// döner ve booster envanterden düşülmez.
        /// </summary>
        public abstract bool TryActivate(BoosterData data);

        /// <summary>Level sökülürken çağrılır. Süren etkiyi iptal edip sahneyi eski haline bırakır.</summary>
        public virtual void Cancel() { }
    }
}
