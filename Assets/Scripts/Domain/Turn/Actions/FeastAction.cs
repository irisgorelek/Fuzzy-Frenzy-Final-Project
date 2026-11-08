using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Predators eat the prey next to them; each prey becomes the predator's remains (the wolf leaves bones).</summary>
    public sealed class FeastAction : IBoardAction
    {
        private readonly IReadOnlyList<Meal> _meals;

        public FeastAction(IReadOnlyList<Meal> meals) { _meals = meals; }

        public void Apply(TurnRun run)
        {
            var eaten = new ClearReport();
            BoardOperations.ApplyMeals(run.Board, _meals, eaten);

            run.Session.AddPoints(eaten.Points);
            run.Session.AddMatched(eaten.Matched);
            for (int i = 0; i < eaten.Destroyed.Count; i++)
                run.Session.AddDestroyed(eaten.Destroyed[i].Id, eaten.Destroyed[i].Count);

            run.Record(new FeastStep(_meals, eaten));
        }
    }
}
