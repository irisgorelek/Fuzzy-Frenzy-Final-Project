using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class SwapStepPlayer : TurnStepPlayer<SwapStep>
{
    protected override Task Play(SwapStep step, PresentationContext context)
    {
        context.PlaySfx(SfxId.Swap, StepPlayerTuning.SwapVolume);
        return context.View.AnimateSwap(step.From.ToVector(), step.To.ToVector(), StepPlayerTuning.SwapSeconds);
    }
}
