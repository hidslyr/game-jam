using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton EndScreen UI — shown on win/lose inside MainScene.
/// Empty for now per spec. Auto-wires by name, no Inspector dragging.
/// </summary>
public class EndScreenUI : MonoBehaviour
{
    public static EndScreenUI Instance { get; private set; }

    // Root panel — starts hidden
    GameObject panelRoot;
    TextMeshProUGUI txtResult;
    Button btnNextLevel, btnReplay;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        FindAndWire();
        Hide();
    }

    void FindAndWire()
    {
        // The EndScreen panel itself — find by name
        var panelTransform = FindInChildren(transform, "PanelEndScreen");
        panelRoot = panelTransform != null ? panelTransform.gameObject : gameObject;

        // Optional elements (empty for now, but ready for future)
        var txtGo = FindInChildren(transform, "TxtResult");
        if (txtGo != null) txtResult = txtGo.GetComponent<TextMeshProUGUI>();

        var btnNext = FindButton("BtnNextLevel");
        if (btnNext != null)
        {
            btnNextLevel = btnNext;
            btnNextLevel.onClick.AddListener(OnNextLevel);
        }

        var btnRep = FindButton("BtnReplay");
        if (btnRep != null)
        {
            btnReplay = btnRep;
            btnReplay.onClick.AddListener(OnReplay);
        }
    }

    /// <summary>
    /// Show the end screen. isWin = true for congratulations, false for game over.
    /// </summary>
    public void Show(bool isWin)
    {
        panelRoot.SetActive(true);

        if (txtResult != null)
            txtResult.text = isWin ? "Congratulations!" : "Game Over";

        // Hide MainUI when showing EndScreen
        if (MainUI.Instance != null)
            MainUI.Instance.gameObject.SetActive(false);

        Debug.Log($"[EndScreenUI] Showing: {(isWin ? "WIN" : "LOSE")}");
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    void OnNextLevel()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadNextLevel();
    }

    void OnReplay()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReloadCurrentLevel();
    }

    // -- Helpers --

    Button FindButton(string name)
    {
        var go = FindInChildren(transform, name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    Transform FindInChildren(Transform parent, string name)
    {
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
