using System.Collections.Generic;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;

// One small player per step type. See TurnStepPlayer for how to add one.

/// <summary>AudioManager SFX ids and shared timings used while replaying turns.</summary>
internal static class TurnSfx
{
    public const int Pop = 8;
    public const int Swap = 12;
    public const int SheepColumnBlast = 17;
    public const int SheepRowBlast = 18;

    public const float PopVolume = 0.2f;
    public const float SwapVolume = 0.2f;
    public const float SwapSeconds = 0.18f;
}

public sealed class SwapStepPlayer : TurnStepPlayer<SwapStep>
{
    protected override Task Play(SwapStep step, PresentationContext context)
    {
        context.PlaySfx(TurnSfx.Swap, TurnSfx.SwapVolume);
        return context.View.AnimateSwap(step.From.ToVector(), step.To.ToVector(), TurnSfx.SwapSeconds);
    }
}

public sealed class SwapBackStepPlayer : TurnStepPlayer<SwapBackStep>
{
    protected override Task Play(SwapBackStep step, PresentationContext context) =>
        context.View.AnimateSwap(step.From.ToVector(), step.To.ToVector(), TurnSfx.SwapSeconds);
}

public sealed class MoveUsedStepPlayer : TurnStepPlayer<MoveUsedStep>
{
    protected override Task Play(MoveUsedStep step, PresentationContext context)
    {
        if (step.ByMatch) context.PlaySfx(TurnSfx.Pop, TurnSfx.PopVolume);
        context.Listener.OnMoveUsed();
        return Task.CompletedTask;
    }
}

public sealed class MatchPopStepPlayer : TurnStepPlayer<MatchPopStep>
{
    protected override Task Play(MatchPopStep step, PresentationContext context) =>
        context.View.AnimateMatchPopFx(step.Cells.ToVectors(), BoardEffectsTiming.MatchPop, BoardClearEffect.Match);
}

public sealed class ClearStepPlayer : TurnStepPlayer<ClearStep>
{
    protected override Task Play(ClearStep step, PresentationContext context)
    {
        if (step.Cause == ClearCause.Match)
            for (int i = 0; i < step.Cleared.Count; i++)
                context.PlaySfx(TurnSfx.Pop, TurnSfx.PopVolume);

        context.Listener.OnTilesScored(step.Points, step.Matched);

        for (int i = 0; i < step.Destroyed.Count; i++)
            context.Listener.OnTilesDestroyed(step.Destroyed[i].Id, step.Destroyed[i].Count);

        context.Listener.OnGoalProgressChanged();
        return Task.CompletedTask;
    }
}

public sealed class BonesBrokenStepPlayer : TurnStepPlayer<BonesBrokenStep>
{
    protected override Task Play(BonesBrokenStep step, PresentationContext context)
    {
        context.ReportBonesBroken(step.Cells, step.Ids);
        context.Listener.OnGoalProgressChanged();
        return Task.CompletedTask;
    }
}

public sealed class BlackSheepBlastStepPlayer : TurnStepPlayer<BlackSheepBlastStep>
{
    protected override async Task Play(BlackSheepBlastStep step, PresentationContext context)
    {
        var view = context.View;
        var center = step.Center.ToVector();
        bool vertical = step.SwipedVertically;

        await view.AnimateSheepAnger(center);
        view.PlayBlackSheepFx(center, vertical, view.Width, view.Height);
        context.PlaySfx(vertical ? TurnSfx.SheepRowBlast : TurnSfx.SheepColumnBlast);

        foreach (var wave in step.Waves)
        {
            if (wave.BonesBroken.Count > 0)
            {
                context.ReportBonesBroken(wave.BonesBroken, wave.BoneIds);
                context.Listener.OnGoalProgressChanged();
            }

            if (wave.PopCells.Count > 0)
                context.PlaySfx(TurnSfx.Pop, TurnSfx.PopVolume);

            await view.AnimateMatchPopFx(wave.PopCells.ToVectors(), BoardEffectsTiming.LineStep, BoardClearEffect.BlackSheep);
        }

        await view.WaitForBlackSheepParticles();
    }
}

public sealed class GravityStepPlayer : TurnStepPlayer<GravityStep>
{
    // Reused every cascade. Safe because steps play one at a time and the view only reads these lists
    // while building its animation, before the await returns.
    private readonly List<Board.FallMove> _falls = new List<Board.FallMove>();
    private readonly List<Board.SpawnInfo> _spawns = new List<Board.SpawnInfo>();

    protected override async Task Play(GravityStep step, PresentationContext context)
    {
        _falls.Clear();
        foreach (var f in step.Falls)
            _falls.Add(new Board.FallMove { from = f.From.ToVector(), to = f.To.ToVector() });

        _spawns.Clear();
        foreach (var s in step.Spawns)
            _spawns.Add(new Board.SpawnInfo { cell = s.Cell.ToVector(), animal = context.Catalog.AnimalOf(s.Type), spawnFromY = s.SpawnFromY });

        await context.View.AnimateGravity(_falls, _spawns, new SnapshotAnimalGrid(step.After, context.Catalog), step.Duration);
        context.Listener.OnGoalProgressChanged();
    }
}

public sealed class FeastStepPlayer : TurnStepPlayer<FeastStep>
{
    private const float NudgeSeconds = 0.20f;

    protected override async Task Play(FeastStep step, PresentationContext context)
    {
        context.Listener.OnTilesScored(step.Points, step.Eaten);
        for (int i = 0; i < step.Destroyed.Count; i++)
            context.Listener.OnTilesDestroyed(step.Destroyed[i].Id, step.Destroyed[i].Count);

        foreach (var meal in step.Meals)
        {
            await context.View.AnimateWolfNudge(meal.Predator.ToVector(), meal.Prey.ToVector(), NudgeSeconds);
            context.View.SetCellAnimal(meal.Prey.ToVector(), context.Catalog.AnimalOf(meal.RemainsType));
            context.Listener.OnGoalProgressChanged();
        }
    }
}

public sealed class ShuffleStepPlayer : TurnStepPlayer<ShuffleStep>
{
    protected override async Task Play(ShuffleStep step, PresentationContext context)
    {
        await context.View.ShowShuffleMessage(0.35f);
        await context.View.AnimateShuffle(new SnapshotAnimalGrid(step.After, context.Catalog), 0.07f, 0.09f, 0.008f);
    }
}
