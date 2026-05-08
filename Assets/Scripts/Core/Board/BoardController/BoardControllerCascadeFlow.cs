using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class BoardControllerCascadeFlow
{
    private readonly BoardController _controller;

    public BoardControllerCascadeFlow(BoardController controller)
    {
        _controller = controller;
    }

    public async Task ResolveCascadesAsync()
    {
        if (_controller.TryHandleLevelComplete())
            return;

        var matches = _controller.Board.FindMatches();
        while (matches.Count > 0)
        {
            await _controller.View.AnimateMatchPopFx(matches, 0.12f);

            var fallMoves = new List<Board.FallMove>();
            var spawns = new List<Board.SpawnInfo>();

            _controller.Board.ResolveMatches(matches, fallMoves, spawns);

            _controller.UpdateGoalUI();
            await _controller.View.AnimateGravity(fallMoves, spawns, _controller.Board, 0.20f);
            _controller.UpdateGoalUI();

            if (_controller.TryHandleLevelComplete())
                return;

            matches = _controller.Board.FindMatches();

            if (!_controller.IsLevelOver && !_controller.AreAllGoalsComplete())
                await _controller.EnsurePlayableBoardAsync();
        }

        // Only after cascades are fully done:
        _controller.Board.ResolveWolfSheepAfterCascades();

        while (_controller.PendingWolfEats.Count > 0)
        {
            var eat = _controller.PendingWolfEats.Dequeue();

            // 1. Wolf nudges
            await _controller.View.AnimateWolfNudge(eat.wolf, eat.sheep, 0.20f);

            // 2. Only now this sheep turns into bones
            _controller.View.RefreshCellSprite(eat.sheep, _controller.Board);

            _controller.UpdateGoalUI();
        }

        _controller.TryHandleLevelComplete();

        if (!_controller.IsLevelOver)
            await _controller.EnsurePlayableBoardAsync();
    }

    public async Task RemoveCellsFromGridAsync(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
            return;

        var uniqueCells = new HashSet<Vector2Int>(cells);

        var fallMoves = new List<Board.FallMove>();
        var spawns = new List<Board.SpawnInfo>();

        _controller.Board.ClearCells(uniqueCells, fallMoves, spawns);
        _controller.UpdateGoalUI();

        await _controller.View.AnimateGravity(fallMoves, spawns, _controller.Board, 0.1f);
        await ResolveCascadesAsync();

        _controller.HideNormalBubbleIfActive();

        await _controller.TryShowTriggeredDialogueAsync();
    }
}
