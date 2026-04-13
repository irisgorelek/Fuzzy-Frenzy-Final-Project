using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private Image _image;

    [Header("Highlight Juice")]
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
    }

    public void ConfigureHighlight(Color selectedColor, Color normalColor)
    {
        _selectedColor = selectedColor;
        _normalColor = normalColor;
    }

    public void SetHighlighted(bool on)
    {
        if (_highlighted == on)
            return;

        _highlighted = on;

        _pulseTween?.Kill();
        ImageRect.DOKill();

        if (on)
        {
            _outline.enabled = true;
            _outline.effectColor = _selectedColor;
            _outline.effectDistance = _outlineDistance;

            ImageRect.localScale = _baseScale;

            // tiny pop on touch
            ImageRect.DOPunchScale(Vector3.one * 0.08f, 0.12f, 1, 0f);

            // soft pulse while selected
            _pulseTween = ImageRect
                .DOScale(_baseScale * _selectedScale, _pulseDuration)
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
    }

    public void OnPointerDown(PointerEventData eventData) => PointerDown?.Invoke(Coord, eventData.position);
    public void OnDrag(PointerEventData eventData) => Drag?.Invoke(Coord, eventData.position);
    public void OnPointerUp(PointerEventData eventData) => PointerUp?.Invoke(Coord, eventData.position);
}