using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class ClearStepPlayer : TurnStepPlayer<ClearStep>
{
    protected override Task Play(ClearStep step, PresentationContext context)
    {
        if (step.Cause == ClearCause.Match)
            for (int i = 0; i < step.Cleared.Count; i++)
                context.PlaySfx(SfxId.Pop, StepPlayerTuning.PopVolume);

        context.Listener.OnTilesScored(step.Points, step.Matched);

        for (int i = 0; i < step.Destroyed.Count; i++)
            context.Listener.OnTilesDestroyed(step.Destroyed[i].Id, step.Destroyed[i].Count);

        context.Listener.OnGoalProgressChanged();
        return Task.CompletedTask;
    }
}
