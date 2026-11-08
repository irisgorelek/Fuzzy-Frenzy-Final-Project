using System;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Behaviours a tile can have. Rules check traits, never particular animals, so a new animal or obstacle
    /// with an existing behaviour is just data.
    /// </summary>
    [Flags]
    public enum TileTraits
    {
        None = 0,
        /// <summary>Breaks when a tile next to it is cleared (bones).</summary>
        BreaksNextToClear = 1 << 0,
        /// <summary>Line blasts and boosters pass over it without clearing it (bones).</summary>
        ResistsBlasts = 1 << 1,
        /// <summary>May be on the starting board but never refills. It appears later only through gameplay.</summary>
        NoRefill = 1 << 2,
        /// <summary>Swapping it blasts a row or column (black sheep).</summary>
        LineBlastOnSwap = 1 << 3,
        /// <summary> Advances in states throughout turns </summary>
        AdvancesFormEachTurn    = 1 << 4,
        /// <summary> After being permenantly awake, becomes a regular tile </summary>
        CanBePermanentlyAwakened = 1 << 5,
        /// <summary> Matching it wakes the nearby sleeping animals </summary>
        PermanentlyWakesAdjacent = 1 << 6
    }
}
