# 04 — Varlık İsimlendirme ve Git Kuralları

## Asset isimlendirme

| Tür | Kalıp | Örnek |
|---|---|---|
| Prefab (gameplay) | `PascalCase` | `Box`, `ConveyorSlot`, `StackItem_Apple` |
| Prefab (UI panel) | `Panel_` | `Panel_Win`, `Panel_Shop` |
| Sahne | `PascalCase` | `BootScene`, `MainScene`, `GameScene` |
| Model | `SM_` | `SM_Conveyor`, `SM_Box` |
| Materyal | `M_` | `M_Box_Default` |
| Texture | `T_` + suffix | `T_Box_BaseColor` |
| UI sprite | `UI_` | `UI_Button_Play`, `UI_Icon_Freeze` |
| VFX | `VFX_` | `VFX_BoxComplete` |
| Ses | `SFX_` / `BGM_` | `SFX_ItemFly`, `BGM_Menu` |
| ScriptableObject asset | `Tip_Ad` | `Item_Apple`, `Level_012`, `GameConfig` |
| Animasyon | `Anim_` | `Anim_Box_Close` |

- Türkçe karakter, boşluk ve sürüm eki kullanılmaz (`kutu final2 (1).fbx` yasak).
- Her asset kendi tür klasöründe durur; "Yeni Klasör", "Test", "Deneme" klasörü commit edilmez.

## Prefab kullanımı

- Gameplay ve UI objeleri **her zaman prefab**'tır; sahneye doğrudan hiyerarşi kurulmaz.
- Prefab varyantı, aynı objenin görsel türevleri için kullanılır (ör. farklı obje modelleri).
- Prefab üzerindeki override'lar sahnede bırakılmaz; ya prefab'a apply edilir ya geri alınır.

## Git

- `.meta` dosyaları **her zaman** commit edilir; asset silinirken `.meta`'sı da silinir.
- `Library/`, `Temp/`, `Logs/`, `*.csproj`, `*.sln` commit edilmez (`.gitignore` bunu kapsar;
  yanlışlıkla eklenmişse temizlenir).
- Aynı sahne veya prefab üzerinde aynı anda iki kişi çalışmaz. Trello kartı sahneye dokunuyorsa
  kartta belirtilir ve iş kısa tutulur.
- Commit mesajı İngilizce, kısa ve emir kipi: `Add conveyor slot pooling`.
- Bir commit tek bir kartın işini içerir; format/yeniden isimlendirme commit'leri ayrı atılır.
- Branch: `feature/<kart-no>-kisa-ad`, `fix/<kart-no>-kisa-ad`.

## Değişiklik yaparken

- Var olan bir sistemi yeniden yazmak yerine genişlet. Yeniden yazma gerekiyorsa önce sor.
- Başkasının kartına ait dosyayı düzenleme; gerekiyorsa kartta not düş.
