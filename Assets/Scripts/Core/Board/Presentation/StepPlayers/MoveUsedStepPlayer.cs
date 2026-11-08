using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class MoveUsedStepPlayer : TurnStepPlayer<MoveUsedStep>
{
    protected override Task Play(MoveUsedStep step, PresentationContext context)
    {
        if (step.ByMatch) context.PlaySfx(SfxId.Pop, StepPlayerTuning.PopVolume);
        context.Listener.OnMoveUsed();
        return Task.CompletedTask;
    }
}
