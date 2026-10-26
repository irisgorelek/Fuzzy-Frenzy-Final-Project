using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GoalRowView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Image _finishedImage;
    [Header("Animal collection feedback")]
    [SerializeField] private Image _shine;
    [SerializeField] private Image _numberShine;
    [SerializeField, Range(1, 1.6f)] private float _iconPulseScale = 1.28f;
    private Vector3 _shineScale, _numberShineScale;
    private Color _shineColor, _numberShineColor;
    private Vector2 _numberShineOffset;
    private bool _shineCached;
    [SerializeField] private Color _feedbackGold = new Color(1f, .82f, .29f, 1f);
    private Tween _numberTween, _iconTween;
    private float _numberPulse;
    private bool _initialized, _complete;
    private string _lastText;
    private Vector3 _iconScale;
    private Vector3 _textScale;

    public Sprite IconSprite => _icon != null ? _icon.sprite : null;
    public RectTransform IconRect => _icon != null ? _icon.rectTransform : null;
    public bool CanCollect => !_complete && _icon != null && _icon.enabled;

    private void Awake()
    {
        if (_icon != null) _iconScale = _icon.rectTransform.localScale;
        CacheShines();
    }

    private void CacheShines()
    {
        if (_shineCached) return;
        _shineCached = true;
        if (_shine != null)
        {
            _shineScale = _shine.rectTransform.localScale;
            _shineColor = _shine.color;
        }
        if (_numberShine != null)
        {
            _numberShineScale = _numberShine.rectTransform.localScale;
            _numberShineColor = _numberShine.color;
            _numberShineOffset = _numberShine.rectTransform.anchoredPosition;
        }
    }

    public void Set(Sprite icon, string text, Color color, bool isComplete = false, bool animateCounter = false)
    {
        bool changed = _initialized && _lastText != text;
        _lastText = text;
        _initialized = true;
        _complete = isComplete;
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.color = color;
            _icon.enabled = icon != null;
        }

        if (_text != null)
        {
            _text.text = text;
            _text.gameObject.SetActive(!isComplete);
        }

        if (_finishedImage != null)
            _finishedImage.gameObject.SetActive(isComplete);

        if (changed && animateCounter) PulseNumber();
    }

    public void PulseIcon()
    {
        if (_icon == null || !_icon.enabled) return;
        _iconTween?.Kill();
        var rect = _icon.rectTransform;
        if (_iconScale == Vector3.zero) _iconScale = rect.localScale;
        rect.localScale = _iconScale;
        if (_text != null && _textScale == Vector3.zero) _textScale = _text.rectTransform.localScale;
        PrepareShine();
        _iconTween = DOTween.Sequence()
            .Append(rect.DOScale(_iconScale * .90f, .06f).SetEase(Ease.OutQuad))
            .Append(rect.DOScale(_iconScale * _iconPulseScale, .14f).SetEase(Ease.OutQuad))
            .Append(rect.DOScale(_iconScale, .22f).SetEase(Ease.OutBack))
            .OnUpdate(() =>
            {
                SyncShine(rect);
                if (_text != null)
                    _text.rectTransform.localScale = _textScale * (rect.localScale.x / _iconScale.x);
            })
            .OnKill(() =>
            {
                if (rect != null) rect.localScale = _iconScale;
                if (_text != null) _text.rectTransform.localScale = _textScale;
                if (_shine != null) _shine.gameObject.SetActive(false);
            });
        if (_shine != null)
        {
            var pulse = (Sequence)_iconTween;
            pulse.Insert(0, _shine.DOFade(_shineColor.a, .13f).SetEase(Ease.OutQuad));
            pulse.Insert(.13f, _shine.DOFade(0, .29f).SetEase(Ease.InQuad));
        }
    }

    private void PrepareShine()
    {
        if (_shine == null) return;
        CacheShines();
        var color = _shineColor;
        color.a = 0;
        _shine.color = color;
        _shine.gameObject.SetActive(true);
        SyncShine(_icon.rectTransform);
    }

    private void SyncShine(RectTransform icon)
    {
        if (_shine == null) return;
        // Preserve the prefab's position, width and height; animate only its scale.
        _shine.rectTransform.localScale = _shineScale * (icon.localScale.x / _iconScale.x);
    }

    private void PulseNumber()
    {
        _numberTween?.Kill();
        _numberPulse = 0;
        if (_text == null || !_text.gameObject.activeInHierarchy)
        {
            if (_finishedImage != null && _finishedImage.gameObject.activeInHierarchy)
            {
                var rect = _finishedImage.rectTransform;
                Vector3 scale = rect.localScale;
                _numberTween = rect.DOScale(scale * 1.15f, .13f).SetLoops(2, LoopType.Yoyo)
                    .OnKill(() => { if (rect != null) rect.localScale = scale; });
            }
            return;
        }
        PrepareNumberShine();
        var pulse = DOTween.Sequence()
            .Append(DOTween.To(() => _numberPulse, SetNumberPulse, -.4f, .06f).SetEase(Ease.OutQuad))
            .Append(DOTween.To(() => _numberPulse, SetNumberPulse, 1, .14f).SetEase(Ease.OutQuad))
            .Append(DOTween.To(() => _numberPulse, SetNumberPulse, 0, .30f).SetEase(Ease.OutQuad))
            .OnKill(() =>
            {
                if (_text != null) SetNumberPulse(0);
                if (_numberShine != null) _numberShine.gameObject.SetActive(false);
            });
        _numberTween = pulse;
        if (_numberShine != null)
        {
            pulse.Insert(0, _numberShine.DOFade(_numberShineColor.a, .18f).SetEase(Ease.OutQuad));
            pulse.Insert(.18f, _numberShine.DOFade(0, .32f).SetEase(Ease.InQuad));
        }
    }

    private void PrepareNumberShine()
    {
        if (_numberShine == null) return;
        CacheShines();
        var color = _numberShineColor;
        color.a = 0;
        _numberShine.color = color;
        _numberShine.gameObject.SetActive(true);
        SetNumberPulse(0);
    }

    private void SetNumberPulse(float value)
    {
        _numberPulse = value;
        if (_text == null || !_text.gameObject.activeInHierarchy) return;
        _text.ForceMeshUpdate();
        var info = _text.textInfo;
        // Animate only the digits; preserve the label, text content and layout.
        Vector3 center = Vector3.zero;
        int count = 0;
        for (int i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible || !char.IsDigit(c.character)) continue;
            center += (c.bottomLeft + c.topRight) * .5f;
            count++;
        }
        if (count == 0) return;
        center /= count;
        float scale = 1 + value * .20f;
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible || !char.IsDigit(c.character)) continue;
            min = Vector2.Min(min, c.bottomLeft);
            max = Vector2.Max(max, c.topRight);
            var mesh = info.meshInfo[c.materialReferenceIndex];
            for (int j = 0; j < 4; j++)
            {
                int vertex = c.vertexIndex + j;
                mesh.vertices[vertex] = center + (mesh.vertices[vertex] - center) * scale;
                mesh.colors32[vertex] = Color.Lerp(mesh.colors32[vertex], _feedbackGold, value);
            }
        }
        if (_numberShine != null && _numberShine.gameObject.activeSelf)
        {
            var rect = _numberShine.rectTransform;
            rect.position = _text.transform.TransformPoint(center) + rect.parent.TransformVector(_numberShineOffset);
            rect.localScale = _numberShineScale * scale;
        }
        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    private void OnDisable()
    {
        _numberTween?.Kill(); _iconTween?.Kill(); _initialized = false;
        if (_shine != null) _shine.gameObject.SetActive(false);
        if (_numberShine != null) _numberShine.gameObject.SetActive(false);
    }
}
