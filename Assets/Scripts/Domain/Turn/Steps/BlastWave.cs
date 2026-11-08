using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    public sealed class BlastWave
    {
        public BlastWave(IReadOnlyList<GridPos> lineCells, IReadOnlyList<GridPos> popCells, IReadOnlyList<GridPos> bonesBroken, IReadOnlyList<string> boneIds)
        {
            LineCells = lineCells;
            PopCells = popCells;
            TilesBroken = bonesBroken;
            BrokenIds = boneIds;
        }

        /// <summary>The cells this wave reaches.</summary>
        public IReadOnlyList<GridPos> LineCells { get; }
        /// <summary>Tiles that visibly pop in this wave (not bones or empty cells).</summary>
        public IReadOnlyList<GridPos> PopCells { get; }
        /// <summary>Bones next to this wave's cells, broken as the wave passes.</summary>
        public IReadOnlyList<GridPos> TilesBroken { get; }
        /// <summary>The tile ID of each broken bone, in the same order as <see cref="TilesBroken"/>.</summary>
        public IReadOnlyList<string> BrokenIds { get; }
    }
}
