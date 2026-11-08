using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Tiles fall into the gaps and new tiles fill the board.</summary>
    public sealed class GravityRefillAction : IBoardAction
    {
        private readonly float _duration;

        public GravityRefillAction(float duration) { _duration = duration; }

        public void Apply(TurnRun run)
        {
            var falls = new List<FallMove>();
            var spawns = new List<SpawnedTile>();
            GravitySolver.Apply(run.Board, falls);
            RefillPlanner.Fill(run.Board, run.Random, spawns);
            run.Record(new GravityStep(falls, spawns, run.Board.Snapshot(), _duration));
        }
    }
}
