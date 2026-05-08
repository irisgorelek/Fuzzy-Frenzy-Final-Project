using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;

public sealed class BoardViewMatchFxAnimator
{
    private readonly BoardViewGrid _grid;
    private readonly BoardViewImagePool _imagePool;
    private readonly Sprite _matchRingSprite;
    private readonly Sprite _sparkleSprite;
    private readonly Color _matchFxColor;
    private readonly int _sparklesPerMatch;

    public BoardViewMatchFxAnimator(
        BoardViewGrid grid,
        BoardViewImagePool imagePool,
        Sprite matchRingSprite,
        Sprite sparkleSprite,
        Color matchFxColor,
        int sparklesPerMatch)
    {
        _grid = grid;
        _imagePool = imagePool;
        _matchRingSprite = matchRingSprite;
        _sparkleSprite = sparkleSprite;
        _matchFxColor = matchFxColor;
        _sparklesPerMatch = sparklesPerMatch;
    }

    public Task AnimateHint(Vector2Int a, Vector2Int b, float duration = 0.2f)
    {
        if (!_grid.TryGetCell(a, out var aCell) || !_grid.TryGetCell(b, out var bCell))
            return Task.CompletedTask;

        var aRt = aCell.ImageRect;
        var bRt = bCell.ImageRect;

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

    public Task AnimateMatchPopFx(List<Vector2Int> matches, float duration = 0.12f)
    {
        if (matches == null || matches.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        var spawnedFx = new List<Image>();
        var touchedCells = new List<RectTransform>();

        Sequence master = DOTween.Sequence();

        foreach (var cell in matches)
        {
            if (!_grid.TryGetCell(cell, out var cv))
                continue;

            var pieceRt = cv.ImageRect;
            touchedCells.Add(pieceRt);

            pieceRt.DOKill();
            pieceRt.localScale = Vector3.one;

            // Main piece pop
            master.Join(BuildMatchLikePop(pieceRt, duration));

            Vector3 center = pieceRt.position;
            Vector2 size = pieceRt.rect.size;

            // White ring
            if (_matchRingSprite != null)
            {
                Image ring = _imagePool.CreateFxImage(_matchRingSprite, _matchFxColor, center, size * 0.95f);
                spawnedFx.Add(ring);

                var ringRt = ring.rectTransform;
                ringRt.localScale = Vector3.one * 0.55f;

                master.Join(ringRt.DOScale(1.45f, duration).SetEase(Ease.OutQuad));
                master.Join(ring.DOFade(0f, duration).SetEase(Ease.OutQuad));
            }

            // Sparkles
            if (_sparkleSprite != null)
            {
                float baseDist = Mathf.Min(size.x, size.y) * 0.28f;

                for (int i = 0; i < _sparklesPerMatch; i++)
                {
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    if (dir.sqrMagnitude < 0.01f)
                        dir = Vector2.up;

                    float dist = baseDist * Random.Range(0.8f, 1.15f);
                    float sparkDur = duration * Random.Range(0.75f, 1.0f);

                    Image spark = _imagePool.CreateFxImage(_sparkleSprite, _matchFxColor, center, size * 0.18f);
                    spawnedFx.Add(spark);

                    var sparkRt = spark.rectTransform;
                    sparkRt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                    master.Join(
                        sparkRt.DOMove(center + (Vector3)(dir * dist), sparkDur)
                               .SetEase(Ease.OutQuad)
                    );

                    master.Join(
                        sparkRt.DOScale(Random.Range(0.35f, 0.7f), sparkDur)
                               .SetEase(Ease.InQuad)
                    );

                    master.Join(
                        spark.DOFade(0f, sparkDur)
                             .SetEase(Ease.OutQuad)
                    );
                }
            }
        }

        master.OnComplete(() =>
        {
            foreach (var rt in touchedCells)
                if (rt != null)
                    rt.localScale = Vector3.one;

            foreach (var fx in spawnedFx)
                _imagePool.ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        master.OnKill(() =>
        {
            foreach (var rt in touchedCells)
                if (rt != null)
                    rt.localScale = Vector3.one;

            foreach (var fx in spawnedFx)
                _imagePool.ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    private Tween BuildMatchLikePop(RectTransform pieceRt, float duration = 0.12f)
    {
        pieceRt.DOKill();
        pieceRt.localScale = Vector3.one;

        Sequence pop = DOTween.Sequence();
        pop.Append(pieceRt.DOScale(1.12f, duration * 0.28f).SetEase(Ease.OutQuad));
        pop.Append(pieceRt.DOScale(0.82f, duration * 0.42f).SetEase(Ease.InBack));

        return pop;
    }
}
