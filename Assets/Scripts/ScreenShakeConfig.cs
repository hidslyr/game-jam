using UnityEngine;

/// <summary>
/// ScriptableObject storing screen shake parameters.
/// Create via Assets > Create > Game > Screen Shake Config.
/// </summary>
[CreateAssetMenu(fileName = "ScreenShakeConfig", menuName = "Game/Screen Shake Config")]
public class ScreenShakeConfig : ScriptableObject
{
    public float Intensity = 0.15f;
    public float Duration = 0.3f;
    public int Vibrato = 10;
    public float Randomness = 90f;
    public bool FadeOut = true;
}
