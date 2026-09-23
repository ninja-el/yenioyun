using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class IAPBuyButton : MonoBehaviour
{
   [SerializeField] private IAPProductKey productKey;

   private void Awake()
   {
      GetComponent<Button>().onClick.AddListener(() => IAPManager.Instance.BuyProduct(productKey));
   }
}
