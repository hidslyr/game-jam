using System.Collections;
using UnityEngine;

/// <summary>
/// Smooth flexible pipe connecting SourcePoint to current active piece.
/// Uses a Bézier curve with slight sag — no physics, no jitter.
/// Supports pump animation (width bulge traveling source→target).
/// </summary>
public class FlexiblePipe : MonoBehaviour
{
    [Header("Pipe Settings")]
    public Transform SourcePoint;

    [Header("Curve")]
    public int Resolution = 20;
    public float SagAmount = 0.5f;
    public float SmoothSpeed = 8f;

    [Header("Line Renderer")]
    public float PipeWidth = 0.08f;
    public Material PipeMaterial;
    public Color PipeColor = Color.gray;

    [Header("Wind Sway")]
    public float SwayAmount = 0.05f;
    public float SwaySpeed = 1.5f;

    [Header("Pump Animation")]
    public float PumpDuration = 0.6f;
    public float PumpBulgeMultiplier = 3f;
    public float PumpBulgeWidth = 0.15f; // How wide the bulge is (0-1 along curve)

    LineRenderer lineRenderer;
    Vector3 smoothedTarget;
    bool initialized;
    GameColor currentPipeColor = (GameColor)(-1);

    // Pump state
    bool isPumping;
    float pumpProgress; // 0→1 as bulge travels source→target

    public bool IsPumping => isPumping;

    void Awake()
    {
        SetupLineRenderer();
    }

    void Start()
    {
        var piece = PuzzleBoard.Instance?.GetCurrentPiece();
        if (piece != null)
        {
            var cp = piece.transform.Find("ConnectPoint");
            smoothedTarget = cp != null ? cp.position : piece.transform.position;
        }
        else if (SourcePoint != null)
            smoothedTarget = SourcePoint.position;
    }

    void LateUpdate()
    {
        if (SourcePoint == null || lineRenderer == null) return;

        var piece = PuzzleBoard.Instance?.GetCurrentPiece();
        if (piece == null) return;

        var connectPoint = piece.transform.Find("ConnectPoint");
        Vector3 targetPos = connectPoint != null ? connectPoint.position : piece.transform.position;

        UpdatePipeMaterial(piece.Color);

        if (!initialized)
        {
            smoothedTarget = targetPos;
            initialized = true;
        }
        else
        {
            smoothedTarget = Vector3.Lerp(smoothedTarget, targetPos, Time.deltaTime * SmoothSpeed);
        }

        UpdateCurve(SourcePoint.position, smoothedTarget);
    }

    /// <summary>
    /// Play the pump animation (bulge traveling source→target).
    /// Returns a Coroutine so callers can yield on it.
    /// </summary>
    public Coroutine PlayPump()
    {
        return StartCoroutine(PumpCoroutine());
    }

    IEnumerator PumpCoroutine()
    {
        isPumping = true;
        pumpProgress = 0f;

        float elapsed = 0f;
        while (elapsed < PumpDuration)
        {
            elapsed += Time.deltaTime;
            pumpProgress = Mathf.Clamp01(elapsed / PumpDuration);
            yield return null;
        }

        pumpProgress = 0f;
        isPumping = false;
    }

    void UpdateCurve(Vector3 start, Vector3 end)
    {
        lineRenderer.positionCount = Resolution;

        Vector3 mid = (start + end) * 0.5f;
        mid.y -= SagAmount;

        float time = Time.time * SwaySpeed;

        // Build width curve with pump bulge
        var widthCurve = new AnimationCurve();

        for (int i = 0; i < Resolution; i++)
        {
            float t = i / (float)(Resolution - 1);

            // Position — Bézier
            Vector3 point = (1 - t) * (1 - t) * start
                          + 2 * (1 - t) * t * mid
                          + t * t * end;

            // Wind sway
            float swayStrength = Mathf.Sin(t * Mathf.PI) * SwayAmount;
            float noiseX = Mathf.PerlinNoise(time + i * 0.3f, 0f) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(0f, time + i * 0.3f) - 0.5f;
            point.x += noiseX * swayStrength;
            point.z += noiseZ * swayStrength;

            lineRenderer.SetPosition(i, point);

            // Width — base + pump bulge
            float width = PipeWidth;
            if (isPumping)
            {
                // Gaussian bulge centered at pumpProgress
                float dist = Mathf.Abs(t - pumpProgress);
                float bulge = Mathf.Exp(-(dist * dist) / (2f * PumpBulgeWidth * PumpBulgeWidth));
                width += PipeWidth * (PumpBulgeMultiplier - 1f) * bulge;
            }

            widthCurve.AddKey(t, width);
        }

        lineRenderer.widthCurve = widthCurve;
    }

    void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = Resolution;
        lineRenderer.startWidth = PipeWidth;
        lineRenderer.endWidth = PipeWidth;
        lineRenderer.startColor = PipeColor;
        lineRenderer.endColor = PipeColor;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.useWorldSpace = true;

        if (PipeMaterial != null)
            lineRenderer.material = PipeMaterial;
        else
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    void UpdatePipeMaterial(GameColor color)
    {
        if (lineRenderer == null) return;
        if (color == currentPipeColor) return;

        currentPipeColor = color;

        string matName;
        switch (color)
        {
            case GameColor.Red:    matName = "mat_box_red"; break;
            case GameColor.Green:  matName = "mat_box_green"; break;
            case GameColor.Blue:   matName = "mat_box_blue"; break;
            case GameColor.Yellow: matName = "mat_box_yellow"; break;
            case GameColor.Purple: matName = "mat_box_purple"; break;
            default:               matName = "mat_box_base"; break;
        }

        var mat = Resources.Load<Material>($"GameJam/Art/Box/{matName}");
        if (mat != null)
            lineRenderer.material = mat;
    }
}
