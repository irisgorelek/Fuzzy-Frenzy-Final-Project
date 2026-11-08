using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class LineBlastStepPlayer : TurnStepPlayer<LineBlastStep>
{
    protected override async Task Play(LineBlastStep step, PresentationContext context)
    {
        var view = context.View;
        var center = step.Center.ToVector();
        bool vertical = step.SwipedVertically;

        await view.AnimateLineBlastCharge(center);
        view.PlayLineBlastFx(center, vertical, view.Width, view.Height);
        context.PlaySfx(vertical ? SfxId.LineBlastShort : SfxId.LineBlastLong);

        foreach (var wave in step.Waves)
        {
            if (wave.TilesBroken.Count > 0)
            {
                context.ReportTilesBroken(wave.TilesBroken, wave.BrokenIds);
                context.Listener.OnGoalProgressChanged();
            }

            if (wave.PopCells.Count > 0)
                context.PlaySfx(SfxId.Pop, StepPlayerTuning.PopVolume);

            await view.AnimateMatchPopFx(wave.PopCells.ToVectors(), BoardEffectsTiming.LineStep, BoardClearEffect.BlackSheep);
        }

        await view.WaitForLineBlastFx();
    }
}
