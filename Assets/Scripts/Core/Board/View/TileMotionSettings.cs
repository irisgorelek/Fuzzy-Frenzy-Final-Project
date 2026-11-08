using Random = UnityEngine.Random;

/// <summary>Tuning for how tiles land and enter. Filled from BoardView's Inspector fields.</summary>
public sealed class TileMotionSettings
{
    public float LandingBounceHeight;
    public float LandingBounceSeconds;
    public float SpawnFallSeconds;
    public float SpawnFadeSeconds;
    public float SpawnStaggerSeconds;
    public float SpawnEntryHeight;
}
