using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Finds swaps that would create a match, for hints and the dead-board check. Tries each swap on the
    /// real board and undoes it, so it allocates nothing.
    /// </summary>
    public static class MoveFinder
    {
        private static readonly GridPos[] Directions = { GridPos.Right, GridPos.Up };

        public static bool HasAnyMove(BoardState board)
        {
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    var from = new GridPos(x, y);
                    for (int d = 0; d < Directions.Length; d++)
                    {
                        var to = from + Directions[d];
                        if (board.InBounds(to) && WouldCreateMatch(board, from, to)) return true;
                    }
                }
            return false;
        }

        /// <summary>Picks one random legal move, so repeated hints don't always point at the same spot.</summary>
        public static bool TryFindMove(BoardState board, IRandomSource random, out SwapMove move)
        {
            var moves = board.Scratch.Moves;
            CollectMoves(board, moves);

            if (moves.Count == 0)
            {
                move = default;
                return false;
            }

            move = moves[random.Range(0, moves.Count)];
            return true;
        }

        public static void CollectMoves(BoardState board, List<SwapMove> moves)
        {
            moves.Clear();
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    var from = new GridPos(x, y);
                    for (int d = 0; d < Directions.Length; d++)
                    {
                        var to = from + Directions[d];
                        if (board.InBounds(to) && WouldCreateMatch(board, from, to))
                            moves.Add(new SwapMove(from, to));
                    }
                }
        }

        private static bool WouldCreateMatch(BoardState board, GridPos from, GridPos to)
        {
            var a = board.DefinitionAt(from);
            var b = board.DefinitionAt(to);
            if (a == null || b == null) return false;
            if (!a.CanSwap || !b.CanSwap) return false;

            board.Swap(from, to);
            bool matches = MatchFinder.HasAnyMatch(board);
            board.Swap(from, to);
            return matches;
        }
    }
}
