using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>A pooled burst. One local unit equals one tile width.</summary>
public sealed class BoardParticleEffect : MonoBehaviour
{
    private ParticleSystem[] _systems;
    private Action<BoardParticleEffect> _returnToPool;
    private float _age;
    private TaskCompletionSource<bool> _finished;
    public Task Completion => _finished?.Task ?? Task.CompletedTask;
    public int LiveParticleCount
    {
        get
        {
            int count = 0;
            foreach (var ps in GetComponentsInChildren<ParticleSystem>()) count += ps.particleCount;
            return count;
        }
    }

    public void Play(Action<BoardParticleEffect> returnToPool = null, Color? animalTint = null)
    {
        _returnToPool = returnToPool;
        _finished = new TaskCompletionSource<bool>();
        _age = 0;
        _systems ??= GetComponentsInChildren<ParticleSystem>(true);
        var tint = GetComponent<AnimalMatchParticleTint>();
        if (tint != null) tint.Apply(animalTint ?? Color.white);
        foreach (var ps in _systems)
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Prefabs contain native preview bursts for the heads. Gameplay emits them
            // explicitly only at the line origin, with board-dependent travel distances.
            if (ps.name.StartsWith("DirectionalStreaks")) continue;
            ps.Play(false);
        }
    }

    // Native bursts serve Prefab Mode; gameplay replaces them with exact line distances.
    public void EmitLineHeads(float negativeDistance, float positiveDistance, float secondsPerTile)
    {
        foreach (var ps in _systems)
        {
            if (!ps.name.StartsWith("DirectionalStreaks")) continue;
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var emission = ps.emission;
            emission.enabled = false;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;
            ps.Play(false);
            bool left = ps.name.EndsWith("Left");
            EmitHead(ps, left ? -1 : 1, left ? negativeDistance : positiveDistance, secondsPerTile);
        }
    }

    private static void EmitHead(ParticleSystem ps, float direction, float distance, float secondsPerTile)
    {
        if (distance <= 0) return;
        var emit = new ParticleSystem.EmitParams
        {
            position = Vector3.zero,
            velocity = Vector3.right * (direction / secondsPerTile),
            startLifetime = distance * secondsPerTile
        };
        ps.Emit(emit, 1);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age < 0.1f || _systems == null) return;
        foreach (var ps in _systems) if (ps.IsAlive(false)) return;
        if (_returnToPool != null) _returnToPool(this);
        else gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_systems == null) return;
        foreach (var ps in _systems) if (ps != null)
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        _finished?.TrySetResult(true);
    }
}
