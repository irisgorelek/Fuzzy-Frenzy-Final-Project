using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Finds every cell that is part of a horizontal or vertical run of 3+ of the same matchable tile.
    /// Never changes the board and allocates nothing: it reuses the board's scratch buffers, because the
    /// hint and dead-board search call it once per possible swap.
    /// </summary>
    public static class MatchFinder
    {
        /// <summary>True if any run of 3+ exists. Stops at the first one found.</summary>
        public static bool HasAnyMatch(BoardState board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int run = 1;
                for (int y = 1; y < board.Height; y++)
                {
                    run = Continues(board, x, y, x, y - 1) ? run + 1 : 1;
                    if (run >= 3) return true;
                }
            }

            for (int y = 0; y < board.Height; y++)
            {
                int run = 1;
                for (int x = 1; x < board.Width; x++)
                {
                    run = Continues(board, x, y, x - 1, y) ? run + 1 : 1;
                    if (run >= 3) return true;
                }
            }

            return false;
        }

        /// <summary>Fills <paramref name="results"/> with matched cells, each once, columns first then rows.</summary>
        public static void Find(BoardState board, List<GridPos> results)
        {
            results.Clear();
            var seen = board.Scratch.BeginSeen();

            for (int x = 0; x < board.Width; x++)
            {
                int run = 1;
                for (int y = 1; y < board.Height; y++)
                {
                    if (Continues(board, x, y, x, y - 1))
                    {
                        run++;
                        continue;
                    }
                    FlushColumn(board, x, y - 1, run, results, seen);
                    run = 1;
                }
                FlushColumn(board, x, board.Height - 1, run, results, seen);
            }

            for (int y = 0; y < board.Height; y++)
            {
                int run = 1;
                for (int x = 1; x < board.Width; x++)
                {
                    if (Continues(board, x, y, x - 1, y))
                    {
                        run++;
                        continue;
                    }
                    FlushRow(board, x - 1, y, run, results, seen);
                    run = 1;
                }
                FlushRow(board, board.Width - 1, y, run, results, seen);
            }
        }

        // Both cells are matchable and hold the same tile type.
        private static bool Continues(BoardState board, int x, int y, int prevX, int prevY)
        {
            var here = new GridPos(x, y);
            var prev = new GridPos(prevX, prevY);
            return board.IsMatchable(here) && board.IsMatchable(prev) && board.TypeAt(here) == board.TypeAt(prev);
        }

        private static void FlushColumn(BoardState board, int x, int endY, int run, List<GridPos> results, bool[] seen)
        {
            if (run < 3) return;
            for (int i = 0; i < run; i++)
                Add(board, new GridPos(x, endY - i), results, seen);
        }

        private static void FlushRow(BoardState board, int endX, int y, int run, List<GridPos> results, bool[] seen)
        {
            if (run < 3) return;
            for (int i = 0; i < run; i++)
                Add(board, new GridPos(endX - i, y), results, seen);
        }

        private static void Add(BoardState board, GridPos p, List<GridPos> results, bool[] seen)
        {
            int index = p.Y * board.Width + p.X;
            if (seen[index]) return;
            seen[index] = true;
            results.Add(p);
        }
    }
}
