using System;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

/// <summary>
/// Shows one kind of <see cref="TurnStep"/>. Adding a new special means adding its step and a player for it,
/// and registering the player in <see cref="LevelFactory"/>; the presenter itself never changes.
/// </summary>
public interface ITurnStepPlayer
{
    Type StepType { get; }
    Task PlayAsync(TurnStep step, PresentationContext context);
}
