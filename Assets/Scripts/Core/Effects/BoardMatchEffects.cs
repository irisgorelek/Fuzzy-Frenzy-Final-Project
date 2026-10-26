using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum BoardClearEffect { Match, BlackSheep }

/// <summary>Owns reusable particle instances. It never delays or modifies board resolution.</summary>
public sealed class BoardMatchEffects : MonoBehaviour
{
    [SerializeField] private BoardView _view;
    [SerializeField] private BoardParticleEffect _match;
    [SerializeField] private BoardParticleEffect _boneBreak;
    [SerializeField] private BoardParticleEffect _blackSheep;
    [SerializeField] private BoardParticleEffect _bomb;
    [SerializeField] private BoardParticleEffect _sheepAnger;
    [SerializeField, Range(0.5f, 1.5f)] private float _effectScale = 1f;
    private readonly Dictionary<BoardParticleEffect, Stack<BoardParticleEffect>> _pools = new();
    private readonly List<BoardParticleEffect> _instances = new();
    private readonly List<Task> _linePlayback = new();
    private bool _linePlaying;
    public bool HasMatch => isActiveAndEnabled && _match != null;
    public bool HasBomb => isActiveAndEnabled && _bomb != null;
    public bool HasClearEffect(BoardClearEffect effect) => isActiveAndEnabled &&
        (effect == BoardClearEffect.BlackSheep ? _blackSheep != null : _match != null);

    public void Match(RectTransform cell) => Spawn(_match, cell);
    public void ClearCell(RectTransform cell, BoardClearEffect effect, Color? animalTint = null)
    {
        Spawn(effect == BoardClearEffect.BlackSheep ? _blackSheep : _match, cell, 1,
            effect == BoardClearEffect.Match ? animalTint : null);
    }
    public void BoneBreak(RectTransform cell) => Spawn(_boneBreak, cell);
    public void Bomb(RectTransform cell, float tileSpan = 3.2f) => Spawn(_bomb, cell, tileSpan);

    // Separate from per-cell clears: one upright cue per sheep activation, including vertical blasts.
    public bool SheepAnger(RectTransform cell, IEnumerable<RectTransform> boardCells)
    {
        var fx = Spawn(_sheepAnger, cell);
        if (fx == null) return false;
        // Native renderer sorting on the cue prefab keeps it above line and wool particles.
        float tile = fx.transform.localScale.x;
        Vector3 position = fx.transform.localPosition;
        if (boardCells != null)
        {
            var corners = new Vector3[4];
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var boardCell in boardCells)
            {
                if (boardCell == null) continue;
                boardCell.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector2 local = fx.transform.parent.InverseTransformPoint(corner);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
            }
            // Reserve room for maximum pop/tilt and the full upward drift.
            float margin = 0.9f * tile;
            if (min.x < max.x && min.y < max.y)
            {
                position.x = Mathf.Clamp(position.x, min.x + margin, Mathf.Max(min.x + margin, max.x - margin));
                position.y = Mathf.Clamp(position.y, min.y + margin, Mathf.Max(min.y + margin, max.y - margin - 0.2f * tile));
            }
        }
        fx.transform.localPosition = position;
        return true;
    }

    public void BlackSheep(RectTransform cell, bool horizontal, float negativeDistance, float positiveDistance)
    {
        _linePlayback.Clear();
        _linePlaying = true;
        var fx = Spawn(_blackSheep, cell);
        if (fx == null) return;
        fx.transform.localRotation = Quaternion.Euler(0, 0, horizontal ? 0 : 90);
        // Undo artistic scale when calculating travel so the heads still reach the actual cells.
        fx.EmitLineHeads(negativeDistance / _effectScale, positiveDistance / _effectScale,
            BoardEffectsTiming.LineStep * _effectScale);
    }

    public Task WaitForBlackSheepParticles()
    {
        _linePlaying = false;
        return Task.WhenAll(_linePlayback.ToArray());
    }

    private BoardParticleEffect Spawn(BoardParticleEffect prefab, RectTransform cell, float scale = 1f, Color? animalTint = null)
    {
        if (!isActiveAndEnabled || prefab == null || cell == null || _view == null) return null;
        if (!_pools.TryGetValue(prefab, out var pool)) _pools.Add(prefab, pool = new());
        BoardParticleEffect fx = null;
        while (pool.Count > 0 && fx == null) fx = pool.Pop();
        var parent = _view.GetFxParent();
        if (fx == null)
        {
            fx = Instantiate(prefab, parent);
            _instances.Add(fx);
        }
        fx.transform.SetParent(parent, false);
        fx.transform.localPosition = parent.InverseTransformPoint(cell.position);
        fx.transform.localRotation = Quaternion.identity;
        Vector3 edge = parent.InverseTransformVector(cell.TransformVector(Vector3.right * cell.rect.width));
        float tileWidth = edge.magnitude / Mathf.Max(0.01f, Mathf.Abs(cell.localScale.x));
        fx.transform.localScale = Vector3.one * tileWidth * _effectScale * scale;
        fx.transform.SetAsLastSibling();
        fx.gameObject.SetActive(true);
        fx.Play(effect => { effect.gameObject.SetActive(false); pool.Push(effect); }, animalTint);
        if (prefab == _blackSheep || (_linePlaying && prefab == _boneBreak)) _linePlayback.Add(fx.Completion);
        return fx;
    }

    private void OnDisable()
    {
        foreach (var instance in _instances) if (instance != null) Destroy(instance.gameObject);
        _instances.Clear();
        _pools.Clear();
        _linePlayback.Clear();
        _linePlaying = false;
    }
}
