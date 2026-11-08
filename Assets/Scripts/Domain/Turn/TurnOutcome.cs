namespace FuzzyFrenzy.Domain
{
    public enum TurnOutcome
    {
        /// <summary>The input wasn't allowed. Nothing changed.</summary>
        Rejected,
        /// <summary>The tiles swapped but made no match, so they swapped back. No move used.</summary>
        NoMatch,
        /// <summary>The turn played out.</summary>
        Resolved
    }
}
