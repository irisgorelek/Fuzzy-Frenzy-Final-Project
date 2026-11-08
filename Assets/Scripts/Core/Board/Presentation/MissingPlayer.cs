using System;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

internal sealed class MissingPlayer : ITurnStepPlayer
{
    public static readonly MissingPlayer Instance = new MissingPlayer();
    public Type StepType => typeof(TurnStep);
    public Task PlayAsync(TurnStep step, PresentationContext context) => Task.CompletedTask;
}
