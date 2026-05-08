using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class BoardViewBombAnimator
{
    private readonly BoardViewGrid _grid;
    private readonly BoardViewImagePool _imagePool;
    private readonly Sprite _bombRingSprite;
    private readonly Sprite _fallbackRingSprite;
    private readonly Color _bombRingColor;

    public BoardViewBombAnimator(
        BoardViewGrid grid,
        BoardViewImagePool imagePool,
        Sprite bombRingSprite,
        Sprite fallbackRingSprite,
        Color bombRingColor)
    {
        _grid = grid;
        _imagePool = imagePool;
        _bombRingSprite = bombRingSprite;
        _fallbackRingSprite = fallbackRingSprite;
        _bombRingColor = bombRingColor;
    }

    public Task AnimateBombImpact(List<Vector2Int> affected, float duration = 0.28f)
    {
        if (affected == null || affected.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        var spawnedFx = new List<Image>();

        Sprite ringSprite = _bombRingSprite != null ? _bombRingSprite : _fallbackRingSprite;
        if (ringSprite == null)
            return Task.CompletedTask;

        Sequence master = DOTween.Sequence();

        foreach (var coord in affected)
        {
            if (!_grid.TryGetCell(coord, out var cv))
                continue;

            RectTransform pieceRt = cv.ImageRect;
            pieceRt.DOKill();
            pieceRt.localScale = Vector3.one;

            Vector3 center = pieceRt.position;
            Vector2 size = pieceRt.rect.size;

            Image ring = _imagePool.CreateFxImage(ringSprite, _bombRingColor, center, size * 1.08f);
            spawnedFx.Add(ring);

            var ringRt = ring.rectTransform;
            ringRt.localScale = Vector3.one * 0.72f;

            Color startColor = _bombRingColor;
            startColor.a = 0f;
            ring.color = startColor;

            Sequence one = DOTween.Sequence();

            // Ring appears
            one.Append(ring.DOFade(_bombRingColor.a, duration * 0.22f).SetEase(Ease.OutQuad));
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
                _imagePool.ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        master.OnKill(() =>
        {
            foreach (var cell in _grid.Cells.Values)
            {
                if (cell != null && cell.ImageRect != null)
                    cell.ImageRect.localScale = Vector3.one;
            }

            foreach (var fx in spawnedFx)
                _imagePool.ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateBombWarning(Vector2Int coord, float totalDuration = 1.5f)
    {
        if (!_grid.TryGetCell(coord, out var cv))
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

        int pulses = 6;
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
}
