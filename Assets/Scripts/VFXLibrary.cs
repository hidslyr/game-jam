using UnityEngine;

/// <summary>
/// ScriptableObject storing particle system prefabs for game effects.
/// Create via Assets > Create > Game > VFX Library.
/// </summary>
[CreateAssetMenu(fileName = "VFXLibrary", menuName = "Game/VFX Library")]
public class VFXLibrary : ScriptableObject
{
    [Header("Piece Effects")]
    public ParticleSystem PieceClearFX;

    [Header("Basket Effects")]
    public ParticleSystem BasketPickFX;
}
