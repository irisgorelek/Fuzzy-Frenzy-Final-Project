using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIHeartbeatTween : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;

    [Header("Autoplay")]
    [SerializeField] private bool playOnEnable = true;

    [Header("Heartbeat")]
    [SerializeField] private float firstBeatScale = 1.10f;
    [SerializeField] private float secondBeatScale = 1.06f;
    [SerializeField] private float firstBeatDuration = 0.08f;
    [SerializeField] private float firstBeatReturn = 0.10f;
    [SerializeField] private float gapBetweenBeats = 0.08f;
    [SerializeField] private float secondBeatDuration = 0.07f;
    [SerializeField] private float secondBeatReturn = 0.10f;
    [SerializeField] private float restBetweenLoops = 0.60f;

    [Header("Juice / Side Shake")]
    [SerializeField] private bool sideShake = true;
    [SerializeField] private float sideShakeDuration = 0.14f;
    [SerializeField] private float sideShakeAmount = 6f;
    [SerializeField] private int sideShakeVibrato = 12;
    [SerializeField] private float sideShakeElasticity = 0.85f;

    [Header("Ease")]
    [SerializeField] private Ease beatOutEase = Ease.OutQuad;
    [SerializeField] private Ease beatInEase = Ease.InQuad;

    private Vector3 _baseScale;
    private Vector2 _baseAnchoredPos;
    private Sequence _loop;

    void Reset()
    {
        rect = GetComponent<RectTransform>();
    }

    void Awake()
    {
        EnsureRefs();
        _baseScale = rect.localScale;
        _baseAnchoredPos = rect.anchoredPosition;
    }

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    void OnDisable()
    {
        StopAndReset();
    }

    public void Play()
    {
        EnsureRefs();
        StopAndReset();

        _baseScale = rect.localScale;
        _baseAnchoredPos = rect.anchoredPosition;

        _loop = DOTween.Sequence()
            // First beat
            .Append(rect.DOScale(_baseScale * firstBeatScale, firstBeatDuration).SetEase(beatOutEase))
            .Append(rect.DOScale(_baseScale, firstBeatReturn).SetEase(beatInEase))

            // Tiny gap
            .AppendInterval(gapBetweenBeats)

            // Second beat
            .Append(rect.DOScale(_baseScale * secondBeatScale, secondBeatDuration).SetEase(beatOutEase));

        // Optional shake layered onto second beat
        if (sideShake)
        {
            _loop.Join(
                rect.DOPunchAnchorPos(
                    new Vector2(sideShakeAmount, 0f),
                    sideShakeDuration,
                    sideShakeVibrato,
                    sideShakeElasticity,
                    false
                )
            );
        }

        _loop
            .Append(rect.DOScale(_baseScale, secondBeatReturn).SetEase(beatInEase))
            .AppendInterval(restBetweenLoops)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    public void StopAndReset()
    {
        _loop?.Kill();
        if (rect != null)
        {
            rect.localScale = _baseScale;
            rect.anchoredPosition = _baseAnchoredPos;
        }
    }

    void EnsureRefs()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();
    }
}