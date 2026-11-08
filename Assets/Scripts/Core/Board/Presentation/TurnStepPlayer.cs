using System;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

/// <summary>Typed base for step players.</summary>
public abstract class TurnStepPlayer<TStep> : ITurnStepPlayer where TStep : TurnStep
{
    public Type StepType => typeof(TStep);

    public Task PlayAsync(TurnStep step, PresentationContext context) => Play((TStep)step, context);

    protected abstract Task Play(TStep step, PresentationContext context);
}
