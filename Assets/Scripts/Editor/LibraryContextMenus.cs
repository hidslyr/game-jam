using UnityEditor;
using UnityEngine;

/// <summary>
/// Context menus for assigning ParticleSystem prefabs to VFXLibrary
/// and AudioClips to SFXLibrary ScriptableObjects.
/// Right-click a prefab with ParticleSystem → "Assign to VFX Library/..."
/// Right-click an AudioClip → "Assign to SFX Library/..."
/// </summary>
public static class LibraryContextMenus
{
    const string VFXPath = "Assets/Resources/VFXLibrary.asset";
    const string SFXPath = "Assets/Resources/SFXLibrary.asset";

    // ────── VFX Library Context Menus ──────

    [MenuItem("Assets/Assign to VFX Library/Piece Clear FX")]
    static void AssignPieceClearFX()
    {
        var lib = LoadOrCreateVFXLibrary();
        var ps = GetParticleSystemFromSelection();
        if (ps == null || lib == null) return;
        lib.PieceClearFX = ps;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VFXLibrary] PieceClearFX → {ps.name}");
    }

    [MenuItem("Assets/Assign to VFX Library/Basket Pick FX")]
    static void AssignBasketPickFX()
    {
        var lib = LoadOrCreateVFXLibrary();
        var ps = GetParticleSystemFromSelection();
        if (ps == null || lib == null) return;
        lib.BasketPickFX = ps;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VFXLibrary] BasketPickFX → {ps.name}");
    }

    [MenuItem("Assets/Assign to VFX Library/Basket Fill FX")]
    static void AssignBasketFillFX()
    {
        var lib = LoadOrCreateVFXLibrary();
        var ps = GetParticleSystemFromSelection();
        if (ps == null || lib == null) return;
        lib.BasketFillFX = ps;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VFXLibrary] BasketFillFX → {ps.name}");
    }

    // Validation — only show when a prefab with ParticleSystem is selected
    [MenuItem("Assets/Assign to VFX Library/Piece Clear FX", true)]
    [MenuItem("Assets/Assign to VFX Library/Basket Pick FX", true)]
    [MenuItem("Assets/Assign to VFX Library/Basket Fill FX", true)]
    static bool ValidateParticleSystemSelection()
    {
        return GetParticleSystemFromSelection() != null;
    }

    // ────── SFX Library Context Menus ──────

    [MenuItem("Assets/Assign to SFX Library/Piece Clear")]
    static void AssignPieceClearSFX()
    {
        var lib = LoadOrCreateSFXLibrary();
        var clip = GetAudioClipFromSelection();
        if (clip == null || lib == null) return;
        lib.PieceClear = clip;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SFXLibrary] PieceClear → {clip.name}");
    }

    [MenuItem("Assets/Assign to SFX Library/Basket Pick")]
    static void AssignBasketPickSFX()
    {
        var lib = LoadOrCreateSFXLibrary();
        var clip = GetAudioClipFromSelection();
        if (clip == null || lib == null) return;
        lib.BasketPick = clip;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SFXLibrary] BasketPick → {clip.name}");
    }

    [MenuItem("Assets/Assign to SFX Library/Basket Fill")]
    static void AssignBasketFillSFX()
    {
        var lib = LoadOrCreateSFXLibrary();
        var clip = GetAudioClipFromSelection();
        if (clip == null || lib == null) return;
        lib.BasketFill = clip;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SFXLibrary] BasketFill → {clip.name}");
    }

    [MenuItem("Assets/Assign to SFX Library/Win")]
    static void AssignWinSFX()
    {
        var lib = LoadOrCreateSFXLibrary();
        var clip = GetAudioClipFromSelection();
        if (clip == null || lib == null) return;
        lib.Win = clip;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SFXLibrary] Win → {clip.name}");
    }

    [MenuItem("Assets/Assign to SFX Library/Lose")]
    static void AssignLoseSFX()
    {
        var lib = LoadOrCreateSFXLibrary();
        var clip = GetAudioClipFromSelection();
        if (clip == null || lib == null) return;
        lib.Lose = clip;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SFXLibrary] Lose → {clip.name}");
    }

    [MenuItem("Assets/Assign to SFX Library/Piece Fill")]
    static void AssignPieceFillSFX()
    {
        var lib = LoadOrCreateSFXLibrary();
        var clip = GetAudioClipFromSelection();
        if (clip == null || lib == null) return;
        lib.PieceFill = clip;
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SFXLibrary] PieceFill → {clip.name}");
    }

    // Validation — only show when an AudioClip is selected
    [MenuItem("Assets/Assign to SFX Library/Piece Clear", true)]
    [MenuItem("Assets/Assign to SFX Library/Piece Fill", true)]
    [MenuItem("Assets/Assign to SFX Library/Basket Pick", true)]
    [MenuItem("Assets/Assign to SFX Library/Basket Fill", true)]
    [MenuItem("Assets/Assign to SFX Library/Win", true)]
    [MenuItem("Assets/Assign to SFX Library/Lose", true)]
    static bool ValidateAudioClipSelection()
    {
        return GetAudioClipFromSelection() != null;
    }

    // ────── Helpers ──────

    static ParticleSystem GetParticleSystemFromSelection()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            // Try loading as prefab
            var obj = Selection.activeObject;
            if (obj != null)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    return prefab.GetComponent<ParticleSystem>();
            }
            return null;
        }
        return go.GetComponent<ParticleSystem>();
    }

    static AudioClip GetAudioClipFromSelection()
    {
        return Selection.activeObject as AudioClip;
    }

    static VFXLibrary LoadOrCreateVFXLibrary()
    {
        var lib = AssetDatabase.LoadAssetAtPath<VFXLibrary>(VFXPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<VFXLibrary>();
            AssetDatabase.CreateAsset(lib, VFXPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[VFXLibrary] Created new VFXLibrary at " + VFXPath);
        }
        return lib;
    }

    static SFXLibrary LoadOrCreateSFXLibrary()
    {
        var lib = AssetDatabase.LoadAssetAtPath<SFXLibrary>(SFXPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<SFXLibrary>();
            AssetDatabase.CreateAsset(lib, SFXPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SFXLibrary] Created new SFXLibrary at " + SFXPath);
        }
        return lib;
    }
}
