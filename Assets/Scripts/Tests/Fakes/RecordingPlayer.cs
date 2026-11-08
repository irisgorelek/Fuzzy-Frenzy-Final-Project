using System.Threading.Tasks;

internal sealed class RecordingPlayer : TurnStepPlayer<CustomStep>
{
    public int Played;
    protected override Task Play(CustomStep step, PresentationContext context)
    {
        Played++;
        return Task.CompletedTask;
    }
}
