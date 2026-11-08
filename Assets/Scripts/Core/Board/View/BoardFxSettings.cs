using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>Sprites and tuning for the tween-based effects. Filled from BoardView's Inspector fields.</summary>
public sealed class BoardFxSettings
{
    public BoardMatchEffects Particles;
    public BoardClearAnimation ClearAnimation;
    public Sprite MatchRingSprite;
    public Sprite SparkleSprite;
    public Color MatchFxColor;
    public int SparklesPerMatch;
    public Sprite BombRingSprite;
    public Color BombRingColor;
}
