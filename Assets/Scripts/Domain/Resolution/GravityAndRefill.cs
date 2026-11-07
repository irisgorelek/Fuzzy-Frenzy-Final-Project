using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    public readonly struct FallMove
    {
        public readonly GridPos From;
        public readonly GridPos To;

        public FallMove(GridPos from, GridPos to)
        {
            From = from;
            To = to;
        }
    }

    public readonly struct SpawnedTile
    {
        /// <summary>Where the new tile lands.</summary>
        public readonly GridPos Cell;
        public readonly int Type;
        /// <summary>-1 when it enters from above the board, otherwise the row of the blocker it enters below.</summary>
        public readonly int SpawnFromY;

        public SpawnedTile(GridPos cell, int type, int spawnFromY)
        {
            Cell = cell;
            Type = type;
            SpawnFromY = spawnFromY;
        }
    }

    /// <summary>Compacts each column downward around tiles that don't fall (bones), and records every move.</summary>
    public static class GravitySolver
    {
        public static void Apply(BoardState board, List<FallMove> moves)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int writeY = board.Height - 1; // next free slot, scanning bottom to top

                for (int y = board.Height - 1; y >= 0; y--)
                {
                    var pos = new GridPos(x, y);
                    var def = board.DefinitionAt(pos);
                    if (def == null)
                        continue;

                    if (!def.AffectedByGravity)
                    {
                        writeY = y - 1; // a blocker starts a new segment above it
                        continue;
                    }

                    if (y != writeY)
                    {
                        while (writeY >= 0 && IsBlocker(board, new GridPos(x, writeY)))
                            writeY--;

                        if (writeY < 0)
                            break;

                        var target = new GridPos(x, writeY);
                        moves?.Add(new FallMove(pos, target));
                        board.Move(pos, target);
                    }

                    writeY--;
                }
            }
        }

        private static bool IsBlocker(BoardState board, GridPos p)
        {
            var def = board.DefinitionAt(p);
            return def != null && !def.AffectedByGravity;
        }
    }

    /// <summary>Fills empty cells from the spawn pool and records where each new tile enters.</summary>
    public static class RefillPlanner
    {
        public static void Fill(BoardState board, IRandomSource random, List<SpawnedTile> spawns)
        {
            var empties = new List<GridPos>();
            for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++)
                {
                    var p = new GridPos(x, y);
                    if (board.IsEmpty(p)) empties.Add(p);
                }

            // An armed spawn (a black sheep) takes exactly one random empty cell.
            int armed = board.ArmedSpawnType;
            if (armed >= 0 && empties.Count > 0)
            {
                var cell = empties[random.Range(0, empties.Count)];
                board.Place(cell, armed);
                spawns?.Add(new SpawnedTile(cell, armed, SpawnFromY(board, cell)));
                board.ArmedSpawnType = -1;
                empties.Remove(cell);
            }

            for (int i = 0; i < empties.Count; i++)
            {
                var cell = empties[i];
                int type = board.ForcedSpawns.Count > 0 ? board.ForcedSpawns.Dequeue() : PickWeighted(board, random);
                board.Place(cell, type);
                spawns?.Add(new SpawnedTile(cell, type, SpawnFromY(board, cell)));
            }
        }

        /// <summary>Weighted random pick from the spawn pool. Returns -1 if the pool is empty.</summary>
        public static int PickWeighted(BoardState board, IRandomSource random)
        {
            var pool = board.SpawnPool;
            if (pool.Count == 0)
                return -1;

            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
                total += Weight(board, pool[i]);

            if (total <= 0f)
                return pool[random.Range(0, pool.Count)];

            float r = random.Value() * total;
            float cumulative = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += Weight(board, pool[i]);
                if (r <= cumulative)
                    return pool[i];
            }

            return pool[pool.Count - 1];
        }

        private static float Weight(BoardState board, int type)
        {
            var def = board.Catalog.Get(type);
            return def == null || def.SpawnWeight < 0f ? 0f : def.SpawnWeight;
        }

        /// <summary>New tiles enter below the nearest blocker above them, so they never fly through a bone.</summary>
        public static int SpawnFromY(BoardState board, GridPos target)
        {
            for (int y = target.Y - 1; y >= 0; y--)
            {
                var def = board.DefinitionAt(new GridPos(target.X, y));
                if (def != null && !def.AffectedByGravity)
                    return y;
            }
            return -1;
        }
    }
}
