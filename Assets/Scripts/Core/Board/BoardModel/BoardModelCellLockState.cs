using System.Collections.Generic;
using UnityEngine;

public sealed class BoardModelCellLockState
{
    private readonly Board _board;

    public BoardModelCellLockState(Board board)
    {
        _board = board;
    }

    public void SetLockedCells(IEnumerable<Vector2Int> cells)
    {
        _board._lockedCells.Clear();
        if (cells == null)
            return;

        foreach (var cell in cells)
        {
            if (_board.IsCellInBounds(cell))
                _board._lockedCells.Add(cell);
        }
    }

    public void ClearLockedCells()
    {
        _board._lockedCells.Clear();
    }

    public bool IsCellLocked(Vector2Int cell)
    {
        return _board._lockedCells.Contains(cell);
    }
}
