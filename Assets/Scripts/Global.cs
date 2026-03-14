using UnityEngine;

/// <summary>
/// Persistent singleton — data store.
/// Stores current selected level index. Lives in Main scene, persists via DontDestroyOnLoad.
/// On scene reload, the duplicate is destroyed (singleton pattern).
/// </summary>
public class Global : MonoBehaviour
{
    public static Global Instance { get; private set; }

    [Header("Level Settings")]
    public int CurrentLevel = 1;
    public int MaxLevel = 3;

    public enum GameState { Boot, Playing, Win, Lose }
    public GameState CurrentState = GameState.Boot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Set the selected level (called by MainUI before starting).
    /// </summary>
    public void SelectLevel(int level)
    {
        CurrentLevel = Mathf.Clamp(level, 1, MaxLevel);
    }
}
