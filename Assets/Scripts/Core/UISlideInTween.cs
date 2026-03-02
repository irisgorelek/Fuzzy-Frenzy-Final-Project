using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class UISlideInTween : MonoBehaviour
{
    public enum FromSide { Left, Right }

    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Slide")]
    [SerializeField] private FromSide from = FromSide.Right;
    [SerializeField] private float distance = 900f;      // tune for your canvas size
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float staggerDelay = 0f;    // set per item for nice cascade
    [SerializeField] private Ease ease = Ease.OutCubic;

    Vector2 _basePos;
    Tween _tween;

    void Awake()
    {
        EnsureRefs();
        _basePos = rect.anchoredPosition;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    void Reset()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void PlayIn()
    {
        EnsureRefs();
        _tween?.Kill();

        _basePos = rect.anchoredPosition;

        float dir = (from == FromSide.Right) ? 1f : -1f;
        rect.anchoredPosition = _basePos + new Vector2(distance * dir, 0f);

        if (canvasGroup != null) canvasGroup.alpha = 0f;

        _tween = DOTween.Sequence()
            .AppendInterval(staggerDelay)
            .Join(rect.DOAnchorPos(_basePos, duration).SetEase(ease))
            .Join(canvasGroup != null ? canvasGroup.DOFade(1f, duration * 0.8f) : null);
    }

    public void PlayOut(bool deactivateOnComplete = false)
    {
        EnsureRefs();
        _tween?.Kill();

        float dir = (from == FromSide.Right) ? 1f : -1f;
        var target = _basePos + new Vector2(distance * dir, 0f);

        _tween = DOTween.Sequence()
            .Join(rect.DOAnchorPos(target, duration * 0.8f).SetEase(Ease.InCubic))
            .Join(canvasGroup != null ? canvasGroup.DOFade(0f, duration * 0.6f) : null)
            .OnComplete(() =>
            {
                if (deactivateOnComplete) gameObject.SetActive(false);
            });
    }

    void EnsureRefs()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); // optional
    }
}