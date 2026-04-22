using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FrostOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _image;
    [SerializeField] private Shader _frostShader;

    [Header("Textures")]
    [SerializeField] private Texture2D _frostTexture;
    [SerializeField] private Texture2D _frostNormals;

    [Header("Look")]
    [Range(0f, 1f)][SerializeField] private float _minFrost = 0.25f;
    [Range(0f, 1f)][SerializeField] private float _maxFrost = 0.75f;
    [SerializeField] private float _edgeSharpness = 1f;
    [Range(0f, 1f)][SerializeField] private float _seeThroughness = 0.1f;
    [Range(0f, 1f)][SerializeField] private float _distortion = 0.05f;

    private Material _runtimeMat;
    private Tween _tween;
    private float _currentAmount;

    private void Awake()
    {
        SetupMaterial();
        SetAmountImmediate(0f);
    }

    private void OnEnable()
    {
        SetupMaterial();
        ApplyStaticProperties();
        ApplyAmount();
    }

    private void OnValidate()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        if (_runtimeMat != null)
        {
            ApplyStaticProperties();
            ApplyAmount();
        }
    }

    private void SetupMaterial()
    {
        if (_image == null)
            _image = GetComponent<Image>();

        if (_image == null || _frostShader == null)
            return;

        if (_runtimeMat == null)
        {
            _runtimeMat = new Material(_frostShader);
            _runtimeMat.hideFlags = HideFlags.DontSave;
            _image.material = _runtimeMat;
        }

        ApplyStaticProperties();
    }

    private void ApplyStaticProperties()
    {
        if (_runtimeMat == null)
            return;

        _edgeSharpness = Mathf.Max(1f, _edgeSharpness);

        _runtimeMat.SetTexture("_FrostTex", _frostTexture);
        _runtimeMat.SetTexture("_FrostNormals", _frostNormals);
        _runtimeMat.SetFloat("_EdgeSharpness", _edgeSharpness);
        _runtimeMat.SetFloat("_SeeThroughness", _seeThroughness);
        _runtimeMat.SetFloat("_Distortion", _distortion);
    }

    private float ToShaderBlend(float value01)
    {
        value01 = Mathf.Clamp01(value01);
        return Mathf.Clamp01(value01 * (_maxFrost - _minFrost) + _minFrost);
    }

    private void ApplyAmount()
    {
        if (_runtimeMat == null)
            return;

        _runtimeMat.SetFloat("_BlendAmount", ToShaderBlend(_currentAmount));

        if (_image != null)
            _image.enabled = _currentAmount > 0.001f;
    }

    public void SetAmountImmediate(float value01)
    {
        _currentAmount = Mathf.Clamp01(value01);
        ApplyAmount();
    }

    public void AnimateTo(float target01, float duration)
    {
        target01 = Mathf.Clamp01(target01);

        _tween?.Kill();

        if (_image != null && target01 > 0.001f)
            _image.enabled = true;

        _tween = DOTween.To(
            () => _currentAmount,
            x =>
            {
                _currentAmount = x;
                ApplyAmount();
            },
            target01,
            duration
        )
        .SetEase(Ease.OutSine)
        .OnComplete(() =>
        {
            if (_image != null && _currentAmount <= 0.001f)
                _image.enabled = false;
        });
    }

    private void OnDisable()
    {
        _tween?.Kill();
    }

    private void OnDestroy()
    {
        _tween?.Kill();

        if (_image != null && _image.material == _runtimeMat)
            _image.material = null;

        if (_runtimeMat != null)
        {
            if (Application.isPlaying)
                Destroy(_runtimeMat);
            else
                DestroyImmediate(_runtimeMat);
        }
    }
}