using DG.Tweening;
using UnityEngine;

/// <summary>Shared landing motion for existing tiles and newly spawned tiles.</summary>
public static class BoardTileDropAnimation
{
    public static Sequence Create(RectTransform tile, Vector3 destination, Vector3 rebound,
        float fallSeconds, float bounceSeconds)
    {
        return DOTween.Sequence()
            .Append(tile.DOMove(destination, fallSeconds).SetEase(Ease.InQuad))
            .Append(tile.DOMove(destination + rebound, bounceSeconds * .45f).SetEase(Ease.OutQuad))
            .Append(tile.DOMove(destination, bounceSeconds * .55f).SetEase(Ease.InQuad));
    }
}
