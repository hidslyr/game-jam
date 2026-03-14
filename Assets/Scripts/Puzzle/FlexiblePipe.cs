using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Two-part pipe system:
///   Part 1 (active pipe): source → through all cleared pieces (extends as pieces clear)
///   Part 2 (skeleton):    frame connecting all pieces in order (always visible)
/// Pump animation travels through the full active pipe on fill.
/// </summary>
public class FlexiblePipe : MonoBehaviour
{
    [Header("Pipe Settings")]
    public Transform SourcePoint;

    [Header("Active Pipe (Part 1)")]
    public int ActiveResolution = 16;
    public float ActiveSagAmount = 0.3f;
    public float ActiveSwayAmount = 0.05f;

    [Header("Skeleton Frame (Part 2)")]
    public int SkeletonResolution = 20;
    public float SkeletonSagAmount = 0f;
    public float SkeletonSwayAmount = 0.01f;

    [Header("Shared")]
    public float SwaySpeed = 1.5f;
    public float PipeWidth = 0.08f;
    public Material PipeMaterial;
    public Color PipeColor = Color.gray;

    [Header("Pump Animation")]
    public float PumpDuration = 0.6f;
    public float PumpBulgeMultiplier = 3f;
    public float PumpBulgeWidth = 0.15f;

    // Part 1: active pipe (extends through cleared pieces)
    LineRenderer activePipe;
    // Part 2: single skeleton spline through all connect points
    LineRenderer skeletonPipe;

    // Cached piece ConnectPoint positions (set once on init)
    List<Vector3> piecePositions = new List<Vector3>();
    int clearedCount = 0;

    // Pump state — multiple simultaneous pumps
    List<float> activePumps = new List<float>();
    public bool IsPumping => activePumps.Count > 0;

    void Awake()
    {
        activePipe = CreateLineRenderer("ActivePipe");
    }

    void Start()
    {
        BuildPipeSystem();
    }

    /// <summary>
    /// Initialize both pipe parts from PuzzleBoard pieces.
    /// </summary>
    void BuildPipeSystem()
    {
        var pieces = PuzzleBoard.Instance?.GetAllPieces();
        if (pieces == null || pieces.Count == 0 || SourcePoint == null) return;

        // Cache all piece ConnectPoint positions
        piecePositions.Clear();
        piecePositions.Add(SourcePoint.position); // Index 0 = source

        for (int i = 0; i < pieces.Count; i++)
        {
            var cp = pieces[i].transform.Find("ConnectPoint");
            piecePositions.Add(cp != null ? cp.position : pieces[i].transform.position);
        }

        // Look for EndPoint under pieces root
        var piecesRoot = pieces[0].transform.parent;
        if (piecesRoot != null)
        {
            var endPoint = piecesRoot.Find("EndPoint");
            if (endPoint != null)
                piecePositions.Add(endPoint.position);
        }

        // Single skeleton spline LineRenderer
        skeletonPipe = CreateLineRenderer("SkeletonSpline");

        clearedCount = 0;
    }

    void LateUpdate()
    {
        if (piecePositions.Count < 2) return;

        float time = Time.time * SwaySpeed;

        // ── Part 1: Active pipe (always source → first piece only) ──
        UpdateMultiSegmentPipe(activePipe, 0, 1, time, true, 0);

        // ── Part 2: Skeleton spline (all connect points as one smooth curve) ──
        UpdateSkeletonSpline(time);
    }

    /// <summary>
    /// Notify that a piece has been cleared — extends active pipe.
    /// </summary>
    public void OnPieceCleared()
    {
        clearedCount++;
    }

    /// <summary>
    /// Play pump animation through the full active pipe.
    /// </summary>
    public Coroutine PlayPump()
    {
        return StartCoroutine(PumpCoroutine());
    }

    // Wrapper so each pump has a stable reference
    class PumpEntry { public float progress; }

    IEnumerator PumpCoroutine()
    {
        var entry = new PumpEntry { progress = 0f };
        activePumps.Add(0f);
        int entryIndex = activePumps.Count - 1;

        float elapsed = 0f;
        while (elapsed < PumpDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / PumpDuration);

            // Update our entry — find it by checking we're still in range
            if (entryIndex < activePumps.Count)
                activePumps[entryIndex] = p;

            yield return null;
        }

        // Remove by setting to a sentinel, then clean up
        if (entryIndex < activePumps.Count)
            activePumps[entryIndex] = -1f;

        // Remove all completed pumps (sentinel = -1)
        activePumps.RemoveAll(v => v < 0f || v >= 1f);
    }

    // ────── Rendering ──────

    /// <summary>
    /// Render a multi-segment Bézier pipe through waypoints[startIdx..endIdx].
    /// segmentOffset: which segment index this represents in the full pump path
    /// </summary>
    void UpdateMultiSegmentPipe(LineRenderer lr, int startIdx, int endIdx, float time, bool withPump, int segmentOffset)
    {
        int segCount = endIdx - startIdx;
        if (segCount <= 0)
        {
            lr.positionCount = 0;
            return;
        }

        int totalPoints = segCount * ActiveResolution;
        lr.positionCount = totalPoints;

        var widthCurve = new AnimationCurve();
        // Total segments in pump path: active(1) + cleared skeleton segments
        int totalPumpSegments = 1 + clearedCount;

        int pointIdx = 0;
        for (int seg = 0; seg < segCount; seg++)
        {
            Vector3 segStart = piecePositions[startIdx + seg];
            Vector3 segEnd = piecePositions[startIdx + seg + 1];
            Vector3 mid = (segStart + segEnd) * 0.5f;
            mid.y -= ActiveSagAmount;

            for (int i = 0; i < ActiveResolution; i++)
            {
                float localT = i / (float)(ActiveResolution - 1);

                Vector3 point = (1 - localT) * (1 - localT) * segStart
                              + 2 * (1 - localT) * localT * mid
                              + localT * localT * segEnd;

                float globalT = (float)pointIdx / (totalPoints - 1);
                float swayStrength = Mathf.Sin(globalT * Mathf.PI) * ActiveSwayAmount;
                float noiseX = Mathf.PerlinNoise(time + pointIdx * 0.3f, 0f) - 0.5f;
                float noiseZ = Mathf.PerlinNoise(0f, time + pointIdx * 0.3f) - 0.5f;
                point.x += noiseX * swayStrength;
                point.z += noiseZ * swayStrength;

                lr.SetPosition(pointIdx, point);

                float width = PipeWidth;
                if (withPump && activePumps.Count > 0 && totalPumpSegments > 0)
                {
                    float pumpT = ((segmentOffset + seg) + localT) / totalPumpSegments;
                    float totalBulge = 0f;
                    for (int p = 0; p < activePumps.Count; p++)
                    {
                        float dist = Mathf.Abs(pumpT - activePumps[p]);
                        float bulge = Mathf.Exp(-(dist * dist) / (2f * PumpBulgeWidth * PumpBulgeWidth));
                        totalBulge = Mathf.Max(totalBulge, bulge);
                    }
                    width += PipeWidth * (PumpBulgeMultiplier - 1f) * totalBulge;
                }
                widthCurve.AddKey(globalT, width);

                pointIdx++;
            }
        }

        lr.widthCurve = widthCurve;
    }

    /// <summary>
    /// Render entire skeleton as one Catmull-Rom spline through all connect points.
    /// </summary>
    void UpdateSkeletonSpline(float time)
    {
        if (skeletonPipe == null || piecePositions.Count < 3) return;

        // Skeleton runs from piecePositions[1] to piecePositions[last]
        int segCount = piecePositions.Count - 2; // number of segments
        int pointsPerSeg = SkeletonResolution;
        int totalPoints = segCount * pointsPerSeg + 1;

        skeletonPipe.positionCount = totalPoints;
        var widthCurve = new AnimationCurve();
        int pointIdx = 0;

        for (int seg = 0; seg < segCount; seg++)
        {
            int idx = seg + 1; // piecePositions index for segment start
            Vector3 p1 = piecePositions[idx];
            Vector3 p2 = piecePositions[idx + 1];
            // Extrapolate phantom points at boundaries for smooth tangents
            Vector3 p0 = idx - 1 >= 0 ? piecePositions[idx - 1] : p1 + (p1 - p2);
            Vector3 p3 = idx + 2 < piecePositions.Count ? piecePositions[idx + 2] : p2 + (p2 - p1);

            int steps = (seg < segCount - 1) ? pointsPerSeg : pointsPerSeg + 1;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)pointsPerSeg;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);

                // Optional sway
                if (SkeletonSwayAmount > 0f)
                {
                    float swayStrength = Mathf.Sin(t * Mathf.PI) * SkeletonSwayAmount;
                    point.x += (Mathf.PerlinNoise(time + (seg * pointsPerSeg + i) * 0.3f, 0.5f) - 0.5f) * swayStrength;
                    point.z += (Mathf.PerlinNoise(0.5f, time + (seg * pointsPerSeg + i) * 0.3f) - 0.5f) * swayStrength;
                }

                skeletonPipe.SetPosition(pointIdx, point);

                // Width + pump bulge
                float width = PipeWidth;
                bool pumpThrough = activePumps.Count > 0 && idx <= clearedCount;
                if (pumpThrough)
                {
                    int totalPumpSegments = 1 + clearedCount;
                    float pumpT = (idx + t) / totalPumpSegments;
                    float totalBulge = 0f;
                    for (int p = 0; p < activePumps.Count; p++)
                    {
                        float dist = Mathf.Abs(pumpT - activePumps[p]);
                        float bulge = Mathf.Exp(-(dist * dist) / (2f * PumpBulgeWidth * PumpBulgeWidth));
                        totalBulge = Mathf.Max(totalBulge, bulge);
                    }
                    width += PipeWidth * (PumpBulgeMultiplier - 1f) * totalBulge;
                }

                float globalT = (float)pointIdx / (totalPoints - 1);
                widthCurve.AddKey(globalT, width);
                pointIdx++;
            }
        }

        skeletonPipe.widthCurve = widthCurve;
    }

    /// <summary>
    /// Catmull-Rom spline interpolation between p1 and p2, using p0 and p3 as tangent guides.
    /// </summary>
    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    // ────── Setup ──────

    LineRenderer CreateLineRenderer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 0;
        lr.startWidth = PipeWidth;
        lr.endWidth = PipeWidth;
        lr.startColor = PipeColor;
        lr.endColor = PipeColor;
        lr.numCornerVertices = 20;
        lr.numCapVertices = 20;
        lr.useWorldSpace = true;

        if (PipeMaterial != null)
            lr.material = PipeMaterial;
        else
            lr.material = new Material(Shader.Find("Sprites/Default"));

        lr.sortingOrder = -10;

        return lr;
    }
}
