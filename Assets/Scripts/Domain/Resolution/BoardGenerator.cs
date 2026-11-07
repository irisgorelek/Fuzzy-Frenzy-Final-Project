using System;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Builds a level's starting board: no ready-made matches and no sheep already next to a wolf.</summary>
    public static class BoardGenerator
    {
        private const int MaxPlacementAttempts = 20;
        private const int MaxRerollAttempts = 30;

        /// <summary>
        /// Fills every cell, then removes bones and black sheep from the spawn pool so they
        /// only appear through gameplay.
        /// </summary>
        public static void Generate(BoardState board, IRandomSource random)
        {
            if (board.SpawnPool.Count == 0)
                throw new InvalidOperationException("Board has no allowed animals. Check BoardConfig.");

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = new GridPos(x, y);
                    int attempts = 0;
                    int chosen;

                    do
                    {
                        chosen = RefillPlanner.PickWeighted(board, random);
                        attempts++;
                    }
                    while (WouldCreateInitialMatch(board, cell, chosen) && attempts < MaxPlacementAttempts);

                    board.Place(cell, chosen);
                }
            }

            board.SpawnPool.Remove(board.Specials.Bone);
            board.SpawnPool.Remove(board.Specials.BlackSheep);

            SeparateSheepFromWolves(board, random);
        }

        // Cells are filled column by column, top to bottom, so only the two cells to the left and above are set.
        private static bool WouldCreateInitialMatch(BoardState board, GridPos cell, int type)
        {
            if (cell.X >= 2 &&
                board.TypeAt(new GridPos(cell.X - 1, cell.Y)) == type &&
                board.TypeAt(new GridPos(cell.X - 2, cell.Y)) == type)
                return true;

            if (cell.Y >= 2 &&
                board.TypeAt(new GridPos(cell.X, cell.Y - 1)) == type &&
                board.TypeAt(new GridPos(cell.X, cell.Y - 2)) == type)
                return true;

            return false;
        }

        /// <summary>Rerolls each sheep that starts next to a wolf into another animal that doesn't create a match.</summary>
        private static void SeparateSheepFromWolves(BoardState board, IRandomSource random)
        {
            var s = board.Specials;
            if (s.Wolf < 0 || s.Sheep < 0) return;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = new GridPos(x, y);
                    if (board.TypeAt(cell) != s.Sheep || !HasNeighbour(board, cell, s.Wolf)) continue;

                    for (int attempt = 0; attempt < MaxRerollAttempts; attempt++)
                    {
                        int candidate = RefillPlanner.PickWeighted(board, random);
                        if (candidate == s.Sheep || candidate == s.Wolf) continue;
                        if (WouldCreateMatchAnywhere(board, cell, candidate)) continue;

                        board.Place(cell, candidate);
                        break;
                    }
                }
            }
        }

        private static bool HasNeighbour(BoardState board, GridPos cell, int type)
        {
            for (int d = 0; d < GridPos.Orthogonal.Length; d++)
            {
                var n = cell + GridPos.Orthogonal[d];
                if (board.InBounds(n) && board.TypeAt(n) == type) return true;
            }
            return false;
        }

        private static bool WouldCreateMatchAnywhere(BoardState board, GridPos cell, int type)
        {
            int h = 1 + CountInDirection(board, cell, GridPos.Left, type) + CountInDirection(board, cell, GridPos.Right, type);
            if (h >= 3) return true;

            int v = 1 + CountInDirection(board, cell, GridPos.Down, type) + CountInDirection(board, cell, GridPos.Up, type);
            return v >= 3;
        }

        private static int CountInDirection(BoardState board, GridPos start, GridPos dir, int type)
        {
            int count = 0;
            var p = start + dir;
            while (board.InBounds(p) && board.TypeAt(p) == type)
            {
                count++;
                p += dir;
            }
            return count;
        }
    }
}
