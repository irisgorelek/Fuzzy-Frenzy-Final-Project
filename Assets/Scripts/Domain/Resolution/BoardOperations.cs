using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// The board mutations the game is built from. Actions decide when these run; this class decides how.
    /// Every method is deterministic given the board and the random source.
    /// </summary>
    public static class BoardOperations
    {
        public static ClearReport Clear(BoardState board, IReadOnlyList<GridPos> cells, ClearCause cause)
        {
            var report = new ClearReport();
            if (cells == null) return report;

            HashSet<GridPos> unique = cause == ClearCause.Booster ? new HashSet<GridPos>() : null;

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (unique != null && !unique.Add(cell)) continue;
                if (!board.InBounds(cell)) continue;

                var def = board.DefinitionAt(cell);
                if (def == null) continue;
                if (cause != ClearCause.Match && def.Has(TileTraits.ResistsBlasts)) continue;

                report.Points += def.Points;
                if (cause != ClearCause.Booster || !string.IsNullOrEmpty(def.Id))
                    report.CountDestroyed(def.Id);

                report.Cleared.Add(cell);
                report.ClearedTiles.Add(new ClearedTile(cell, def.Type));

                board.Clear(cell);
            }

            return report;
        }

        /// <summary>Tiles that break next to a clear (bones) orthogonally next to any source cell, each once, in source order.</summary>
        public static void FindBreakableNeighbours(BoardState board, IReadOnlyList<GridPos> sources, List<GridPos> results)
        {
            results.Clear();
            if (sources == null) return;

            var seen = board.Scratch.BeginSeen();
            for (int i = 0; i < sources.Count; i++)
            {
                for (int d = 0; d < GridPos.Orthogonal.Length; d++)
                {
                    var n = sources[i] + GridPos.Orthogonal[d];
                    if (!board.InBounds(n) || !board.Has(n, TileTraits.BreaksNextToClear)) continue;

                    int index = n.Y * board.Width + n.X;
                    if (seen[index]) continue;
                    seen[index] = true;
                    results.Add(n);
                }
            }
        }

        /// <summary>Every tile with a predator next to it, paired with the first predator in neighbour order.</summary>
        public static void FindMeals(BoardState board, List<Meal> results)
        {
            results.Clear();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var prey = new GridPos(x, y);
                    var preyDef = board.DefinitionAt(prey);
                    if (preyDef == null) continue;

                    for (int d = 0; d < GridPos.Orthogonal.Length; d++)
                    {
                        var predator = prey + GridPos.Orthogonal[d];
                        if (!board.InBounds(predator)) continue;

                        var predatorDef = board.DefinitionAt(predator);
                        if (predatorDef == null || !predatorDef.Eats(preyDef)) continue;

                        results.Add(new Meal(predator, prey, board.Catalog.TypeOfId(predatorDef.LeavesId)));
                        break;
                    }
                }
            }
        }

        /// <summary>Replaces each eaten tile with its remains and counts it. Returns the points the eaten tiles were worth.</summary>
        public static int ApplyMeals(BoardState board, IReadOnlyList<Meal> meals, ClearReport eaten)
        {
            int points = 0;
            for (int i = 0; i < meals.Count; i++)
            {
                var def = board.DefinitionAt(meals[i].Prey);
                if (def == null) continue;

                points += def.Points;
                eaten.CountDestroyed(def.Id);
                eaten.Cleared.Add(meals[i].Prey);
                board.Place(meals[i].Prey, meals[i].RemainsType);
            }
            eaten.Points += points;
            return points;
        }

        /// <summary>
        /// The black sheep's line, as waves spreading out from the centre. A swipe along a column (vertical)
        /// blasts the row; a swipe along a row blasts the column.
        /// </summary>
        public static void BuildBlastWaves(BoardState board, GridPos center, bool swipedVertically, List<List<GridPos>> waves, List<GridPos> line)
        {
            waves.Clear();
            line.Clear();

            int maxWave = swipedVertically
                ? System.Math.Max(center.X, board.Width - 1 - center.X)
                : System.Math.Max(center.Y, board.Height - 1 - center.Y);

            for (int wave = 0; wave <= maxWave; wave++)
            {
                var cells = new List<GridPos>(2);
                if (swipedVertically)
                {
                    int left = center.X - wave;
                    int right = center.X + wave;
                    if (left >= 0) cells.Add(new GridPos(left, center.Y));
                    if (right < board.Width && right != left) cells.Add(new GridPos(right, center.Y));
                }
                else
                {
                    int down = center.Y - wave;
                    int up = center.Y + wave;
                    if (down >= 0) cells.Add(new GridPos(center.X, down));
                    if (up < board.Height && up != down) cells.Add(new GridPos(center.X, up));
                }
                waves.Add(cells);
            }

            if (swipedVertically)
                for (int x = 0; x < board.Width; x++) line.Add(new GridPos(x, center.Y));
            else
                for (int y = 0; y < board.Height; y++) line.Add(new GridPos(center.X, y));
        }

        /// <summary>Shuffles every swappable tile into the swappable cells. Blockers stay where they are.</summary>
        public static void ShuffleSwappable(BoardState board, IRandomSource random)
        {
            var cells = board.Scratch.ShuffleCells;
            var tiles = board.Scratch.ShuffleTiles;
            cells.Clear();
            tiles.Clear();

            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    var p = new GridPos(x, y);
                    var def = board.DefinitionAt(p);
                    if (def == null || !def.CanSwap) continue;
                    cells.Add(p);
                    tiles.Add(board.Get(p));
                }

            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }

            for (int i = 0; i < cells.Count; i++)
                board.Put(cells[i], tiles[i]);
        }

        /// <summary>Shuffles until the board has no ready-made match and at least one legal move.</summary>
        public static bool ShuffleUntilPlayable(BoardState board, IRandomSource random, int maxAttempts)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                ShuffleSwappable(board, random);
                if (MatchFinder.HasAnyMatch(board)) continue;
                if (MoveFinder.HasAnyMove(board)) return true;
            }
            return false;
        }
    }
}
