using UnityEngine;

/// <summary>
/// ScriptableObject for per-level configuration.
/// Create instances via Assets > Create > Game > Level Data.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int LevelNumber = 1;
    public string LevelName = "Level 1";

    [Header("Gameplay Config")]
    [Tooltip("Extend with level-specific settings as needed")]
    public float TimeLimitSeconds = 120f;
}
