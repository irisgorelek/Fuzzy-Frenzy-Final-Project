using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Once the board settles, every predator eats the prey next to it (see <see cref="TileDefinition.EatsId"/>).
    /// The wolf eating sheep and leaving bones is one case; new predators only need data.
    /// </summary>
    public sealed class PredatorsEatRule : ActionRule<BoardSettledAction>
    {
        private readonly List<Meal> _buffer = new List<Meal>();

        protected override void React(TurnRun run, BoardSettledAction applied)
        {
            BoardOperations.FindMeals(run.Board, _buffer);
            if (_buffer.Count > 0)
                run.Enqueue(new FeastAction(_buffer.ToArray()));
        }
    }
}
