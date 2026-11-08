namespace FuzzyFrenzy.Domain
{
    /// <summary>All randomness in the core goes through this, so tests and bug reports can be replayed with a seed.</summary>
    public interface IRandomSource
    {
        /// <summary>Integer in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>Float in [0, 1].</summary>
        float Value();
    }
}
