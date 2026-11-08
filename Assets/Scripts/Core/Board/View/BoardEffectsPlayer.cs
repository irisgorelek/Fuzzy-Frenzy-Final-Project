using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Clear and booster effects: match pops, the black sheep's line, bone breaks and the bomb. Uses the particle
/// effects when they are set up and falls back to pooled ring and sparkle images when they are not.
/// </summary>
public sealed class BoardEffectsPlayer
{
    private readonly Dictionary<Vector2Int, CellView> _cells;
    private readonly TileImagePool _pool;
    private readonly BoardFxSettings _fx;
    private readonly object _tweenTarget;

    /// <param name="tweenTarget">Tween target for match pops, so callers can complete or kill them through DOTween.</param>
    public BoardEffectsPlayer(Dictionary<Vector2Int, CellView> cells, TileImagePool pool, BoardFxSettings fx, object tweenTarget)
    {
        _cells = cells;
        _pool = pool;
        _fx = fx;
        _tweenTarget = tweenTarget;
    }

    public Task AnimateMatchPopFx(List<Vector2Int> matches, float duration, BoardClearEffect effect)
    {
        if (matches == null || matches.Count == 0)
        {
            if (effect == BoardClearEffect.BlackSheep)
            {
                var step = new TaskCompletionSource<bool>();
                DOVirtual.DelayedCall(duration, () => step.TrySetResult(true), false)
                    .OnKill(() => step.TrySetResult(true));
                return step.Task;
            }
            return Task.CompletedTask;
        }

        // The sheep's travelling line uses the caller's LineStep timing and original pop.
        if (effect == BoardClearEffect.Match && _fx.ClearAnimation != null && _fx.ClearAnimation.isActiveAndEnabled)
            return _fx.ClearAnimation.Animate(matches, effect);

        var tcs = new TaskCompletionSource<bool>();
        var spawnedFx = new List<Image>();
        var touchedCells = new List<RectTransform>();

        Sequence master = DOTween.Sequence();
        master.SetTarget(_tweenTarget);

        foreach (var cell in matches)
        {
            if (!_cells.TryGetValue(cell, out var cv))
                continue;

            var pieceRt = cv.ImageRect;
            touchedCells.Add(pieceRt);

            pieceRt.DOKill();
            pieceRt.localScale = Vector3.one;

            // Main piece pop
            master.Insert(0f, BuildMatchLikePop(pieceRt, duration));

            bool particleMatch = _fx.Particles != null && _fx.Particles.HasClearEffect(effect);
            if (particleMatch)
                master.InsertCallback(duration * 0.55f, () => _fx.Particles.ClearCell(pieceRt, effect));

            Vector3 center = pieceRt.position;
            Vector2 size = pieceRt.rect.size;

            // White ring
            if (!particleMatch && _fx.MatchRingSprite != null)
            {
                Image ring = _pool.CreateFxImage(_fx.MatchRingSprite, _fx.MatchFxColor, center, size * 0.95f);
                spawnedFx.Add(ring);

                var ringRt = ring.rectTransform;
                ringRt.localScale = Vector3.one * 0.55f;

                master.Join(ringRt.DOScale(1.45f, duration).SetEase(Ease.OutQuad));
                master.Join(ring.DOFade(0f, duration).SetEase(Ease.OutQuad));
            }

            // Sparkles
            if (!particleMatch && _fx.SparkleSprite != null)
            {
                float baseDist = Mathf.Min(size.x, size.y) * 0.28f;

                for (int i = 0; i < _fx.SparklesPerMatch; i++)
                {
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    if (dir.sqrMagnitude < 0.01f)
                        dir = Vector2.up;

                    float dist = baseDist * Random.Range(0.8f, 1.15f);
                    float sparkDur = duration * Random.Range(0.75f, 1.0f);

                    Image spark = _pool.CreateFxImage(_fx.SparkleSprite, _fx.MatchFxColor, center, size * 0.18f);
                    spawnedFx.Add(spark);

                    var sparkRt = spark.rectTransform;
                    sparkRt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                    master.Join(sparkRt.DOMove(center + (Vector3)(dir * dist), sparkDur).SetEase(Ease.OutQuad));
                    master.Join(sparkRt.DOScale(Random.Range(0.35f, 0.7f), sparkDur).SetEase(Ease.InQuad));
                    master.Join(spark.DOFade(0f, sparkDur).SetEase(Ease.OutQuad));
                }
            }
        }

        master.OnComplete(() =>
        {
            foreach (var rt in touchedCells)
                if (rt != null)
                {
                    if (effect == BoardClearEffect.BlackSheep) rt.GetComponent<Image>().enabled = false;
                    rt.localScale = Vector3.one;
                }

            foreach (var fx in spawnedFx)
                _pool.ReleaseFx(fx);

            tcs.TrySetResult(true);
        });

        master.OnKill(() =>
        {
            foreach (var rt in touchedCells)
                if (rt != null)
                    rt.localScale = Vector3.one;

            foreach (var fx in spawnedFx)
                _pool.ReleaseFx(fx);

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateBombImpact(List<Vector2Int> affected, float duration)
    {
        if (affected == null || affected.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        var spawnedFx = new List<Image>();

        if (_fx.Particles != null && _fx.Particles.HasBomb)
        {
            // One centered particle explosion covers the footprint while all tiles pop together.
            Sequence blast = DOTween.Sequence();
            foreach (var coord in affected)
            {
                if (!_cells.TryGetValue(coord, out var cell)) continue;
                var rt = cell.ImageRect;
                rt.DOKill();
                rt.localScale = Vector3.one;
                var pop = DOTween.Sequence();
                pop.Append(rt.DOScale(1.08f, duration * 0.2f).SetEase(Ease.OutQuad));
                pop.AppendInterval(duration * 0.18f);
                pop.Append(rt.DOScale(0f, duration * 0.62f).SetEase(Ease.InBack));
                blast.Join(pop);
            }
            blast.OnComplete(() => tcs.TrySetResult(true));
            blast.OnKill(() =>
            {
                foreach (var coord in affected)
                    if (_cells.TryGetValue(coord, out var cell)) cell.ImageRect.localScale = Vector3.one;
                tcs.TrySetResult(true);
            });
            return tcs.Task;
        }

        Sprite ringSprite = _fx.BombRingSprite != null ? _fx.BombRingSprite : _fx.MatchRingSprite;
        if (ringSprite == null)
            return Task.CompletedTask;

        Sequence master = DOTween.Sequence();

        foreach (var coord in affected)
        {
            if (!_cells.TryGetValue(coord, out var cv))
                continue;

            RectTransform pieceRt = cv.ImageRect;
            pieceRt.DOKill();
            pieceRt.localScale = Vector3.one;

            Vector3 center = pieceRt.position;
            Vector2 size = pieceRt.rect.size;

            Image ring = _pool.CreateFxImage(ringSprite, _fx.BombRingColor, center, size * 1.08f);
            spawnedFx.Add(ring);

            var ringRt = ring.rectTransform;
            ringRt.localScale = Vector3.one * 0.72f;

            Color startColor = _fx.BombRingColor;
            startColor.a = 0f;
            ring.color = startColor;

            Sequence one = DOTween.Sequence();

            // Ring appears
            one.Append(ring.DOFade(_fx.BombRingColor.a, duration * 0.22f).SetEase(Ease.OutQuad));
            one.Join(ringRt.DOScale(1.08f, duration * 0.22f).SetEase(Ease.OutQuad));

            // Short hold so the player reads the impacted cells
            one.AppendInterval(duration * 0.18f);

            // Ring fade + exact match-like pop at the END
            one.Append(ring.DOFade(0f, duration * 0.30f).SetEase(Ease.InQuad));
            one.Join(ringRt.DOScale(1.22f, duration * 0.30f).SetEase(Ease.OutQuad));

            // Same feel as match pop, but ends at zero so it doesn't stick half-shrunk
            one.Join(pieceRt.DOScale(1.12f, 0.12f * 0.28f).SetEase(Ease.OutQuad));
            one.Append(pieceRt.DOScale(0f, 0.12f * 0.42f).SetEase(Ease.InBack));

            master.Join(one);
        }

        master.OnComplete(() =>
        {
            foreach (var fx in spawnedFx)
                _pool.ReleaseFx(fx);

            tcs.TrySetResult(true);
        });

        master.OnKill(() =>
        {
            foreach (var kvp in _cells)
            {
                if (kvp.Value != null && kvp.Value.ImageRect != null)
                    kvp.Value.ImageRect.localScale = Vector3.one;
            }

            foreach (var fx in spawnedFx)
                _pool.ReleaseFx(fx);

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateBombWarning(Vector2Int coord, float totalDuration)
    {
        if (!_cells.TryGetValue(coord, out var cv))
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        RectTransform rt = cv.ImageRect;
        Image img = cv.CellImage;

        Color originalColor = img.color;
        Color warmColor = new Color(1f, 0.82f, 0.35f, 1f);
        Color dangerColor = new Color(1f, 0.35f, 0.35f, 1f);

        rt.DOKill();
        img.DOKill();

        rt.localScale = Vector3.one;
        img.color = originalColor;

        int pulses = 3;
        float halfStep = totalDuration / (pulses * 2f);

        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < pulses; i++)
        {
            float t = (i + 1f) / pulses;
            float scale = Mathf.Lerp(1.03f, 1.16f, t);
            Color flashColor = Color.Lerp(warmColor, dangerColor, t);

            seq.Append(rt.DOScale(scale, halfStep).SetEase(Ease.OutQuad));
            seq.Join(img.DOColor(flashColor, halfStep));

            seq.Append(rt.DOScale(1f, halfStep).SetEase(Ease.InQuad));
            seq.Join(img.DOColor(originalColor, halfStep));
        }

        seq.OnComplete(() =>
        {
            rt.localScale = Vector3.one;
            img.color = originalColor;
            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            rt.localScale = Vector3.one;
            img.color = originalColor;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public void PlayTileBreakFx(Vector2Int coord)
    {
        if (!_cells.TryGetValue(coord, out var cell)) return;
        if (_fx.Particles != null) _fx.Particles.BoneBreak(cell.ImageRect);
        cell.SetImageEnabled(false);
    }

    public Task AnimateLineBlastCharge(Vector2Int coord)
    {
        if (_fx.Particles == null || !_cells.TryGetValue(coord, out var cell)) return Task.CompletedTask;
        var bounds = new List<RectTransform>(_cells.Count);
        foreach (var entry in _cells.Values) bounds.Add(entry.ImageRect);
        if (!_fx.Particles.SheepAnger(cell.ImageRect, bounds)) return Task.CompletedTask;
        var tcs = new TaskCompletionSource<bool>();
        DOVirtual.DelayedCall(BoardEffectsTiming.SheepAnticipation, () => tcs.TrySetResult(true), false)
            .OnKill(() => tcs.TrySetResult(true));
        return tcs.Task;
    }

    public Task WaitForLineBlastFx() =>
        _fx.Particles != null ? _fx.Particles.WaitForLineBlastFx() : Task.CompletedTask;

    public void PlayLineBlastFx(Vector2Int coord, bool horizontal, int width, int height)
    {
        if (_fx.Particles == null || !_cells.TryGetValue(coord, out var cell)) return;
        float step = 1f;
        var neighborCoord = coord + (horizontal ? Vector2Int.right : Vector2Int.down);
        if (!_cells.TryGetValue(neighborCoord, out var neighbor))
            _cells.TryGetValue(coord - (horizontal ? Vector2Int.right : Vector2Int.down), out neighbor);
        if (neighbor != null)
        {
            Vector3 delta = cell.ImageRect.InverseTransformVector(neighbor.ImageRect.position - cell.ImageRect.position);
            step = delta.magnitude / Mathf.Max(1f, cell.ImageRect.rect.width);
        }
        _fx.Particles.BlackSheep(cell.ImageRect, horizontal,
            (horizontal ? coord.x : height - 1 - coord.y) * step,
            (horizontal ? width - 1 - coord.x : coord.y) * step);
    }

    public bool PlayBombBurstFx(Vector2Int coord)
    {
        if (_fx.Particles == null || !_fx.Particles.HasBomb || !_cells.TryGetValue(coord, out var cell))
            return false;
        float span = 3f;
        if (!_cells.TryGetValue(coord + Vector2Int.right, out var neighbor))
            _cells.TryGetValue(coord + Vector2Int.left, out neighbor);
        if (neighbor != null)
        {
            var delta = cell.ImageRect.InverseTransformVector(neighbor.ImageRect.position - cell.ImageRect.position);
            span = 1f + 2f * delta.magnitude / Mathf.Max(1f, cell.ImageRect.rect.width);
        }
        _fx.Particles.Bomb(cell.ImageRect, span);
        return true;
    }

    private static Tween BuildMatchLikePop(RectTransform pieceRt, float duration)
    {
        pieceRt.DOKill();
        pieceRt.localScale = Vector3.one;

        Sequence pop = DOTween.Sequence();
        pop.Append(pieceRt.DOScale(1.12f, duration * 0.28f).SetEase(Ease.OutQuad));
        pop.Append(pieceRt.DOScale(0f, duration * 0.72f).SetEase(Ease.InBack));

        return pop;
    }
}
