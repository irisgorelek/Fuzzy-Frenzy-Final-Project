using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class MatchPopStepPlayer : TurnStepPlayer<MatchPopStep>
{
    protected override Task Play(MatchPopStep step, PresentationContext context) =>
        context.View.AnimateMatchPopFx(step.Cells.ToVectors(), BoardEffectsTiming.MatchPop, BoardClearEffect.Match);
}
