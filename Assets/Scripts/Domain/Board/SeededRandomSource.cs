using System;

namespace FuzzyFrenzy.Domain
{
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
