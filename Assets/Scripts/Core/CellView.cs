using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Animal Sprite")]
    [SerializeField] private Image _image;

    [Header("Highlight Juice")]
    [SerializeField] private Image _highlightImage;
    [SerializeField] private float _highlightScale = 1.12f;
    [SerializeField] private float _selectedScale = 1.08f;
    [SerializeField] private float _pulseDuration = 0.18f;
    [SerializeField] private Vector2 _outlineDistance = new Vector2(10f, 10f);

    public Sprite CurrentSprite => _image.sprite;
    public Color CurrentColor => _image.color;

    public Vector2Int Coord { get; private set; }

    private bool _highlighted;
    private Outline _outline;
    private Tween _pulseTween;
    private Vector3 _baseScale;

    private Color _selectedColor = Color.white;
    private Color _normalColor = Color.clear;
    private Color _tutorialLockedColor = new Color(1f, 0.9f, 0.35f, 1f);
    private bool _tutorialLocked;

    public event Action<Vector2Int, Vector2> PointerDown;
    public event Action<Vector2Int, Vector2> Drag;
    public event Action<Vector2Int, Vector2> PointerUp;

    public Image CellImage => _image;
    public RectTransform ImageRect => _image.rectTransform;
    public void SetImageEnabled(bool enabled) => _image.enabled = enabled;

    private void Awake()
    {
        _baseScale = _image.rectTransform.localScale;

        _outline = _image.GetComponent<Outline>();
        if (_outline == null)
            _outline = _image.gameObject.AddComponent<Outline>();

        _outline.enabled = false;
        _outline.effectDistance = _outlineDistance;
        _outline.useGraphicAlpha = true;
    }

    public void Init(Vector2Int coord)
    {
        Coord = coord;
        name = $"Cell ({coord.x},{coord.y})";
    }

    public void SetSprite(Sprite sprite, Color color)
    {
        _image.sprite = sprite;
        _image.color = color;

        if (_highlightImage != null)
            _highlightImage.sprite = sprite;
    }

    public void ConfigureHighlight(Color selectedColor, Color normalColor)
    {
        _selectedColor = selectedColor;
        _normalColor = normalColor;
    }

    public void ConfigureTutorialLock(Color lockColor)
    {
        _tutorialLockedColor = lockColor;
    }

    public void SetHighlighted(bool on)
    {
        if (_highlighted == on)
            return;

        _highlighted = on;
        _pulseTween?.Kill();
        ImageRect.DOKill();

        if (_highlightImage != null)
        {
            _highlightImage.enabled = on;
            _highlightImage.color = _selectedColor;
            _highlightImage.rectTransform.localScale = on
                ? _baseScale * _highlightScale
                : _baseScale;
        }

        if (on)
        {
            ImageRect.localScale = _baseScale;

            ImageRect.DOPunchScale(Vector3.one * 0.08f, 0.12f, 1, 0f);

            _pulseTween = ImageRect
                .DOScale(_baseScale * _selectedScale, _pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            ImageRect.localScale = _baseScale;
        }

    }

    public void SetTutorialLocked(bool locked)
    {
        if (_tutorialLocked == locked)
            return;

        _tutorialLocked = locked;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        _pulseTween?.Kill();
        ImageRect.DOKill();

        if (_highlighted || _tutorialLocked)
        {
            _outline.enabled = true;
            _outline.effectColor = _highlighted ? _selectedColor : _tutorialLockedColor;
            _outline.effectDistance = _outlineDistance;

            ImageRect.localScale = _baseScale;

            if (_highlighted)
            {
                // tiny pop on touch
                ImageRect.DOPunchScale(Vector3.one * 0.08f, 0.12f, 1, 0f);
            }

            float targetScale = _highlighted ? _selectedScale : 1.04f;
            _pulseTween = ImageRect
                .DOScale(_baseScale * targetScale, _pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            _outline.effectColor = _normalColor;
            _outline.enabled = false;
            ImageRect.localScale = _baseScale;
        }
    }

    private void OnDisable()
    {
        _pulseTween?.Kill();
        ImageRect.DOKill();
        ImageRect.localScale = _baseScale;

        if (_outline != null)
            _outline.enabled = false;

        _highlighted = false;
        _tutorialLocked = false;
    }

    public void OnPointerDown(PointerEventData eventData) => PointerDown?.Invoke(Coord, eventData.position);
    public void OnDrag(PointerEventData eventData) => Drag?.Invoke(Coord, eventData.position);
    public void OnPointerUp(PointerEventData eventData) => PointerUp?.Invoke(Coord, eventData.position);
}