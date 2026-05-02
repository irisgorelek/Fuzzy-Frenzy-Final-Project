using DG.Tweening;
using UnityEngine;

public class UIPulseTween : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float startDelay = 0.3f;
    [SerializeField] private bool playOnEnable = true;

    private Vector3 _originalScale;
    private Tween _tween;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Invoke(nameof(Play), startDelay);
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        Stop();

        _tween = transform.DOScale(_originalScale * scaleMultiplier, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void Stop()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill();

        transform.localScale = _originalScale;
    }
}