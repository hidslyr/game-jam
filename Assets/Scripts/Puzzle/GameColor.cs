using UnityEngine;

/// <summary>
/// Shared color enum used across all puzzle scripts.
/// </summary>
public enum GameColor
{
    Red,
    Green,
    Blue
}

/// <summary>
/// Extension methods for GameColor.
/// </summary>
public static class GameColorExtensions
{
    /// <summary>
    /// Returns a Unity Color for sprite tinting.
    /// </summary>
    public static Color ToColor(this GameColor gc)
    {
        switch (gc)
        {
            case GameColor.Red:   return new Color(0.91f, 0.27f, 0.37f); // #E94560
            case GameColor.Green: return new Color(0.06f, 0.61f, 0.35f); // #0F9B58
            case GameColor.Blue:  return new Color(0.26f, 0.52f, 0.96f); // #4285F4
            default:              return Color.white;
        }
    }
}
