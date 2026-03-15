using UnityEngine;

/// <summary>
/// Gentle idle floating animation for the piece chain.
/// Attach to the chain root GameObject.
/// </summary>
public class ChainIdleFloat : MonoBehaviour
{
    [Header("Vertical Bob")]
    public float BobAmount = 0.05f;
    public float BobSpeed = 1.2f;

    [Header("Horizontal Sway")]
    public float SwayAmount = 0.03f;
    public float SwaySpeed = 0.8f;

    Vector3 startPos;
    float timeOffsetX;
    float timeOffsetY;

    void Start()
    {
        startPos = transform.localPosition;
        timeOffsetX = Random.Range(0f, Mathf.PI * 2f);
        timeOffsetY = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float y = Mathf.Sin((Time.time + timeOffsetY) * BobSpeed) * BobAmount;
        float x = Mathf.Sin((Time.time + timeOffsetX) * SwaySpeed) * SwayAmount;
        // Add slight Perlin noise for organic feel
        x += (Mathf.PerlinNoise(Time.time * 0.5f, 0f) - 0.5f) * SwayAmount * 0.5f;

        transform.localPosition = startPos + new Vector3(x, y, 0f);
    }
}
