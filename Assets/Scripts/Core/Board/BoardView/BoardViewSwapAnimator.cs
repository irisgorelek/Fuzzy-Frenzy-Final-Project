using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class BoardViewSwapAnimator
{
    private readonly BoardViewGrid _grid;
    private readonly BoardViewImagePool _imagePool;

    public BoardViewSwapAnimator(BoardViewGrid grid, BoardViewImagePool imagePool)
    {
        _grid = grid;
        _imagePool = imagePool;
    }

    // Dotween animation
    public Task AnimateSwap(Vector2Int a, Vector2Int b, float duration = 0.18f)
    {
        if (!_grid.TryGetCell(a, out var aView) || !_grid.TryGetCell(b, out var bView))
            return Task.CompletedTask;

        // Create 2 temporary images that can move freely
        Image tempA = _imagePool.CreateTempImage(aView);
        Image tempB = _imagePool.CreateTempImage(bView);

        // Hide the real images during animation
        aView.SetImageEnabled(false);
        bView.SetImageEnabled(false);

        var tcs = new TaskCompletionSource<bool>(); // Create a future task that Ill mark as finished later.

        Sequence seq = DOTween.Sequence(); // Create a sequence of animations
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

            _imagePool.ReleaseTempImage(tempA);
            _imagePool.ReleaseTempImage(tempB);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateWolfNudge(Vector2Int wolfCell, Vector2Int sheepCell, float duration = 0.10f)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFXPitchAdjusted(2, 0.5f); // Play bone sound

        if (!_grid.TryGetCell(wolfCell, out var wolfSource) ||
            !_grid.TryGetCell(sheepCell, out _))
        {
            tcs.SetResult(true);
            return tcs.Task;
        }

        RectTransform wolfRt = wolfSource.ImageRect;

        wolfRt.DOKill();

        Vector2 start = wolfRt.anchoredPosition;

        Vector2 dir = new Vector2(
            Mathf.Clamp(sheepCell.x - wolfCell.x, -1, 1),
            Mathf.Clamp(sheepCell.y - wolfCell.y, -1, 1)
        );

        float pixels = 15f;
        Vector2 target = start + dir * pixels;

        var seq = DOTween.Sequence();
        seq.Append(wolfRt.DOAnchorPos(target, duration * 0.45f).SetEase(Ease.OutQuad));
        seq.Append(wolfRt.DOAnchorPos(start, duration * 0.55f).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            wolfRt.anchoredPosition = start;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateInvalidSwap(Vector2Int a, Vector2Int? b = null, Vector2Int? dir = null, float duration = 0.20f)
    {
        var targets = new List<(RectTransform rt, bool isPrimary)>();

        if (_grid.TryGetCell(a, out var aView))
            targets.Add((aView.ImageRect, true));

        if (b.HasValue && _grid.TryGetCell(b.Value, out var bView))
            targets.Add((bView.ImageRect, false));

        if (targets.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        int completed = 0;

        // Board direction -> UI direction
        // x stays the same
        // y is inverted because your board coordinates go downward
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

    public Task AnimateBlockedTap(Vector2Int cell, float duration = 0.12f)
    {
        if (!_grid.TryGetCell(cell, out var cv))
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
}
