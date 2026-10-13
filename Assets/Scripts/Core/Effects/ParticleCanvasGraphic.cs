using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws local-space particles in the board canvas, including screen-space overlay canvases.</summary>
[RequireComponent(typeof(ParticleSystem), typeof(CanvasRenderer))]
[ExecuteAlways]
public sealed class ParticleCanvasGraphic : MaskableGraphic
{
    [SerializeField] private Texture _texture;
    private ParticleSystem _system;
    private ParticleSystem.Particle[] _particles;
    private ParticleSystemRenderer _particleRenderer;
    private Texture _lastTexture;
    private bool _nativePreview;
    public override Texture mainTexture
    {
        get
        {
            if (_particleRenderer == null) _particleRenderer = GetComponent<ParticleSystemRenderer>();
            var source = _particleRenderer != null ? _particleRenderer.sharedMaterial : null;
            var texture = source != null ? source.mainTexture : null;
            return texture != null ? texture : _texture != null ? _texture : Texture2D.whiteTexture;
        }
    }

    public void SetTexture(Texture texture) { _texture = texture; SetMaterialDirty(); }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        _system = GetComponent<ParticleSystem>();
        ConfigureRenderer();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ConfigureRenderer();
    }

    protected override void OnTransformParentChanged()
    {
        base.OnTransformParentChanged();
        ConfigureRenderer();
    }

    private void ConfigureRenderer()
    {
        // Retained for rollback only. A disabled bridge must never suppress native rendering.
        if (!enabled)
        {
            var native = GetComponent<ParticleSystemRenderer>();
            if (native != null) native.enabled = true;
            canvasRenderer.Clear();
            return;
        }
        // Native rendering makes Particle System Play/Simulate work in Prefab Mode.
        // Inside a Canvas, only the UI mesh renders, preventing a duplicate world-space effect.
        _nativePreview = GetComponentInParent<Canvas>() == null;
#if UNITY_EDITOR
        // Unity may add an environment Canvas around a UI prefab in Prefab Mode.
        // Use the native renderer there too, so the particle preview toolbar animates it.
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        _nativePreview |= stage != null && stage.scene == gameObject.scene;
#endif
        var renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer != null) renderer.enabled = _nativePreview;
        if (_nativePreview) canvasRenderer.Clear();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        var native = GetComponent<ParticleSystemRenderer>();
        if (native != null) native.enabled = true;
    }

    private void LateUpdate()
    {
        if (!Application.IsPlaying(gameObject)) ConfigureRenderer();
        // Renderer material edits must also reach the gameplay Canvas, including pooled instances.
        var texture = mainTexture;
        if (_lastTexture != texture)
        {
            _lastTexture = texture;
            SetMaterialDirty();
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_nativePreview) return;
        if (_system == null) _system = GetComponent<ParticleSystem>();
        if (_particles == null || _particles.Length < _system.main.maxParticles)
            _particles = new ParticleSystem.Particle[_system.main.maxParticles];
        int count = _system.GetParticles(_particles);
        for (int i = 0; i < count; i++)
        {
            var p = _particles[i];
            Vector3 size = p.GetCurrentSize3D(_system) * 0.5f;
            Color32 tint = p.GetCurrentColor(_system) * color;
            float angle = p.rotation * Mathf.Deg2Rad;
            Vector3 right = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * size.x;
            Vector3 up = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0) * size.y;
            Rect uv = ParticleUV(p);
            int start = vh.currentVertCount;
            vh.AddVert(p.position - right - up, tint, new Vector2(uv.xMin, uv.yMin));
            vh.AddVert(p.position - right + up, tint, new Vector2(uv.xMin, uv.yMax));
            vh.AddVert(p.position + right + up, tint, new Vector2(uv.xMax, uv.yMax));
            vh.AddVert(p.position + right - up, tint, new Vector2(uv.xMax, uv.yMin));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }

    // Board flipbooks use a whole-sheet grid, lifetime curve, zero start frame and one cycle.
    // Read the same module as Unity's native renderer so Canvas and Prefab Mode stay in sync.
    private Rect ParticleUV(ParticleSystem.Particle particle)
    {
        var sheet = _system.textureSheetAnimation;
        if (!sheet.enabled) return new Rect(0, 0, 1, 1);
        int columns = Mathf.Max(1, sheet.numTilesX);
        int rows = Mathf.Max(1, sheet.numTilesY);
        float age = Mathf.Clamp01(1 - particle.remainingLifetime / particle.startLifetime);
        int frame = Mathf.Clamp(Mathf.FloorToInt(sheet.frameOverTime.Evaluate(age) * columns * rows),
            0, columns * rows - 1);
        return new Rect((frame % columns) / (float)columns,
            1 - (frame / columns + 1) / (float)rows, 1f / columns, 1f / rows);
    }
}
