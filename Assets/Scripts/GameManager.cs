using DG.Tweening;
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

    VFXLibrary vfxLibrary;
    SFXLibrary sfxLibrary;
    AudioSource audioSource;
    ScreenShakeConfig shakeConfig;
    GameAnimationConfig animConfig;

    public GameAnimationConfig AnimConfig => animConfig;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AllLevels = Resources.LoadAll<LevelData>("LevelData");

        // Load libraries from Resources
        vfxLibrary = Resources.Load<VFXLibrary>("VFXLibrary");
        sfxLibrary = Resources.Load<SFXLibrary>("SFXLibrary");
        shakeConfig = Resources.Load<ScreenShakeConfig>("ScreenShakeConfig");
        animConfig = Resources.Load<GameAnimationConfig>("GameAnimationConfig");

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        InitLevel();
    }

    /// <summary>
    /// Play particle FX and SFX at the given position when a piece is cleared.
    /// </summary>
    public void PlayPieceClearEffect(Vector3 position)
    {
        // Particle FX from VFXLibrary (prefab position used as offset)
        if (vfxLibrary != null && vfxLibrary.PieceClearFX != null)
        {
            Vector3 offset = vfxLibrary.PieceClearFX.transform.position;
            Quaternion rotation = vfxLibrary.PieceClearFX.transform.rotation;
            var fx = Instantiate(vfxLibrary.PieceClearFX, position + offset, rotation);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        // SFX from SFXLibrary
        if (sfxLibrary != null && sfxLibrary.PieceClear != null && audioSource != null)
            audioSource.PlayOneShot(sfxLibrary.PieceClear);

        // Screen shake
        if (Camera.main != null && shakeConfig != null)
            Camera.main.transform.DOShakePosition(shakeConfig.Duration, shakeConfig.Intensity,
                shakeConfig.Vibrato, shakeConfig.Randomness, false, shakeConfig.FadeOut)
                .SetUpdate(true);
    }

    /// <summary>
    /// Play SFX when piece visual updates (blendshape fill).
    /// </summary>
    public void PlayPieceFillSFX()
    {
        if (sfxLibrary != null && sfxLibrary.PieceFill != null && audioSource != null)
            audioSource.PlayOneShot(sfxLibrary.PieceFill);
    }

    public void PlayBasketPickSFX()
    {
        if (sfxLibrary != null && sfxLibrary.BasketPick != null && audioSource != null)
            audioSource.PlayOneShot(sfxLibrary.BasketPick);
    }

    public void PlayBasketEmptySFX()
    {
        if (sfxLibrary != null && sfxLibrary.BasketEmpty != null && audioSource != null)
            audioSource.PlayOneShot(sfxLibrary.BasketEmpty);
    }

    public void PlayBasketDisappearEffect(Vector3 position)
    {
        if (vfxLibrary != null && vfxLibrary.BasketDisappearFX != null)
        {
            Vector3 offset = vfxLibrary.BasketDisappearFX.transform.position;
            Quaternion rotation = vfxLibrary.BasketDisappearFX.transform.rotation;
            var fx = Instantiate(vfxLibrary.BasketDisappearFX, position + offset, rotation);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }
    }

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

    public void StartLevel()
    {
        if (Global.Instance != null)
            Global.Instance.CurrentState = Global.GameState.Playing;

        Debug.Log("[GameManager] Level started — state → Playing");
    }

    public void TriggerWin()
    {
        if (Global.Instance != null)
            Global.Instance.CurrentState = Global.GameState.Win;

        Debug.Log("[GameManager] WIN triggered!");
        EndScreenUI.Instance?.Show(true);
    }

    public void TriggerLose()
    {
        if (Global.Instance != null)
            Global.Instance.CurrentState = Global.GameState.Lose;

        Debug.Log("[GameManager] LOSE triggered!");
        EndScreenUI.Instance?.Show(false);
    }

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

    public void ReloadCurrentLevel()
    {
        SceneManager.LoadScene("Main");
    }
}

