using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

public sealed class TilesBrokenStepPlayer : TurnStepPlayer<TilesBrokenStep>
{
    protected override Task Play(TilesBrokenStep step, PresentationContext context)
    {
        context.ReportTilesBroken(step.Cells, step.Ids);
        context.Listener.OnGoalProgressChanged();
        return Task.CompletedTask;
    }
}
