using UnityEngine;

namespace MatchPack.Gameplay
{
    /// <summary>
    /// Banttaki tek kutu pozisyonu. Kutuyu kendine parent etmez, yalnızca konumuna oturtur;
    /// havuz objeleri level sahnesine bağlanmaz.
    /// </summary>
    public class ConveyorSlot : MonoBehaviour
    {
        public Box CurrentBox { get; private set; }
        public bool IsEmpty => CurrentBox == null;

        /// <summary>Kutuyu bu slotun konumuna yerleştirir.</summary>
        public void PlaceBox(Box box)
        {
            if (box == null) { return; }

            CurrentBox = box;
            box.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }

        /// <summary>Slotu boşaltır. Kutuyu havuza iade etmek çağıranın işidir.</summary>
        public void Clear()
        {
            CurrentBox = null;
        }
    }
}
