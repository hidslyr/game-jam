using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton Main UI — level select buttons, start, force win/lose.
/// Auto-wires buttons by finding them via name in children. No Inspector dragging.
/// </summary>
public class MainUI : MonoBehaviour
{
    public static MainUI Instance { get; private set; }

    int selectedLevel = 1;

    // Cached button references (found by name)
    Button btnLevel1, btnLevel2, btnLevel3;
    Button btnStart;
    Button btnForceWin, btnForceLose;
    TextMeshProUGUI txtSelectedLevel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        FindAndWireButtons();
    }

    void Start()
    {
        // Display current level from Global on initialize
        int currentLevel = Global.Instance != null ? Global.Instance.CurrentLevel : 1;
        selectedLevel = currentLevel;
        if (txtSelectedLevel != null)
            txtSelectedLevel.text = $"Level {currentLevel}";
    }

    void FindAndWireButtons()
    {
        // Find buttons by name in children
        btnLevel1 = FindButton("BtnLevel1");
        btnLevel2 = FindButton("BtnLevel2");
        btnLevel3 = FindButton("BtnLevel3");
        btnStart = FindButton("BtnStart");
        btnForceWin = FindButton("BtnForceWin");
        btnForceLose = FindButton("BtnForceLose");

        // Find optional label
        var txtGo = FindInChildren(transform, "TxtSelectedLevel");
        if (txtGo != null) txtSelectedLevel = txtGo.GetComponent<TextMeshProUGUI>();

        // Wire listeners
        if (btnLevel1 != null) btnLevel1.onClick.AddListener(() => SelectLevel(1));
        if (btnLevel2 != null) btnLevel2.onClick.AddListener(() => SelectLevel(2));
        if (btnLevel3 != null) btnLevel3.onClick.AddListener(() => SelectLevel(3));
        if (btnStart != null) btnStart.onClick.AddListener(OnStartClicked);
        if (btnForceWin != null) btnForceWin.onClick.AddListener(OnForceWin);
        if (btnForceLose != null) btnForceLose.onClick.AddListener(OnForceLose);
    }

    void SelectLevel(int level)
    {
        Debug.Log($"[MainUI] Level {level} selected — reloading Main");
        if (Global.Instance != null)
            Global.Instance.SelectLevel(level);
        SceneManager.LoadScene("Main");
    }

    void OnStartClicked()
    {
        Debug.Log($"[MainUI] Starting level {selectedLevel}");
        if (Global.Instance != null)
            Global.Instance.SelectLevel(selectedLevel);
        if (GameManager.Instance != null)
            GameManager.Instance.StartLevel();
    }

    void OnForceWin()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerWin();
    }

    void OnForceLose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerLose();
    }

    // -- Helpers --

    Button FindButton(string name)
    {
        var go = FindInChildren(transform, name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    Transform FindInChildren(Transform parent, string name)
    {
        // Breadth-first search by name
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
        }
        foreach (Transform child in parent)
        {
            var found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
