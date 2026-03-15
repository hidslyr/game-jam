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
    public bool enableDebugUI = true;
    public GameObject debugButtonsContainer;
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

    // Debug piece stack
    GameObject debugPieceStack;
    System.Collections.Generic.List<TextMeshProUGUI> debugLabels = new System.Collections.Generic.List<TextMeshProUGUI>();

    void Start()
    {
        // Display current level from Global on initialize
        int currentLevel = Global.Instance != null ? Global.Instance.CurrentLevel : 1;
        selectedLevel = currentLevel;
        if (txtSelectedLevel != null)
            txtSelectedLevel.text = $"Level {currentLevel}";

        // Generate debug piece stack
        if (enableDebugUI)
        {
            debugButtonsContainer.SetActive(true);
            GenerateDebugPieceStack();
        }
    }

    void GenerateDebugPieceStack()
    {
        var levelData = GameManager.Instance?.CurrentLevelData;
        if (levelData == null || levelData.PuzzlePieces == null) return;

        // Destroy previous stack if any
        if (debugPieceStack != null) Destroy(debugPieceStack);
        debugLabels.Clear();

        // Create container with vertical layout, top-left
        debugPieceStack = new GameObject("DebugPieceStack");
        debugPieceStack.transform.SetParent(transform, false);

        var rect = debugPieceStack.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(200f, 0f);

        var layout = debugPieceStack.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = debugPieceStack.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header
        CreateDebugLabel(debugPieceStack.transform, "── Pieces ──", Color.white, 36);

        // Each piece
        for (int i = 0; i < levelData.PuzzlePieces.Length; i++)
        {
            var entry = levelData.PuzzlePieces[i];
            string colorLetter = entry.Color.ToString()[0].ToString();
            string label = $"{i + 1}. {colorLetter}:{entry.Amount}";
            var tmp = CreateDebugLabel(debugPieceStack.transform, label, entry.Color.ToColor(), 32);
            debugLabels.Add(tmp);
        }
    }

    /// <summary>
    /// Called by PuzzleBoard when a piece is cleared or updated.
    /// </summary>
    public void UpdateDebugPiece(int pieceIndex, int remainingAmount, bool cleared)
    {
        if (pieceIndex < 0 || pieceIndex >= debugLabels.Count) return;
        var tmp = debugLabels[pieceIndex];
        if (tmp == null) return;

        if (cleared)
        {
            tmp.fontStyle = FontStyles.Strikethrough;
            var c = tmp.color;
            c.a = 0.3f;
            tmp.color = c;
            tmp.text = tmp.text.Split(':')[0] + ":0 ✓";
        }
        else
        {
            // Update remaining amount
            var parts = tmp.text.Split(':');
            tmp.text = parts[0] + ":" + remainingAmount;
        }
    }

    TextMeshProUGUI CreateDebugLabel(Transform parent, string text, Color color, int fontSize)
    {
        var go = new GameObject("DebugLabel");
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = false;
        return tmp;
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

    public void HideOnboarding()
    {
        var onboarding = transform.Find("Onboarding");
        if (onboarding != null)
            onboarding.gameObject.SetActive(false);
    }
}
