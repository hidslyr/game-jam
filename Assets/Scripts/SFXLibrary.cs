using UnityEngine;

/// <summary>
/// ScriptableObject storing audio clips for game SFX.
/// Create via Assets > Create > Game > SFX Library.
/// </summary>
[CreateAssetMenu(fileName = "SFXLibrary", menuName = "Game/SFX Library")]
public class SFXLibrary : ScriptableObject
{
    [Header("Piece SFX")]
    public AudioClip PieceClear;

    [Header("Basket SFX")]
    public AudioClip BasketPick;
    public AudioClip BasketFill;

    [Header("Game State SFX")]
    public AudioClip Win;
    public AudioClip Lose;
}
