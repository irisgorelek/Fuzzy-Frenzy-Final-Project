using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Swapping a black sheep sets off its blast, centred where the sheep ends up.</summary>
    public sealed class BlackSheepSwipeRule : IActionRule
    {
        public void React(TurnRun run, IBoardAction applied)
        {
            if (!(applied is SwapAction swap)) return;

            // After the swap, the tile the player dragged is at To and the other tile is at From.
            GridPos center;
            if (run.Board.IsBlackSheep(swap.To)) center = swap.To;
            else if (run.Board.IsBlackSheep(swap.From)) center = swap.From;
            else return;

            bool swipedVertically = swap.From.X == swap.To.X;
            run.Enqueue(new BlackSheepBlastAction(center, swipedVertically));
        }
    }

    /// <summary>Bones next to a clear break.</summary>
    public sealed class BoneDamageRule : IActionRule
    {
        private readonly List<GridPos> _buffer = new List<GridPos>();

        public void React(TurnRun run, IBoardAction applied)
        {
            if (!(applied is ClearAction clear)) return;

            BoardOperations.FindAdjacentBones(run.Board, clear.DamageSources, _buffer);
            if (_buffer.Count > 0)
                run.Enqueue(new BreakBonesAction(_buffer.ToArray()));
        }
    }

    /// <summary>Once the board settles, each wolf eats a sheep next to it, leaving bones.</summary>
    public sealed class WolfEatsSheepRule : IActionRule
    {
        private readonly List<WolfMeal> _buffer = new List<WolfMeal>();

        public void React(TurnRun run, IBoardAction applied)
        {
            if (!(applied is BoardSettledAction)) return;

            BoardOperations.FindWolfMeals(run.Board, _buffer);
            if (_buffer.Count > 0)
                run.Enqueue(new WolfFeastAction(_buffer.ToArray()));
        }
    }
}
