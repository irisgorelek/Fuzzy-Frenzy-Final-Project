using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class FeastStepPlayer : TurnStepPlayer<FeastStep>
{
    private const float NudgeSeconds = 0.20f;

    protected override async Task Play(FeastStep step, PresentationContext context)
    {
        context.Listener.OnTilesScored(step.Points, step.Eaten);
        for (int i = 0; i < step.Destroyed.Count; i++)
            context.Listener.OnTilesDestroyed(step.Destroyed[i].Id, step.Destroyed[i].Count);

        foreach (var meal in step.Meals)
        {
            context.PlaySfx(SfxId.BoneBreak, StepPlayerTuning.NudgeVolume);
            await context.View.AnimatePredatorNudge(meal.Predator.ToVector(), meal.Prey.ToVector(), NudgeSeconds);
            context.View.SetCellAnimal(meal.Prey.ToVector(), context.Catalog.AnimalOf(meal.RemainsType));
            context.Listener.OnGoalProgressChanged();
        }
    }
}
