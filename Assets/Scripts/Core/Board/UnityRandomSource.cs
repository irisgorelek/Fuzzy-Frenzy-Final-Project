using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>Uses Unity's global random, so gameplay randomness behaves exactly as before.</summary>
public sealed class UnityRandomSource : IRandomSource
{
    public int Range(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
    public float Value() => Random.value;
}
