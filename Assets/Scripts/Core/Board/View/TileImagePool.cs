using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pools the free-moving images animations use: copies of tiles that slide or fall (temp images) and
/// rings and sparkles (FX images). Nothing is created per animation once the pools are warm.
/// </summary>
public sealed class TileImagePool
{
    private readonly RectTransform _overlay;
    private readonly Stack<Image> _tempImages = new Stack<Image>();
    private readonly Stack<Image> _fxImages = new Stack<Image>();

    public TileImagePool(RectTransform overlay)
    {
        _overlay = overlay;
    }

    /// <summary>A copy of a cell's tile, placed exactly over it.</summary>
    public Image CreateTempImage(CellView source)
    {
        var img = Get(_tempImages, "SwapTemp");
        var rt = img.rectTransform;

        img.sprite = source.CurrentSprite;
        img.color = source.CurrentColor;

        // Match screen position + size
        rt.position = source.ImageRect.position;
        rt.rotation = source.ImageRect.rotation;
        rt.sizeDelta = source.ImageRect.rect.size;
        rt.localScale = Vector3.one;

        return img;
    }

    /// <summary>A tile image the size of a cell, for a tile that isn't on the board yet.</summary>
    public Image CreateTempImageFromSprite(Sprite sprite, Color color, CellView sizeReference)
    {
        var img = Get(_tempImages, "FallTemp");
        var rt = img.rectTransform;

        img.sprite = sprite;
        img.color = color;

        rt.sizeDelta = sizeReference.ImageRect.rect.size;
        rt.localScale = Vector3.one;

        return img;
    }

    public Image CreateFxImage(Sprite sprite, Color color, Vector3 position, Vector2 size)
    {
        var img = Get(_fxImages, "MatchFX");
        var rt = img.rectTransform;

        img.sprite = sprite;
        img.color = color;

        rt.position = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;

        return img;
    }

    public void ReleaseTemp(Image img) => Release(img, _tempImages);

    public void ReleaseFx(Image img) => Release(img, _fxImages);

    private Image Get(Stack<Image> pool, string objectName)
    {
        Image img;

        if (pool.Count > 0)
        {
            img = pool.Pop();
        }
        else
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_overlay, worldPositionStays: false);
            img = go.GetComponent<Image>();
            img.raycastTarget = false;
        }

        img.gameObject.name = objectName;
        img.transform.SetParent(_overlay, worldPositionStays: false);
        img.gameObject.SetActive(true);

        img.DOKill();
        img.rectTransform.DOKill();

        var rt = img.rectTransform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition3D = Vector3.zero;

        img.sprite = null;
        img.color = Color.white;
        img.raycastTarget = false;

        return img;
    }

    private static void Release(Image img, Stack<Image> pool)
    {
        if (img == null)
            return;

        img.DOKill();
        img.rectTransform.DOKill();

        // Hide first, so resetting transform doesn't become visible for one frame
        img.gameObject.SetActive(false);

        img.sprite = null;
        img.color = Color.white;

        var rt = img.rectTransform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition3D = Vector3.zero;

        pool.Push(img);
    }
}
