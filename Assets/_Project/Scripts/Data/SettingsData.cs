using System;
using UnityEngine;

namespace MatchPack.Data
{
    /// <summary>
    /// Cihaza özgü oyun ayarları. PlayerData'dan ayrı bir JSON dosyasında tutulur ve Cloud Save'e gönderilmez.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        // Alan adları PlayerData'daki eski adlarla aynı kalmalı; eski kayıttan taşıma bu eşleşmeye dayanır.
        [Tooltip("Ses efektleri açık mı?")]
        [SerializeField] private bool _isSoundEnabled = true;

        [Tooltip("Müzik açık mı?")]
        [SerializeField] private bool _isMusicEnabled = true;

        [Tooltip("Titreşim (taptic) açık mı?")]
        [SerializeField] private bool _isHapticsEnabled = true;

        public bool IsSoundEnabled { get => _isSoundEnabled; set => _isSoundEnabled = value; }
        public bool IsMusicEnabled { get => _isMusicEnabled; set => _isMusicEnabled = value; }
        public bool IsHapticsEnabled { get => _isHapticsEnabled; set => _isHapticsEnabled = value; }
    }
}
