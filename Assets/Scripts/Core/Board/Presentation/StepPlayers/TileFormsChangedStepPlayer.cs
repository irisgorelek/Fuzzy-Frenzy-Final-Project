using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class TileFormsChangedStepPlayer : TurnStepPlayer<TileFormsChangedStep>
{
    protected override Task Play(TileFormsChangedStep step, PresentationContext context)
    {
        for (int i = 0; i < step.Changes.Count; i++)
        {
            TileFormChange change = step.Changes[i];

            context.View.SetCellAnimal(change.Cell.ToVector(),context.Catalog.AnimalOf(change.NewType));
        }

        return Task.CompletedTask;
    }
}
