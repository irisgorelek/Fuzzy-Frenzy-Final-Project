namespace FuzzyFrenzy.Domain
{
    public enum RejectReason
    {
        None,
        /// <summary>One of the cells is empty. Ignored silently.</summary>
        EmptyCell,
        /// <summary>One of the tiles can't be swapped (bones).</summary>
        NotSwappable,
        /// <summary>The cells aren't neighbours or are off the board.</summary>
        NotAdjacent
    }
}
