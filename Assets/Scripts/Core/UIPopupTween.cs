using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class UIPopupTween : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Show")]
    [SerializeField] private float showDuration = 0.26f;
    [SerializeField] private float showScaleFrom = 0.85f;
    [SerializeField] private Ease showEase = Ease.OutBack;

    [Header("Hide")]
    [SerializeField] private float hideDuration = 0.18f;
    [SerializeField] private float hideScaleTo = 0.85f;
    [SerializeField] private Ease hideEase = Ease.InQuad;

    Tween _tween;

    //private void Start()
    //{
    //    Hide(deactivateOnComplete: false);
    //}

    void Reset()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Show()
    {
        Debug.Log("Show called on " + name);
        EnsureRefs();
        _tween?.Kill();

        gameObject.SetActive(true);

        rect.localScale = Vector3.one * showScaleFrom;
        canvasGroup.alpha = 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        _tween = DOTween.Sequence()
            .Join(rect.DOScale(1f, showDuration).SetEase(showEase))
            .Join(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad))
            .OnComplete(() => canvasGroup.interactable = true);
    }

    public void Hide(bool deactivateOnComplete = true)
    {
        EnsureRefs();
        _tween?.Kill();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        _tween = DOTween.Sequence()
            .Join(rect.DOScale(hideScaleTo, hideDuration).SetEase(hideEase))
            .Join(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                if (deactivateOnComplete) gameObject.SetActive(false);
            });
    }

    void EnsureRefs()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }
}