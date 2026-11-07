using System;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Builds a level's starting board: no ready-made matches and no prey already next to its predator.</summary>
    public static class BoardGenerator
    {
        private const int MaxPlacementAttempts = 20;
        private const int MaxRerollAttempts = 30;

        /// <summary>
        /// Fills every cell, then removes tiles that never refill (bones, black sheep) from the spawn pool so
        /// they only appear through gameplay.
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

            board.SpawnPool.RemoveAll(type => board.Catalog.Get(type)?.Has(TileTraits.NoRefill) == true);

            SeparatePreyFromPredators(board, random);
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

        /// <summary>Rerolls each tile that starts next to something that eats it into one that is safe there and makes no match.</summary>
        private static void SeparatePreyFromPredators(BoardState board, IRandomSource random)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = new GridPos(x, y);
                    if (!IsInDanger(board, cell, board.DefinitionAt(cell))) continue;

                    int prey = board.TypeAt(cell);
                    for (int attempt = 0; attempt < MaxRerollAttempts; attempt++)
                    {
                        int candidate = RefillPlanner.PickWeighted(board, random);
                        if (candidate == prey) continue;
                        if (InFeedingPair(board, cell, board.Catalog.Get(candidate))) continue;
                        if (WouldCreateMatchAnywhere(board, cell, candidate)) continue;

                        board.Place(cell, candidate);
                        break;
                    }
                }
            }
        }

        /// <summary>True when a neighbour would eat a tile of this kind placed at the cell.</summary>
        private static bool IsInDanger(BoardState board, GridPos cell, TileDefinition def)
        {
            if (def == null) return false;
            for (int d = 0; d < GridPos.Orthogonal.Length; d++)
            {
                var n = cell + GridPos.Orthogonal[d];
                if (board.InBounds(n) && board.DefinitionAt(n)?.Eats(def) == true) return true;
            }
            return false;
        }

        /// <summary>True when a tile of this kind at the cell would eat, or be eaten by, a neighbour.</summary>
        private static bool InFeedingPair(BoardState board, GridPos cell, TileDefinition def)
        {
            if (def == null) return false;
            if (IsInDanger(board, cell, def)) return true;
            for (int d = 0; d < GridPos.Orthogonal.Length; d++)
            {
                var n = cell + GridPos.Orthogonal[d];
                if (board.InBounds(n) && def.Eats(board.DefinitionAt(n))) return true;
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
