using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Tiles fell and new tiles entered.</summary>
    public sealed class GravityStep : TurnStep
    {
        public GravityStep(IReadOnlyList<FallMove> falls, IReadOnlyList<SpawnedTile> spawns, BoardSnapshot after, float duration)
        {
            Falls = falls;
            Spawns = spawns;
            After = after;
            Duration = duration;
        }

        public IReadOnlyList<FallMove> Falls { get; }
        public IReadOnlyList<SpawnedTile> Spawns { get; }
        /// <summary>The board once this step has finished.</summary>
        public BoardSnapshot After { get; }
        /// <summary>Suggested animation length in seconds.</summary>
        public float Duration { get; }
    }
}
