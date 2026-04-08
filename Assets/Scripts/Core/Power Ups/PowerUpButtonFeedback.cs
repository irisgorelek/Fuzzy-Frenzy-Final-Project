using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpButtonFeedback : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private RectTransform _iconRect;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _selectedFrame;
    [SerializeField] private TextMeshProUGUI _amountText;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _disabledColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color _selectedColor = new Color(1f, 0.95f, 0.6f, 1f);
    [SerializeField] private Color _flashColor = Color.white;

    [Header("Scale")]
    [SerializeField] private float _normalScale = 1f;
    [SerializeField] private float _selectedScale = 1.12f;
    [SerializeField] private float _pressScale = 0.92f;
    [SerializeField] private float _successPunchScale = 1.18f;

    [Header("Timing")]
    [SerializeField] private float _stateTweenDuration = 0.12f;
    [SerializeField] private float _pressDuration = 0.06f;
    [SerializeField] private float _successDuration = 0.18f;

    private Tween _scaleTween;
    private Tween _colorTween;

    private void Awake()
    {
        if (_iconImage == null)
            _iconImage = GetComponent<Image>();

        if (_iconRect == null && _iconImage != null)
            _iconRect = _iconImage.rectTransform;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetSelected(bool selected)
    {
        if (_iconImage == null || _iconRect == null)
            return;

        _scaleTween?.Kill();
        _colorTween?.Kill();

        if (_selectedFrame != null)
            _selectedFrame.SetActive(selected);

        _colorTween = _iconImage.DOColor(selected ? _selectedColor : _normalColor, _stateTweenDuration);

        _scaleTween = _iconRect
            .DOScale(selected ? _selectedScale : _normalScale, _stateTweenDuration)
            .SetEase(selected ? Ease.OutBack : Ease.OutQuad);
    }

    public void PlayPress()
    {
        if (_iconRect == null)
            return;

        _scaleTween?.Kill();

        Sequence seq = DOTween.Sequence();
        seq.Append(_iconRect.DOScale(_pressScale, _pressDuration).SetEase(Ease.OutQuad));
        seq.Append(_iconRect.DOScale(_normalScale, _pressDuration).SetEase(Ease.OutQuad));

        _scaleTween = seq;
    }

    public void PlaySuccess()
    {
        if (_iconRect == null || _iconImage == null)
            return;

        _scaleTween?.Kill();
        _colorTween?.Kill();

        Color original = _iconImage.color;

        Sequence seq = DOTween.Sequence();
        seq.Append(_iconRect.DOScale(_successPunchScale, _successDuration * 0.4f).SetEase(Ease.OutBack));
        seq.Join(_iconImage.DOColor(_flashColor, _successDuration * 0.25f));
        seq.Append(_iconRect.DOScale(_normalScale, _successDuration * 0.6f).SetEase(Ease.OutQuad));
        seq.Join(_iconImage.DOColor(original, _successDuration * 0.6f));

        _scaleTween = seq;
    }

    public void SetAvailable(bool available)
    {
        if (_iconImage != null)
            _iconImage.color = available ? _normalColor : _disabledColor;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = available ? 1f : 0.8f;
            _canvasGroup.blocksRaycasts = available;
            _canvasGroup.interactable = available;
        }
    }

    public void PopAmount()
    {
        if (_amountText == null)
            return;

        _amountText.rectTransform.DOKill();
        _amountText.rectTransform.localScale = Vector3.one;

        _amountText.rectTransform
            .DOScale(1.18f, 0.12f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }
}