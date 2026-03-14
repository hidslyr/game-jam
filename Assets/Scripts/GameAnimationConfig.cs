using UnityEngine;

/// <summary>
/// ScriptableObject storing game animation parameters.
/// Create via Assets > Create > Game > Game Animation Config.
/// </summary>
[CreateAssetMenu(fileName = "GameAnimationConfig", menuName = "Game/Game Animation Config")]
public class GameAnimationConfig : ScriptableObject
{
    [Header("Basket Pump")]
    [Tooltip("How many times the pump animation plays per fill")]
    public int PumpCount = 1;

    [Tooltip("Delay between pump repeats")]
    public float PumpRepeatDelay = 0.1f;

    [Header("Fill Animation")]
    [Tooltip("Duration of gradual basket→piece amount transfer")]
    public float FillDuration = 0.5f;
}
