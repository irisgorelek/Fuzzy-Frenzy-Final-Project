using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class BoardViewImagePool
{
    private readonly RectTransform _overlay;
    private readonly Stack<Image> _tempImagePool = new();
    private readonly Stack<Image> _fxImagePool = new();

    public BoardViewImagePool(RectTransform overlay)
    {
        _overlay = overlay;
    }

    public Image CreateTempImage(CellView source)
    {
        var img = GetPooledImage(_tempImagePool, "SwapTemp");
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

    public Image CreateTempImageFromSprite(Sprite sprite, Color color, CellView sizeReference)
    {
        var img = GetPooledImage(_tempImagePool, "FallTemp");
        var rt = img.rectTransform;

        img.sprite = sprite;
        img.color = color;

        rt.sizeDelta = sizeReference.ImageRect.rect.size;
        rt.localScale = Vector3.one;

        return img;
    }

    public Image CreateFxImage(Sprite sprite, Color color, Vector3 position, Vector2 size, string objectName = "MatchFX")
    {
        var img = GetPooledImage(_fxImagePool, objectName);
        var rt = img.rectTransform;

        img.sprite = sprite;
        img.color = color;

        rt.position = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;

        return img;
    }

    public void ReleaseTempImage(Image img)
    {
        ReleasePooledImage(img, _tempImagePool);
    }

    public void ReleaseFxImage(Image img)
    {
        ReleasePooledImage(img, _fxImagePool);
    }

    private Image GetPooledImage(Stack<Image> pool, string objectName)
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

    private void ReleasePooledImage(Image img, Stack<Image> pool)
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
