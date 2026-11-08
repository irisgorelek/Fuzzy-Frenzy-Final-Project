using System;
using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Pooled working memory for one board. Each buffer has a single user at a time; callers take it,
    /// use it and are done before returning, so nothing here is shared across calls.
    /// </summary>
    internal sealed class BoardScratch
    {
        private readonly bool[] _seen;

        public BoardScratch(int cellCount)
        {
            _seen = new bool[cellCount];
        }

        public readonly List<SwapMove> Moves = new List<SwapMove>();
        public readonly List<GridPos> ShuffleCells = new List<GridPos>();
        public readonly List<Tile> ShuffleTiles = new List<Tile>();

        /// <summary>A cleared per-cell flag array, indexed y * width + x.</summary>
        public bool[] BeginSeen()
        {
            Array.Clear(_seen, 0, _seen.Length);
            return _seen;
        }
    }
}
