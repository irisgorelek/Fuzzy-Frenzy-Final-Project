namespace FuzzyFrenzy.Domain
{
    /// <summary>How a clear treats the cells it's given.</summary>
    public enum ClearCause
    {
        /// <summary>Matched tiles. Every cell in the list is a matchable tile.</summary>
        Match,
        /// <summary>A special's line blast. Blast-resistant tiles (bones) and empty cells in the line are skipped.</summary>
        Explosion,
        /// <summary>A booster area (bomb). Duplicates, out-of-bounds cells, blast-resistant tiles and empty cells are skipped.</summary>
        Booster
    }
}
