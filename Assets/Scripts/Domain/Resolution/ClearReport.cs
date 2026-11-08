using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>What a clear removed. The caller adds it to the session and records it for the view.</summary>
    public sealed class ClearReport
    {
        public readonly List<GridPos> Cleared = new List<GridPos>();
        public readonly List<ClearedTile> ClearedTiles = new List<ClearedTile>();
        public readonly List<DestroyedCount> Destroyed = new List<DestroyedCount>();
        public int Points;

        /// <summary>Number of tiles removed. Counts toward "matches" goals.</summary>
        public int Matched => Cleared.Count;

        internal void CountDestroyed(string id)
        {
            for (int i = 0; i < Destroyed.Count; i++)
            {
                if (Destroyed[i].Id != id) continue;
                Destroyed[i] = new DestroyedCount(id, Destroyed[i].Count + 1);
                return;
            }
            Destroyed.Add(new DestroyedCount(id, 1));
        }
    }
}
