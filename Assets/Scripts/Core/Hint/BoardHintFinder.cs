using System.Collections.Generic;
using UnityEngine;

public class BoardHintFinder
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.up
    };

    public bool TryFindHint(Board board, out HintMove hint)
    {
        List<HintMove> validMoves = new List<HintMove>(); // list of valid moves

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Vector2Int from = new Vector2Int(x, y);

                foreach (var dir in Directions) // try to match both right and up
                {
                    Vector2Int to = from + dir;

                    if (!board.IsCellInBounds(to))
                        continue;

                    if (WouldCreateMatch(board, from, to)) // if the match is possible -> add it to the valid moves list
                        validMoves.Add(new HintMove(from, to));
                }
            }
        }

        if (validMoves.Count == 0) // no valid moves were found
        {
            hint = default;
            return false;
        }

        hint = validMoves[Random.Range(0, validMoves.Count)]; // give the user a random valid move out of the ones found
        return true;
    }

    private bool WouldCreateMatch(Board board, Vector2Int from, Vector2Int to)
    {
        var a = board.GetAnimalFromCell(from);
        var b = board.GetAnimalFromCell(to);

        if (a == null || b == null)
            return false;

        if (!a._canSwap || !b._canSwap)
            return false;

        if (!board.SwapCellsRaw(from, to)) // swap the given cells 
            return false;

        bool createsMatch = board.FindMatches().Count > 0; // check if the swap causes matches

        board.SwapCellsRaw(from, to); // revert swap
        return createsMatch;
    }
}