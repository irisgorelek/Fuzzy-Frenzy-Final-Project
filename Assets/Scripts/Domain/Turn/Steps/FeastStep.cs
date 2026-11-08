using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Predators ate the prey next to them; each prey became the predator's remains.</summary>
    public sealed class FeastStep : TurnStep
    {
        public FeastStep(IReadOnlyList<Meal> meals, ClearReport eaten)
        {
            Meals = meals;
            Points = eaten.Points;
            Eaten = eaten.Matched;
            Destroyed = eaten.Destroyed;
        }

        public IReadOnlyList<Meal> Meals { get; }
        public int Points { get; }
        /// <summary>Tiles eaten. Counts toward "matches" goals.</summary>
        public int Eaten { get; }
        public IReadOnlyList<DestroyedCount> Destroyed { get; }
    }
}
