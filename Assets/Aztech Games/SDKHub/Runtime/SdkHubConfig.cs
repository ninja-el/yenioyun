using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDKHub
{
    public class SdkHubConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string module;
            public string key;
            public string value;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        static SdkHubConfig instance;

        public static SdkHubConfig Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<SdkHubConfig>("SdkHubConfig");
                return instance;
            }
        }

        public string Get(string module, string key, string fallback = "")
        {
            foreach (var entry in entries)
            {
                if (entry.module == module && entry.key == key) return entry.value;
            }
            return fallback;
        }

        public void Set(string module, string key, string value)
        {
            foreach (var entry in entries)
            {
                if (entry.module != module || entry.key != key) continue;
                entry.value = value;
                return;
            }
            entries.Add(new Entry { module = module, key = key, value = value });
        }
    }
}
