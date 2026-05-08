using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;

public sealed class BoardViewShuffleAnimator
{
    private readonly BoardViewGrid _grid;
    private readonly Image _shufflePopUp;

    public BoardViewShuffleAnimator(BoardViewGrid grid, Image shufflePopUp)
    {
        _grid = grid;
        _shufflePopUp = shufflePopUp;
    }

    public Task ShowShuffleMessage(float hold = 1.2f)
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
        cg.alpha = 0f;

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

    public Task AnimateShuffle(Board board, float outDuration = 0.08f, float inDuration = 0.1f, float stagger = 0.002f)
    {
        var orderedCells = _grid.GetOrderedCells();
        if (board == null || orderedCells.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

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

            seq.Join(
                rt.DOScale(0f, outDuration)
                  .SetDelay(i * stagger)
                  .SetEase(Ease.InBack)
            );
        }

        // Swap sprites only after old board is fully hidden
        seq.AppendCallback(() =>
        {
            _grid.AssignSprites(board);

            for (int i = 0; i < orderedCells.Count; i++)
            {
                var rt = orderedCells[i].ImageRect;
                rt.localScale = Vector3.zero;
            }
        });

        // Pop new shuffled board back in one by one
        for (int i = 0; i < orderedCells.Count; i++)
        {
            var rt = orderedCells[i].ImageRect;

            seq.Join(
                rt.DOScale(1f, inDuration)
                  .SetDelay(i * stagger)
                  .SetEase(Ease.OutBack)
            );
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
}
