using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// One thing that happened during a turn, in order. Each step carries everything needed to animate it,
    /// so the view never reads the final board to reconstruct an earlier moment.
    /// </summary>
    public abstract class TurnStep { }

    /// <summary>Two tiles swapped places.</summary>
    public sealed class SwapStep : TurnStep
    {
        public SwapStep(GridPos from, GridPos to) { From = from; To = to; }
        public GridPos From { get; }
        public GridPos To { get; }
    }

    /// <summary>A swap made no match, so the tiles went back.</summary>
    public sealed class SwapBackStep : TurnStep
    {
        public SwapBackStep(GridPos from, GridPos to) { From = from; To = to; }
        public GridPos From { get; }
        public GridPos To { get; }
    }

    /// <summary>The swap was accepted and used a move.</summary>
    public sealed class MoveUsedStep : TurnStep
    {
        public MoveUsedStep(bool byMatch) { ByMatch = byMatch; }
        /// <summary>True when the swap was accepted because it made a match (rather than setting off a special).</summary>
        public bool ByMatch { get; }
    }

    /// <summary>Matched tiles pop, just before they're cleared.</summary>
    public sealed class MatchPopStep : TurnStep
    {
        public MatchPopStep(IReadOnlyList<GridPos> cells) { Cells = cells; }
        public IReadOnlyList<GridPos> Cells { get; }
    }

    /// <summary>Tiles were removed and scored.</summary>
    public sealed class ClearStep : TurnStep
    {
        public ClearStep(ClearCause cause, ClearReport report)
        {
            Cause = cause;
            Cleared = report.Cleared;
            Points = report.Points;
            Matched = report.Matched;
            Destroyed = report.Destroyed;
        }

        public ClearCause Cause { get; }
        public IReadOnlyList<GridPos> Cleared { get; }
        public int Points { get; }
        public int Matched { get; }
        public IReadOnlyList<DestroyedCount> Destroyed { get; }
    }

    /// <summary>Bones broke. They're counted as destroyed but give no points.</summary>
    public sealed class BonesBrokenStep : TurnStep
    {
        public BonesBrokenStep(IReadOnlyList<GridPos> cells, string boneId) { Cells = cells; BoneId = boneId; }
        public IReadOnlyList<GridPos> Cells { get; }
        public string BoneId { get; }
    }

    public sealed class BlastWave
    {
        public BlastWave(IReadOnlyList<GridPos> lineCells, IReadOnlyList<GridPos> popCells, IReadOnlyList<GridPos> bonesBroken)
        {
            LineCells = lineCells;
            PopCells = popCells;
            BonesBroken = bonesBroken;
        }

        /// <summary>The cells this wave reaches.</summary>
        public IReadOnlyList<GridPos> LineCells { get; }
        /// <summary>Tiles that visibly pop in this wave (not bones or empty cells).</summary>
        public IReadOnlyList<GridPos> PopCells { get; }
        /// <summary>Bones next to this wave's cells, broken as the wave passes.</summary>
        public IReadOnlyList<GridPos> BonesBroken { get; }
    }

    /// <summary>A black sheep blasted its row or column. A ClearStep for the line follows it.</summary>
    public sealed class BlackSheepBlastStep : TurnStep
    {
        public BlackSheepBlastStep(GridPos center, bool swipedVertically, IReadOnlyList<BlastWave> waves, string boneId)
        {
            Center = center;
            SwipedVertically = swipedVertically;
            Waves = waves;
            BoneId = boneId;
        }

        public GridPos Center { get; }
        /// <summary>A vertical swipe blasts the row; a horizontal swipe blasts the column.</summary>
        public bool SwipedVertically { get; }
        public IReadOnlyList<BlastWave> Waves { get; }
        public string BoneId { get; }
    }

    /// <summary>Tiles fell and new tiles entered.</summary>
    public sealed class GravityStep : TurnStep
    {
        public GravityStep(IReadOnlyList<FallMove> falls, IReadOnlyList<SpawnedTile> spawns, BoardSnapshot after, float duration)
        {
            Falls = falls;
            Spawns = spawns;
            After = after;
            Duration = duration;
        }

        public IReadOnlyList<FallMove> Falls { get; }
        public IReadOnlyList<SpawnedTile> Spawns { get; }
        /// <summary>The board once this step has finished.</summary>
        public BoardSnapshot After { get; }
        /// <summary>Suggested animation length in seconds.</summary>
        public float Duration { get; }
    }

    /// <summary>Wolves ate the sheep next to them; each sheep became bones.</summary>
    public sealed class WolfFeastStep : TurnStep
    {
        public WolfFeastStep(IReadOnlyList<WolfMeal> meals, int points, int eaten, string sheepId, int boneType)
        {
            Meals = meals;
            Points = points;
            Eaten = eaten;
            SheepId = sheepId;
            BoneType = boneType;
        }

        public IReadOnlyList<WolfMeal> Meals { get; }
        public int Points { get; }
        public int Eaten { get; }
        public string SheepId { get; }
        public int BoneType { get; }
    }

    /// <summary>The board had no legal move, so it was shuffled.</summary>
    public sealed class ShuffleStep : TurnStep
    {
        public ShuffleStep(BoardSnapshot after) { After = after; }
        public BoardSnapshot After { get; }
    }
}
