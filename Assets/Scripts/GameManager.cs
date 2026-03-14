using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton GameManager — all gameplay logic lives here (in MainScene).
/// Flow: Select level → Start → Playing → Win/Lose → EndScreen → Next/Replay
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Data (loaded from Resources)")]
    public LevelData[] AllLevels;

    public LevelData CurrentLevelData { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AllLevels = Resources.LoadAll<LevelData>("LevelData");
    }

    void Start()
    {
        InitLevel();
    }

    /// <summary>
    /// Load level data based on Global's current level. Does NOT start gameplay yet.
    /// </summary>
    void InitLevel()
    {
        int targetLevel = Global.Instance != null ? Global.Instance.CurrentLevel : 1;

        CurrentLevelData = null;
        if (AllLevels != null)
        {
            foreach (var ld in AllLevels)
            {
                if (ld.LevelNumber == targetLevel)
                {
                    CurrentLevelData = ld;
                    break;
                }
            }
        }

        Debug.Log($"[GameManager] Level {targetLevel} initialized. LevelData found: {CurrentLevelData != null}");
    }

    /// <summary>
    /// Start gameplay — sets state to Playing.
    /// Block interaction logic can be added here later.
    /// </summary>
    public void StartLevel()
    {
        if (Global.Instance != null)
            Global.Instance.CurrentState = Global.GameState.Playing;

        Debug.Log("[GameManager] Level started — state → Playing");
        // TODO: unblock player interaction here when implemented
    }

    /// <summary>
    /// Trigger win condition → show EndScreen.
    /// </summary>
    public void TriggerWin()
    {
        if (Global.Instance != null)
            Global.Instance.CurrentState = Global.GameState.Win;

        Debug.Log("[GameManager] WIN triggered!");
        EndScreenUI.Instance?.Show(true);
    }

    /// <summary>
    /// Trigger lose condition → show EndScreen.
    /// </summary>
    public void TriggerLose()
    {
        if (Global.Instance != null)
            Global.Instance.CurrentState = Global.GameState.Lose;

        Debug.Log("[GameManager] LOSE triggered!");
        EndScreenUI.Instance?.Show(false);
    }

    /// <summary>
    /// Reload Main scene with next level.
    /// </summary>
    public void LoadNextLevel()
    {
        if (Global.Instance != null)
        {
            int next = Global.Instance.CurrentLevel + 1;
            if (next > Global.Instance.MaxLevel) next = 1;
            Global.Instance.SelectLevel(next);
        }
        SceneManager.LoadScene("Main");
    }

    /// <summary>
    /// Reload Main scene with current level.
    /// </summary>
    public void ReloadCurrentLevel()
    {
        SceneManager.LoadScene("Main");
    }
}
