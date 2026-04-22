using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class UIFrostEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Shader frostShader;

    [Header("Textures")]
    [SerializeField] private Texture2D frostTexture;
    [SerializeField] private Texture2D frostNormals;

    [Header("Settings")]
    [Range(0f, 1f)][SerializeField] private float frostAmount = 0.5f;
    [SerializeField] private float edgeSharpness = 10f;
    [Range(0f, 1f)][SerializeField] private float minFrost = 0.25f;
    [Range(0f, 1f)][SerializeField] private float maxFrost = 0.75f;
    [Range(0f, 1f)][SerializeField] private float seeThroughness = 0.1f;
    [Range(0f, 1f)][SerializeField] private float distortion = 0.05f;

    private Material _material;

    private void Awake()
    {
        Setup();
        Apply();
    }

    private void OnEnable()
    {
        Setup();
        Apply();
    }

    private void OnValidate()
    {
        Setup();
        Apply();
    }

    private void Setup()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage == null || frostShader == null)
            return;

        if (_material == null || _material.shader != frostShader)
        {
            CleanupMaterial();

            _material = new Material(frostShader);
            _material.hideFlags = HideFlags.DontSave;

            targetImage.material = _material;
        }
    }

    public void Apply()
    {
        if (_material == null)
            return;

        edgeSharpness = Mathf.Max(1f, edgeSharpness);

        float finalBlend = Mathf.Clamp01(Mathf.Clamp01(frostAmount) * (maxFrost - minFrost) + minFrost);

        _material.SetTexture("_FrostTex", frostTexture);
        _material.SetTexture("_FrostNormals", frostNormals);
        _material.SetFloat("_BlendAmount", finalBlend);
        _material.SetFloat("_EdgeSharpness", edgeSharpness);
        _material.SetFloat("_SeeThroughness", seeThroughness);
        _material.SetFloat("_Distortion", distortion);
    }

    public void SetFrostAmount(float amount)
    {
        frostAmount = Mathf.Clamp01(amount);
        Apply();
    }

    private void OnDisable()
    {
        CleanupMaterial();
    }

    private void OnDestroy()
    {
        CleanupMaterial();
    }

    private void CleanupMaterial()
    {
        if (targetImage != null && targetImage.material == _material)
            targetImage.material = null;

        if (_material != null)
        {
            if (Application.isPlaying)
                Destroy(_material);
            else
                DestroyImmediate(_material);
        }

        _material = null;
    }
}