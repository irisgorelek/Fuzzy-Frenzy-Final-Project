using System.Collections.Generic;
using FuzzyFrenzy.Domain;

/// <summary>What a step player can use: the board view, Animal lookups, gameplay callbacks and audio.</summary>
public sealed class PresentationContext
{
    public PresentationContext(IBoardAnimator view, AnimalCatalog catalog, ITurnPresentationListener listener, IGameAudio audio)
    {
        View = view;
        Catalog = catalog;
        Listener = listener;
        Audio = audio ?? SilentAudio.Instance;
    }

    public IBoardAnimator View { get; }
    public AnimalCatalog Catalog { get; }
    public ITurnPresentationListener Listener { get; }
    public IGameAudio Audio { get; }

    /// <summary>Plays a sound with a little pitch variation, since turn sounds repeat a lot.</summary>
    public void PlaySfx(SfxId sfx, float volume = 1f) => Audio.PlaySfxVaried(sfx, volume);

    /// <summary>Shows broken obstacle tiles (effect and sound for each) and counts each one toward goals.</summary>
    public void ReportTilesBroken(IReadOnlyList<GridPos> cells, IReadOnlyList<string> ids)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            View.PlayTileBreakFx(cells[i].ToVector());
            Audio.PlayTileBreak();
            Listener.OnTilesDestroyed(ids[i], 1);
        }
    }
}
