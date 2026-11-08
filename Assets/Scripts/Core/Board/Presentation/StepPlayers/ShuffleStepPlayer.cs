using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class ShuffleStepPlayer : TurnStepPlayer<ShuffleStep>
{
    protected override async Task Play(ShuffleStep step, PresentationContext context)
    {
        await context.View.ShowShuffleMessage(0.35f);
        await context.View.AnimateShuffle(new SnapshotAnimalGrid(step.After, context.Catalog), 0.07f, 0.09f, 0.008f);
    }
}
