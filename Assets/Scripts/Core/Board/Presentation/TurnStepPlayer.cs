using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

/// <summary>What a step player can use: the board view, Animal lookups, gameplay callbacks and audio.</summary>
public sealed class PresentationContext
{
    public PresentationContext(IBoardAnimator view, AnimalCatalog catalog, ITurnPresentationListener listener)
    {
        View = view;
        Catalog = catalog;
        Listener = listener;
    }

    public IBoardAnimator View { get; }
    public AnimalCatalog Catalog { get; }
    public ITurnPresentationListener Listener { get; }

    public void PlaySfx(int id, float volume = 1f)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFXPitchAdjusted(id, volume);
    }

    /// <summary>Reports broken bones: effect and sound per bone, and each counted for goals.</summary>
    public void ReportBonesBroken(IReadOnlyList<GridPos> cells, IReadOnlyList<string> ids)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Listener.OnBoneBroken(cells[i].ToVector());
            Listener.OnTilesDestroyed(ids[i], 1);
        }
    }
}

/// <summary>
/// Shows one kind of <see cref="TurnStep"/>. Adding a new special means adding its step and a player for it,
/// and registering the player in <see cref="LevelFactory"/>; the presenter itself never changes.
/// </summary>
public interface ITurnStepPlayer
{
    Type StepType { get; }
    Task PlayAsync(TurnStep step, PresentationContext context);
}

/// <summary>Typed base for step players.</summary>
public abstract class TurnStepPlayer<TStep> : ITurnStepPlayer where TStep : TurnStep
{
    public Type StepType => typeof(TStep);

    public Task PlayAsync(TurnStep step, PresentationContext context) => Play((TStep)step, context);

    protected abstract Task Play(TStep step, PresentationContext context);
}
