using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Match presentation only: staggered clears and flights to visible animal goals.</summary>
public sealed class BoardClearAnimation : MonoBehaviour
{
    [Serializable]
    private struct AnimalPalette
    {
        public Animal animal;
        public Color color;
    }

    [SerializeField] private BoardView _view;
    [SerializeField] private BoardMatchEffects _effects;
    [Header("Match timing")]
    [SerializeField, InspectorName("Delay Between Tiles (Seconds)"), Range(0, .2f)]
    [Tooltip("Delay between successive tiles in a normal match, including objective collections.")]
    private float _stagger = .07f;
    [SerializeField, Min(.1f)] private float _popSeconds = .25f;
    [SerializeField, Range(1, 1.2f)] private float _popScale = 1.07f;
    [Header("Objective collection")]
    [SerializeField, InspectorName("Flight To Objective (Seconds)"), Min(.2f)]
    [Tooltip("Travel time after the anticipation swell. Larger values make collection slower.")]
    private float _flightSeconds = .65f;
    [SerializeField, Range(0, .15f)]
    [Tooltip("Start the objective pulse and group fade this many seconds before the last arrival.")]
    private float _collectionArrivalOverlap = .08f;
    [SerializeField, Min(.05f)] private float _collectionFadeSeconds = .22f;
    [SerializeField, Range(1.18f, 1.6f)] private float _collectionPeakScale = 1.32f;
    private const float CollectionTravelScale = 1.18f;
    [SerializeField] private AnimalPalette[] _palette;
    private readonly Stack<Image> _pool = new();
    private readonly List<Image> _images = new();
    private readonly List<Sequence> _running = new();

    private Color ColorFor(Sprite sprite)
    {
        if (_palette != null)
            foreach (var entry in _palette)
                if (entry.animal != null && entry.animal._sprite == sprite) return entry.color;
        return Color.white;
    }

    public Task Animate(List<Vector2Int> cells, BoardClearEffect effect)
    {
        if (cells == null || cells.Count == 0 || _view == null) return Task.CompletedTask;
        var ordered = new List<Vector2Int>(cells);
        ordered.Sort((a, b) => a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        var originals = new List<(Image image, Vector3 scale, bool enabled)>();
        var flying = new List<Image>();
        var done = new TaskCompletionSource<bool>();
        var master = DOTween.Sequence();
        var lastArrivals = new Dictionary<GoalRowView, float>();
        var arrivals = new List<(Image image, GoalRowView goal, float time, Vector2 offset)>();
        var goalCounts = new Dictionary<GoalRowView, int>();
        bool cleaned = false;
        bool originalsReleased = false;
        float boardReadyTime = 0;
        int index = 0;
        foreach (var cell in ordered)
        {
            var rect = _view.GetMatchCellRect(cell);
            var image = rect != null ? rect.GetComponent<Image>() : null;
            if (image == null) continue;
            rect.DOKill();
            originals.Add((image, Vector3.one, image.enabled));
            rect.localScale = Vector3.one;
            float delay = index++ * _stagger;
            // Special clears retain their blast presentation. Actual matched goal animals fly.
            var goal = effect == BoardClearEffect.Match ? _view.FindAnimalGoal(image.sprite) : null;
            if (goal != null && goal.IconRect != null)
            {
                var copy = Rent(image);
                flying.Add(copy);
                image.enabled = false;
                goalCounts.TryGetValue(goal, out int slot);
                goalCounts[goal] = slot + 1;
                // Alternate sides, with small variation so arrivals form a loose floating cluster.
                float side = slot % 2 == 0 ? -1 : 1;
                var offset = new Vector2(side * (.16f + .10f * (slot / 2)) + UnityEngine.Random.Range(-.04f, .04f),
                    .45f + UnityEngine.Random.Range(-.06f, .10f));
                var flight = BuildFlight(copy, goal, offset);
                float arrivalTime = delay + flight.Duration();
                lastArrivals[goal] = arrivalTime;
                arrivals.Add((copy, goal, arrivalTime, offset));
                master.Insert(delay, flight);
                boardReadyTime = Mathf.Max(boardReadyTime, delay + .33f);
            }
            else
            {
                var pop = DOTween.Sequence()
                    .Append(rect.DOScale(_popScale, _popSeconds * .30f).SetEase(Ease.OutQuad))
                    .Append(rect.DOScale(0, _popSeconds * .70f).SetEase(Ease.InCubic));
                master.Insert(delay, pop);
                boardReadyTime = Mathf.Max(boardReadyTime, delay + _popSeconds);
                Color tint = ColorFor(image.sprite);
                if (_effects != null && _effects.HasClearEffect(effect))
                    master.InsertCallback(delay + _popSeconds * .36f, () => _effects.ClearCell(rect, effect, tint));
            }
        }
        foreach (var arrival in arrivals)
        {
            float release = CollectionReleaseTime(lastArrivals[arrival.goal]);
            master.Insert(arrival.time, BuildHover(arrival.image, arrival.goal, arrival.offset,
                release - arrival.time + _collectionFadeSeconds));
            master.Insert(release, arrival.image.DOFade(0, _collectionFadeSeconds).SetEase(Ease.InQuad));
        }
        // Begin feedback during the last approach, before that animal starts hovering.
        foreach (var arrival in lastArrivals)
        {
            var goal = arrival.Key;
            master.InsertCallback(CollectionReleaseTime(arrival.Value), () => { if (goal != null) goal.PulseIcon(); });
        }
        void ReleaseBoard()
        {
            if (originalsReleased) return;
            originalsReleased = true;
            foreach (var original in originals)
                if (original.image != null)
                {
                    original.image.rectTransform.localScale = original.scale;
                    original.image.enabled = original.enabled;
                }
            // From here onward only detached visual copies animate. Never touch these cells again.
            done.TrySetResult(true);
        }
        master.InsertCallback(boardReadyTime, ReleaseBoard);
        void Cleanup()
        {
            if (cleaned) return;
            cleaned = true;
            ReleaseBoard();
            foreach (var copy in flying)
                if (copy != null) { copy.gameObject.SetActive(false); _pool.Push(copy); }
            _running.Remove(master);
        }
        master.OnComplete(Cleanup).OnKill(Cleanup);
        _running.Add(master);
        return done.Task;
    }

    private float CollectionReleaseTime(float lastArrival) => lastArrival -
        Mathf.Min(_collectionArrivalOverlap, _flightSeconds * .25f, _collectionFadeSeconds * .5f);

    private Image Rent(Image source)
    {
        Image copy = null;
        while (_pool.Count > 0 && copy == null) copy = _pool.Pop();
        if (copy == null)
        {
            var go = new GameObject("CollectedAnimal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = source.gameObject.layer;
            copy = go.GetComponent<Image>();
            copy.raycastTarget = false;
            _images.Add(copy);
        }
        var parent = _view.GetFxParent();
        var rect = copy.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        var sourceRect = source.rectTransform;
        rect.sizeDelta = new Vector2(
            parent.InverseTransformVector(sourceRect.TransformVector(Vector3.right * sourceRect.rect.width)).magnitude,
            parent.InverseTransformVector(sourceRect.TransformVector(Vector3.up * sourceRect.rect.height)).magnitude);
        rect.position = sourceRect.TransformPoint(sourceRect.rect.center);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();
        copy.sprite = source.sprite;
        copy.color = source.color;
        copy.preserveAspect = true;
        copy.gameObject.SetActive(true);
        return copy;
    }

    private Vector3 HoverTarget(RectTransform rect, GoalRowView goal, Vector2 offset)
    {
        float tile = Mathf.Max(rect.rect.width, rect.rect.height);
        Vector3 target = goal != null && goal.IconRect != null
            ? rect.parent.InverseTransformPoint(goal.IconRect.TransformPoint(goal.IconRect.rect.center))
            : rect.localPosition;
        target += (Vector3)(offset * tile);
        target.z = rect.localPosition.z;
        return target;
    }

    private Tween BuildHover(Image copy, GoalRowView goal, Vector2 offset, float seconds)
    {
        var rect = copy.rectTransform;
        float tile = Mathf.Max(rect.rect.width, rect.rect.height);
        float phase = UnityEngine.Random.Range(0, Mathf.PI * 2);
        float elapsed = 0;
        return DOTween.To(() => elapsed, value =>
        {
            elapsed = value;
            if (copy == null) return;
            float angle = phase + value * 5;
            rect.localPosition = HoverTarget(rect, goal, offset) + new Vector3(
                (Mathf.Sin(angle) - Mathf.Sin(phase)) * tile * .035f,
                (Mathf.Cos(angle) - Mathf.Cos(phase)) * tile * .045f, 0);
            rect.localRotation = Quaternion.Euler(0, 0, (Mathf.Sin(angle) - Mathf.Sin(phase)) * 4);
        }, seconds, seconds).SetEase(Ease.Linear);
    }

    private Sequence BuildFlight(Image copy, GoalRowView goal, Vector2 offset)
    {
        var rect = copy.rectTransform;
        Vector3 start = rect.localPosition;
        float tile = Mathf.Max(rect.rect.width, rect.rect.height);
        float progress = 0;
        var flight = DOTween.To(() => progress, value =>
        {
            progress = value;
            if (copy == null) return;
            Vector3 target = HoverTarget(rect, goal, offset);
            target.z = start.z;
            float side = start.x < target.x ? -1 : 1;
            Vector3 a = start + new Vector3(side * tile * .85f, tile * .75f, 0);
            Vector3 b = target + new Vector3(side * tile * 1.2f, -tile * .65f, 0);
            float s = 1 - value;
            rect.localPosition = s * s * s * start + 3 * s * s * value * a + 3 * s * value * value * b + value * value * value * target;
            rect.localScale = Vector3.one * Mathf.Lerp(CollectionTravelScale, .38f, value);
        }, 1, _flightSeconds).SetEase(Ease.InOutCubic);
        return DOTween.Sequence()
            .Append(rect.DOScale(.88f, .08f).SetEase(Ease.OutQuad))
            .Append(rect.DOScale(_collectionPeakScale, .16f).SetEase(Ease.OutCubic))
            .Append(rect.DOScale(CollectionTravelScale, .09f).SetEase(Ease.InOutSine))
            .Append(flight);
    }

    private void OnDisable()
    {
        foreach (var sequence in _running.ToArray()) sequence.Kill();
        foreach (var image in _images) if (image != null) Destroy(image.gameObject);
        _images.Clear(); _pool.Clear();
    }
}
