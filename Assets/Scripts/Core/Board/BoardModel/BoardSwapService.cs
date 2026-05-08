using UnityEngine;

public sealed class BoardModelSwapService
{
    private readonly Board _board;

    public BoardModelSwapService(Board board)
    {
        _board = board;
    }

    public bool SwapCellsRaw(Vector2Int cell1, Vector2Int cell2)
    {
        if (!(_board.IsCellInBounds(cell1) && _board.IsCellInBounds(cell2))) return false;
        if (_board.IsCellLocked(cell1) || _board.IsCellLocked(cell2)) return false;

        var a = _board._grid[cell1.x, cell1.y];
        var b = _board._grid[cell2.x, cell2.y];

        // Safety checks
        if ((a != null && !a._canSwap) || (b != null && !b._canSwap)) return false;
        if (!_board.AreCellsNeighbours(cell1, cell2)) return false;

        // Swapping the values with a tupple
        (_board._grid[cell2.x, cell2.y], _board._grid[cell1.x, cell1.y]) = (_board._grid[cell1.x, cell1.y], _board._grid[cell2.x, cell2.y]);
        return true;
    }
}
