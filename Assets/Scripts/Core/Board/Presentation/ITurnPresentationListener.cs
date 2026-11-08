/// <summary>Receives the gameplay side effects of a turn at the moment the player sees them.</summary>
public interface ITurnPresentationListener
{
    void OnMoveUsed();
    /// <summary>Tiles were scored. <paramref name="matched"/> counts toward "matches" goals.</summary>
    void OnTilesScored(int points, int matched);
    void OnTilesDestroyed(string tileId, int count);
    void OnGoalProgressChanged();
}
