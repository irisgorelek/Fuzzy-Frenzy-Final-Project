using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class BoardViewGravityAnimator
{
    private readonly BoardViewGrid _grid;
    private readonly BoardViewImagePool _imagePool;

    public BoardViewGravityAnimator(BoardViewGrid grid, BoardViewImagePool imagePool)
    {
        _grid = grid;
        _imagePool = imagePool;
    }

    // Animate the gravity 
    public Task AnimateGravity(List<Board.FallMove> moves, List<Board.SpawnInfo> spawns, Board board, float duration = 0.20f)
    {
        bool hasMoves = moves != null && moves.Count > 0;
        bool hasSpawns = spawns != null && spawns.Count > 0;

        if (!hasMoves && !hasSpawns)
        {
            _grid.AssignSprites(board);
            return Task.CompletedTask;
        }

        // Hide all involved cells so we only see the moving temp images
        var involved = new HashSet<Vector2Int>();
        if (hasMoves)
        {
            foreach (var m in moves) { involved.Add(m.from); involved.Add(m.to); }
        }
        if (hasSpawns)
        {
            foreach (var s in spawns) involved.Add(s.cell);
        }

        foreach (var c in involved)
            if (_grid.TryGetCell(c, out var cv))
                cv.SetImageEnabled(false);

        var temps = new List<Image>();
        var tcs = new TaskCompletionSource<bool>();

        Sequence seq = DOTween.Sequence();

        // Falling moves
        if (hasMoves)
        {
            foreach (var m in moves)
            {
                if (!_grid.TryGetCell(m.from, out var fromView) || !_grid.TryGetCell(m.to, out var toView)) continue;

                var temp = _imagePool.CreateTempImage(fromView);
                temps.Add(temp);

                seq.Join(temp.rectTransform
                    .DOMove(toView.ImageRect.position, duration)
                    .SetEase(Ease.InQuad));
            }
        }

        // Spawns
        if (hasSpawns)
        {
            foreach (var s in spawns)
            {
                if (!_grid.TryGetCell(s.cell, out var targetView) || s.animal == null) continue;

                var targetPos = targetView.ImageRect.position;

                int entryY = (s.spawnFromY >= 0) ? (s.spawnFromY + 1) : 0;

                // Create temp image
                var temp = _imagePool.CreateTempImageFromSprite(s.animal._sprite, s.animal.color, targetView);
                temps.Add(temp);

                float cellH = targetView.ImageRect.rect.height;
                float upOffset = cellH * 1.2f;

                if (s.spawnFromY < 0)
                {
                    // Normal: spawn from above board in same column
                    if (!_grid.TryGetCell(new Vector2Int(s.cell.x, 0), out var startView))
                        continue;

                    temp.rectTransform.position = startView.ImageRect.position + Vector3.up * upOffset;

                    seq.Join(temp.rectTransform
                        .DOMove(targetPos, duration)
                        .SetEase(Ease.InQuad));
                }
                else
                {
                    // Better gravity feel for pieces coming from under a blocker:
                    // start slightly ABOVE the entry cell and let them accelerate down.
                    // No fade-in, because fade looks like "appearing", not falling.

                    var entryCell = new Vector2Int(s.cell.x, entryY);
                    if (!_grid.TryGetCell(entryCell, out var entryView))
                        continue;

                    float startAbove = cellH * 0.42f;

                    temp.rectTransform.position = entryView.ImageRect.position + Vector3.up * startAbove;
                    temp.rectTransform.localScale = new Vector3(0.94f, 1.08f, 1f);
                    temp.color = Color.white;

                    Sequence sseq = DOTween.Sequence();

                    sseq.Append(
                        temp.rectTransform
                            .DOMove(targetPos, duration)
                            .SetEase(Ease.InQuad)
                    );

                    sseq.Join(
                        temp.rectTransform
                            .DOScale(Vector3.one, duration * 0.35f)
                            .SetEase(Ease.OutQuad)
                    );

                    seq.Join(sseq);
                }
            }
        }

        seq.OnComplete(() =>
        {
            // Redraw final state
            _grid.AssignSprites(board);

            // Re-enable real cell images
            foreach (var c in involved)
                if (_grid.TryGetCell(c, out var cv))
                    cv.SetImageEnabled(true);

            // Cleanup temps
            for (int i = 0; i < temps.Count; i++)
                _imagePool.ReleaseTempImage(temps[i]);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }
}
