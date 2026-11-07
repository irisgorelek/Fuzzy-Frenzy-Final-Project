using System;

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

    /// <summary>Seeded random source for tests and deterministic replays.</summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource(int seed) => _random = new Random(seed);

        public int Range(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : _random.Next(minInclusive, maxExclusive);

        public float Value() => (float)_random.NextDouble();
    }
}
