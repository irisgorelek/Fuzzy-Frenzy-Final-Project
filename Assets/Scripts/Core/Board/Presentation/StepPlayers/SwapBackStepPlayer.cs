using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class SwapBackStepPlayer : TurnStepPlayer<SwapBackStep>
{
    protected override Task Play(SwapBackStep step, PresentationContext context) =>
        context.View.AnimateSwap(step.From.ToVector(), step.To.ToVector(), StepPlayerTuning.SwapSeconds);
}
