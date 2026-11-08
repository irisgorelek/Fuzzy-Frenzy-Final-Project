internal sealed class NullListener : ITurnPresentationListener
{
    public void OnMoveUsed() { }
    public void OnTilesScored(int points, int matched) { }
    public void OnTilesDestroyed(string tileId, int count) { }
    public void OnGoalProgressChanged() { }
}
