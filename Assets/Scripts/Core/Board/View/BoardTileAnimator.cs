using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using FuzzyFrenzy.Domain;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Moves tiles: swaps, gravity and refill, shuffles, hints and the small feedback nudges.
/// Every animation returns a task that completes when the tiles are back in their resting state.
/// </summary>
public sealed class BoardTileAnimator
{
    private readonly Dictionary<Vector2Int, CellView> _cells;
    private readonly TileImagePool _pool;
    private readonly TileMotionSettings _motion;
    private readonly Image _shufflePopUp;
    private readonly Action<IAnimalGrid> _redraw;

    public BoardTileAnimator(Dictionary<Vector2Int, CellView> cells, TileImagePool pool, TileMotionSettings motion,
        Image shufflePopUp, Action<IAnimalGrid> redraw)
    {
        _cells = cells;
        _pool = pool;
        _motion = motion;
        _shufflePopUp = shufflePopUp;
        _redraw = redraw;
    }

    public Task AnimateSwap(Vector2Int a, Vector2Int b, float duration)
    {
        if (!_cells.ContainsKey(a) || !_cells.ContainsKey(b))
            return Task.CompletedTask;

        var aView = _cells[a];
        var bView = _cells[b];

        // Create 2 temporary images that can move freely
        Image tempA = _pool.CreateTempImage(aView);
        Image tempB = _pool.CreateTempImage(bView);

        // Hide the real images during animation
        aView.SetImageEnabled(false);
        bView.SetImageEnabled(false);

        var tcs = new TaskCompletionSource<bool>();

        Sequence seq = DOTween.Sequence();
        seq.Join(tempA.rectTransform.DOMove(bView.ImageRect.position, duration).SetEase(Ease.InOutQuad));
        seq.Join(tempB.rectTransform.DOMove(aView.ImageRect.position, duration).SetEase(Ease.InOutQuad));

        seq.OnComplete(() =>
        {
            // Swap the real sprites at the end
            var aSprite = aView.CurrentSprite;
            var aColor = aView.CurrentColor;

            aView.SetSprite(bView.CurrentSprite, bView.CurrentColor);
            bView.SetSprite(aSprite, aColor);

            aView.SetImageEnabled(true);
            bView.SetImageEnabled(true);

            _pool.ReleaseTemp(tempA);
            _pool.ReleaseTemp(tempB);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateGravity(IReadOnlyList<FallMove> moves, IReadOnlyList<SpawnedTile> spawns, IAnimalGrid board, float duration)
    {
        bool hasMoves = moves != null && moves.Count > 0;
        bool hasSpawns = spawns != null && spawns.Count > 0;

        if (!hasMoves && !hasSpawns)
        {
            _redraw(board);
            return Task.CompletedTask;
        }

        // Hide all involved cells so we only see the moving temp images
        var involved = new HashSet<Vector2Int>();
        if (hasMoves)
        {
            foreach (var m in moves) { involved.Add(m.From.ToVector()); involved.Add(m.To.ToVector()); }
        }
        if (hasSpawns)
        {
            foreach (var s in spawns) involved.Add(s.Cell.ToVector());
        }

        foreach (var c in involved)
            if (_cells.TryGetValue(c, out var cv))
                cv.SetImageEnabled(false);

        var temps = new List<Image>();
        var tcs = new TaskCompletionSource<bool>();

        Sequence seq = DOTween.Sequence();

        // Falling moves
        if (hasMoves)
        {
            foreach (var m in moves)
            {
                if (!_cells.TryGetValue(m.From.ToVector(), out var fromView) || !_cells.TryGetValue(m.To.ToVector(), out var toView)) continue;

                var temp = _pool.CreateTempImage(fromView);
                temps.Add(temp);

                seq.Insert(0, BuildTileDrop(temp.rectTransform, toView, duration));
            }
        }

        // Spawns
        if (hasSpawns)
        {
            // Feed tiles through one entry below the header, deepest destination first.
            var spawnOrder = new Dictionary<(int column, int blocker), int>();
            var orderedSpawns = new List<SpawnedTile>(spawns);
            orderedSpawns.Sort((a, b) => b.Cell.Y.CompareTo(a.Cell.Y));
            foreach (var spawned in orderedSpawns)
            {
                var cell = spawned.Cell.ToVector();
                // The landed board shows what entered each cell.
                var animal = board.GetAnimalFromCell(cell);
                if (!_cells.TryGetValue(cell, out var targetView) || animal == null) continue;

                int spawnFromY = spawned.SpawnFromY;
                int entryY = (spawnFromY >= 0) ? (spawnFromY + 1) : 0;

                var temp = _pool.CreateTempImageFromSprite(animal._sprite, animal.color, targetView);
                temps.Add(temp);

                var entryView = _cells[new Vector2Int(cell.x, entryY)];
                var key = (cell.x, spawnFromY);
                spawnOrder.TryGetValue(key, out int order);
                spawnOrder[key] = order + 1;
                Vector3 step = GravityStep(cell, targetView);
                // Blockers split columns into separate refill segments; never fly through a bone.
                float entryOffset = spawnFromY < 0 ? _motion.SpawnEntryHeight : .42f;
                temp.rectTransform.position = entryView.ImageRect.position + step * entryOffset;
                var spawnColor = temp.color;
                float targetAlpha = spawnColor.a;
                spawnColor.a = 0;
                temp.color = spawnColor;
                float delay = order * _motion.SpawnStaggerSeconds;
                float fallSeconds = Mathf.Max(duration, _motion.SpawnFallSeconds) + (cell.y - entryY) * .035f;
                seq.Insert(delay, temp.DOFade(targetAlpha, Mathf.Min(_motion.SpawnFadeSeconds, fallSeconds)).SetEase(Ease.InOutSine));
                seq.Insert(delay, BuildTileDrop(temp.rectTransform, targetView, fallSeconds));
            }
        }

        seq.OnComplete(() =>
        {
            // Redraw final state
            _redraw(board);

            // Re-enable real cell images
            foreach (var c in involved)
                if (_cells.TryGetValue(c, out var cv))
                    cv.SetImageEnabled(true);

            for (int i = 0; i < temps.Count; i++)
                _pool.ReleaseTemp(temps[i]);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimatePredatorNudge(Vector2Int predatorCell, Vector2Int preyCell, float duration)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (!_cells.TryGetValue(predatorCell, out var predatorSource) ||
            !_cells.TryGetValue(preyCell, out _))
        {
            tcs.SetResult(true);
            return tcs.Task;
        }

        RectTransform predatorRt = predatorSource.ImageRect;

        predatorRt.DOKill();

        Vector2 start = predatorRt.anchoredPosition;

        Vector2 dir = new Vector2(
            Mathf.Clamp(preyCell.x - predatorCell.x, -1, 1),
            Mathf.Clamp(preyCell.y - predatorCell.y, -1, 1)
        );

        float pixels = 15f;
        Vector2 target = start + dir * pixels;

        var seq = DOTween.Sequence();
        seq.Append(predatorRt.DOAnchorPos(target, duration * 0.45f).SetEase(Ease.OutQuad));
        seq.Append(predatorRt.DOAnchorPos(start, duration * 0.55f).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            predatorRt.anchoredPosition = start;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateHint(Vector2Int a, Vector2Int b, float duration)
    {
        if (!_cells.ContainsKey(a) || !_cells.ContainsKey(b))
            return Task.CompletedTask;

        var aRt = _cells[a].ImageRect;
        var bRt = _cells[b].ImageRect;

        aRt.DOKill();
        bRt.DOKill();

        aRt.localScale = Vector3.one;
        bRt.localScale = Vector3.one;

        var tcs = new TaskCompletionSource<bool>();

        Sequence seq = DOTween.Sequence();
        seq.Join(aRt.DOScale(1.15f, duration).SetLoops(4, LoopType.Yoyo));
        seq.Join(bRt.DOScale(1.15f, duration).SetLoops(4, LoopType.Yoyo));

        seq.OnComplete(() =>
        {
            aRt.localScale = Vector3.one;
            bRt.localScale = Vector3.one;
            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            aRt.localScale = Vector3.one;
            bRt.localScale = Vector3.one;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task ShowShuffleMessage(float hold)
    {
        if (_shufflePopUp == null)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        _shufflePopUp.DOKill();
        _shufflePopUp.rectTransform.DOKill();

        _shufflePopUp.gameObject.SetActive(true);

        var rt = _shufflePopUp.rectTransform;

        var cg = _shufflePopUp.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = _shufflePopUp.gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.one * 0.75f;
        cg.alpha = 100f;

        DOTween.Sequence()
            .Append(rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack))
            .Join(cg.DOFade(1f, 0.12f))
            .AppendInterval(hold)
            .Append(rt.DOScale(0.9f, 0.16f).SetEase(Ease.InBack))
            .Join(cg.DOFade(0f, 0.16f))
            .OnComplete(() =>
            {
                _shufflePopUp.gameObject.SetActive(false);
                tcs.TrySetResult(true);
            });

        return tcs.Task;
    }

    public Task AnimateShuffle(IAnimalGrid board, float outDuration, float inDuration, float stagger)
    {
        if (board == null || _cells.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        var orderedCells = new List<CellView>(_cells.Values);

        // Randomize order so the board doesn't disappear row-by-row every time
        for (int i = orderedCells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (orderedCells[i], orderedCells[j]) = (orderedCells[j], orderedCells[i]);
        }

        Sequence seq = DOTween.Sequence();

        // OUT: shrink old animals away one by one
        for (int i = 0; i < orderedCells.Count; i++)
        {
            var rt = orderedCells[i].ImageRect;
            rt.DOKill();
            rt.localScale = Vector3.one;

            seq.Join(rt.DOScale(0f, outDuration).SetDelay(i * stagger).SetEase(Ease.InBack));
        }

        // Swap sprites only after old board is fully hidden
        seq.AppendCallback(() =>
        {
            _redraw(board);

            for (int i = 0; i < orderedCells.Count; i++)
                orderedCells[i].ImageRect.localScale = Vector3.zero;
        });

        // Pop new shuffled board back in one by one
        for (int i = 0; i < orderedCells.Count; i++)
        {
            var rt = orderedCells[i].ImageRect;
            seq.Join(rt.DOScale(1f, inDuration).SetDelay(i * stagger).SetEase(Ease.OutBack));
        }

        seq.OnComplete(() =>
        {
            for (int i = 0; i < orderedCells.Count; i++)
                orderedCells[i].ImageRect.localScale = Vector3.one;

            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            for (int i = 0; i < orderedCells.Count; i++)
                orderedCells[i].ImageRect.localScale = Vector3.one;

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateInvalidSwap(Vector2Int a, Vector2Int? b, Vector2Int? dir, float duration)
    {
        var targets = new List<(RectTransform rt, bool isPrimary)>();

        if (_cells.TryGetValue(a, out var aView))
            targets.Add((aView.ImageRect, true));

        if (b.HasValue && _cells.TryGetValue(b.Value, out var bView))
            targets.Add((bView.ImageRect, false));

        if (targets.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        int completed = 0;

        // Board direction -> UI direction: x stays the same, y is inverted because board rows go downward.
        Vector2 primaryOffset = dir.HasValue
            ? new Vector2(dir.Value.x, -dir.Value.y) * 16f
            : new Vector2(18f, 0f); // fallback if no direction known

        Vector2 secondaryOffset = -primaryOffset * 0.65f;

        foreach (var target in targets)
        {
            var rt = target.rt;
            rt.DOKill();

            Vector2 startPos = rt.anchoredPosition;
            Vector3 startScale = rt.localScale;

            Vector2 moveOffset = target.isPrimary ? primaryOffset : secondaryOffset;

            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOAnchorPos(startPos + moveOffset, duration * 0.28f).SetEase(Ease.OutQuad));
            seq.Join(rt.DOScale(startScale * 1.04f, duration * 0.20f).SetEase(Ease.OutQuad));

            seq.Append(rt.DOAnchorPos(startPos, duration * 0.72f).SetEase(Ease.OutBack));
            seq.Join(rt.DOScale(startScale, duration * 0.60f).SetEase(Ease.OutQuad));

            seq.OnComplete(() =>
            {
                rt.anchoredPosition = startPos;
                rt.localScale = startScale;

                completed++;
                if (completed >= targets.Count)
                    tcs.TrySetResult(true);
            });

            seq.OnKill(() =>
            {
                rt.anchoredPosition = startPos;
                rt.localScale = startScale;

                completed++;
                if (completed >= targets.Count)
                    tcs.TrySetResult(true);
            });
        }

        return tcs.Task;
    }

    public Task AnimateBlockedTap(Vector2Int cell, float duration)
    {
        if (!_cells.TryGetValue(cell, out var cv))
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        RectTransform rt = cv.ImageRect;
        Image img = cv.CellImage;

        rt.DOKill();
        img.DOKill();

        Vector3 startScale = rt.localScale;
        Color startColor = img.color;
        Color flashColor = Color.Lerp(startColor, new Color(1f, 0.8f, 0.8f, 1f), 0.35f);

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOScale(startScale * 0.94f, duration * 0.35f).SetEase(Ease.OutQuad));
        seq.Join(img.DOColor(flashColor, duration * 0.35f));
        seq.Append(rt.DOScale(startScale, duration * 0.65f).SetEase(Ease.OutBack));
        seq.Join(img.DOColor(startColor, duration * 0.65f));

        seq.OnComplete(() =>
        {
            rt.localScale = startScale;
            img.color = startColor;
            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            rt.localScale = startScale;
            img.color = startColor;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    private Vector3 GravityStep(Vector2Int cell, CellView view)
    {
        if (_cells.TryGetValue(cell + Vector2Int.down, out var above))
            return above.ImageRect.position - view.ImageRect.position;
        if (_cells.TryGetValue(cell + Vector2Int.up, out var below))
            return view.ImageRect.position - below.ImageRect.position;
        return view.ImageRect.TransformVector(Vector3.up * view.ImageRect.rect.height);
    }

    private Sequence BuildTileDrop(RectTransform tile, CellView target, float duration)
    {
        var rebound = target.ImageRect.TransformVector(Vector3.up * target.ImageRect.rect.height * _motion.LandingBounceHeight);
        return BoardTileDropAnimation.Create(tile, target.ImageRect.position, rebound, duration, _motion.LandingBounceSeconds);
    }
}
