using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDKHub
{
    public enum IapProductKind { Consumable, NonConsumable, Subscription }

    [Serializable]
    public class IapProductEntry
    {
        public string id;
        public IapProductKind kind;
    }

    [Serializable]
    public class IapCatalog
    {
        public List<IapProductEntry> products = new List<IapProductEntry>();

        public static IapCatalog Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return new IapCatalog();
            try
            {
                return JsonUtility.FromJson<IapCatalog>(json) ?? new IapCatalog();
            }
            catch (ArgumentException)
            {
                return new IapCatalog();
            }
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
