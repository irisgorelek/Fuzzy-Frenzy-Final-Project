using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class GravityStepPlayer : TurnStepPlayer<GravityStep>
{
    protected override async Task Play(GravityStep step, PresentationContext context)
    {
        await context.View.AnimateGravity(step.Falls, step.Spawns, new SnapshotAnimalGrid(step.After, context.Catalog), step.Duration);
        context.Listener.OnGoalProgressChanged();
    }
}
