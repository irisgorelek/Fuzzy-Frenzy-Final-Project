using UnityEngine;

public sealed class BoardModelScoreState
{
    private readonly Board _board;

    public BoardModelScoreState(Board board)
    {
        _board = board;
    }

    public void ClearGridCell(Vector2Int cell)
    {
        if (_board._grid[cell.x, cell.y] == null)
            return;

        _board._points += _board._grid[cell.x, cell.y]._points;
        _board._matchedAnimals++;
        _board._grid[cell.x, cell.y] = null;
    }

    /// <summary>Debug / cheats only: satisfy the board's primary goal (points or matches).</summary>
    public void DebugCheatFillPrimaryGoal()
    {
        if (_board._goalType == PointsOrMatches.points)
            _board._points = Mathf.Max(_board._points, _board._goalAmount);
        else if (_board._goalType == PointsOrMatches.matches)
            _board._matchedAnimals = Mathf.Max(_board._matchedAnimals, _board._goalAmount);
    }
}
