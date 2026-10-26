using UnityEngine;

/// <summary>Per-animal tint only. Burst shape, motion, size and timing remain native prefab settings.</summary>
public sealed class AnimalMatchParticleTint : MonoBehaviour
{
    [SerializeField] private ParticleSystem _puffs;
    [SerializeField] private ParticleSystem _stars;
    [SerializeField, Range(0, 1)] private float _puffTint = .78f;
    [SerializeField, Range(0, 1)] private float _starTint = .40f;
    private ParticleSystem.MinMaxGradient _puffBase, _starBase;
    private bool _cached;

    public void Apply(Color animalColor)
    {
        if (!_cached)
        {
            if (_puffs != null) _puffBase = _puffs.main.startColor;
            if (_stars != null) _starBase = _stars.main.startColor;
            _cached = true;
        }
        Tint(_puffs, _puffBase, animalColor, _puffTint);
        Tint(_stars, _starBase, animalColor, _starTint);
    }

    private static Color Blend(Color original, Color tint, float amount)
    {
        var result = Color.Lerp(original, tint, amount);
        result.a = original.a;
        return result;
    }

    private static Gradient TintGradient(Gradient source, Color tint, float amount)
    {
        var keys = source.colorKeys;
        for (int i = 0; i < keys.Length; i++) keys[i].color = Blend(keys[i].color, tint, amount);
        var result = new Gradient { mode = source.mode };
        result.SetKeys(keys, source.alphaKeys);
        return result;
    }

    private static void Tint(ParticleSystem system, ParticleSystem.MinMaxGradient original, Color color, float amount)
    {
        if (system == null) return;
        var tinted = original;
        if (original.mode == ParticleSystemGradientMode.Color || original.mode == ParticleSystemGradientMode.TwoColors)
        {
            tinted.colorMin = Blend(original.colorMin, color, amount);
            tinted.colorMax = Blend(original.colorMax, color, amount);
        }
        else
        {
            if (original.gradientMin != null) tinted.gradientMin = TintGradient(original.gradientMin, color, amount);
            if (original.gradientMax != null) tinted.gradientMax = TintGradient(original.gradientMax, color, amount);
        }
        var main = system.main; main.startColor = tinted;
    }
}
