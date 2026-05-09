using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardModelShuffler
{
    private readonly Board _board;

    public BoardModelShuffler(Board board)
    {
        _board = board;
    }

    public void ShuffleSwappablePieces()
    {
        var cells = new List<Vector2Int>();
        var pieces = new List<Animal>();

        for (int x = 0; x < _board._width; x++)
        {
            for (int y = 0; y < _board._height; y++)
            {
                var a = _board._grid[x, y];
                if (a == null) continue;
                if (!a._canSwap) continue;   // keep blockers / bones in place

                cells.Add(new Vector2Int(x, y));
                pieces.Add(a);
            }
        }

        for (int i = pieces.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pieces[i], pieces[j]) = (pieces[j], pieces[i]);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            _board._grid[c.x, c.y] = pieces[i];
        }
    }

    public bool ShuffleUntilPlayable(BoardHintFinder hintFinder, int maxAttempts = 100)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            ShuffleSwappablePieces();

            if (_board.HasAnyMatch())
                continue;

            if (hintFinder.TryFindHint(_board, out _))
                return true;
        }

        return false;
    }
}
