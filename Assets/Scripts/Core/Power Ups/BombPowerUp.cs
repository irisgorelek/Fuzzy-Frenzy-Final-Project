using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BombPowerUp : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private BoardController _board;
    [SerializeField] private BoardView _boardView;

    [Header("Saving")]
    [SerializeField] private TextMeshProUGUI _amount;
    [SerializeField] private PowerUpEventChannelSO _powerUpChannel;

    [Header("VFX")]
    [SerializeField] private Transform _bombParent;
    [SerializeField] private GameObject _bombExplosionPrefab;
    [SerializeField] private GameObject _heldPowerUpPrefab;
    [SerializeField] private RectTransform _armedVfxAnchor;

    [Header("Selected Visuals")]
    [SerializeField] private PowerUpButtonFeedback _feedback;

    [Header("Camera")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private float _fxWorldZ = 0f;

    private GameBootstrapper _bootstrapper;

    private bool _armed;
    private GameObject _armedHeldFxInstance;
    private bool _bombInProgress;

    private void Awake()
    {
        _bootstrapper = GameBootstrapper.Instance;

        if (_bootstrapper == null)
            Debug.LogError("BombPowerUp: GameBootstrapper not found (should be DontDestroyOnLoad).");
    }

    private void OnEnable()
    {
        if (_bootstrapper != null)
            _bootstrapper.Economy.OnChanged += RefreshAmount;

        if (_board != null)
            _board.OnTimerBombStateChanged += HandleTimerStateChanged;

        RefreshAmount();
    }

    private void OnDisable()
    {
        UnarmBomb();

        if (_bootstrapper != null)
            _bootstrapper.Economy.OnChanged -= RefreshAmount;

        if (_board != null)
            _board.OnTimerBombStateChanged -= HandleTimerStateChanged;
    } 

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_bombInProgress)
            return;

        if (_board != null && _board.IsSpeechBubbleInputBlocked)
            return;

        if (_board != null && _board.IsTimerBombActive)
            return;

        // Arming waits until the board is idle; disarming is always allowed.
        if (!_armed && _board != null && !_board.CanAcceptInput)
            return;

        if (_armed)
        {
            UnarmBomb();
            _feedback?.StopHoldLoop();
            _feedback?.SetSelected(false);
        }
        else
        {
            ArmBomb();
            _feedback?.SetSelected(true);
            _feedback?.StartHoldLoop();
        }
    }

    private void ArmBomb()
    {
        if (_armed || _bombInProgress || _bootstrapper == null)
            return;

        // Not during a turn, another booster or dialogue.
        if (_board != null && !_board.CanAcceptInput)
            return;

        if (_board != null && _board.IsTimerBombActive)
            return;

        int count = _bootstrapper.Economy.GetBoosterCount(BoosterEffectType.FuzzyBlast);
        if (count <= 0)
            return;

        _armed = true;
        if (_board != null)
            _board.SetSwapsSuppressed(true); // taps choose the target instead of swiping
        _boardView.CellTapped += OnCellTapped;

        //ShowArmedButtonVfx(); - The VFX looks bad with the current UI (Might add something else in a future update)

        Debug.Log("Armed bomb");
    }
    private void UnarmBomb()
    {
        if (!_armed) return;

        _armed = false;
        if (_board != null)
            _board.SetSwapsSuppressed(false);

        _boardView.CellTapped -= OnCellTapped;

        HideArmedButtonVfx();

        Debug.Log("Unarmed bomb");
    }

    private void OnCellTapped(Vector2Int coord)
    {
        if (!_armed || _bombInProgress)
            return;

        if (_board != null && _board.IsSpeechBubbleInputBlocked)
            return;

        _feedback?.StopHoldLoop();
        UnarmBomb();
        _feedback?.SetSelected(false);

        TryUseBomb(coord);
    }

    public async void TryUseBomb(Vector2Int coord)
    {
        if (_bombInProgress)
            return;

        // Not during a turn, another booster or dialogue.
        if (_board == null || !_board.CanAcceptInput)
            return;

        _bombInProgress = true;
        RefreshAmount();

        IDisposable inputLock = null;
        try
        {
            if (!_bootstrapper.Economy.TryConsumeBooster(BoosterEffectType.FuzzyBlast, 1))
                return;

            // Hold input until the bomb's whole cascade has played out.
            inputLock = _board.AcquireInputLock("bomb");
            var affected = new List<Vector2Int>(9);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = coord.x + dx;
                    int ny = coord.y + dy;

                    if (nx < 0 || nx >= _board.GetWidth() || ny < 0 || ny >= _board.GetHeight())
                        continue;

                    affected.Add(new Vector2Int(nx, ny));
                }
            }

            await _boardView.AnimateBombWarning(coord, 1f);

            if (!_boardView.PlayBombBurstFx(coord))
                PlayVFX(coord, _bombExplosionPrefab);

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFXPitchAdjusted(1, 0.5f);

            await _boardView.AnimateBombImpact(affected, BoardEffectsTiming.BombImpact);

            var resolving = _board.ResolveBombAsync(affected);
            _powerUpChannel.RaiseEvent("bomb");
            RefreshAmount();
            _feedback?.PopAmount();

            await resolving;
        }
        finally
        {
            inputLock?.Dispose();
            _bombInProgress = false;
            RefreshAmount();
        }
    }

    private void PlayVFX(Vector2Int coord, GameObject vfx)
    {
        if (vfx == null || _boardView == null || _worldCamera == null)
            return;

        Vector3 worldPoint = _boardView.GetCellScenePosition(coord, _worldCamera, _fxWorldZ);
        GameObject fx = Instantiate(vfx, worldPoint, Quaternion.identity);
        fx.transform.SetParent(_bombParent, true);
        var ps = fx.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(fx, lifetime);
        }
        else
        {
            Destroy(fx, 1.2f);
        }
    }

    public void AddOneToCurrentAmount()
    {
        _bootstrapper.Economy.AddBooster(BoosterEffectType.FuzzyBlast, 1);
        RefreshAmount();
    }

    private void RefreshAmount()
    {
        if (_bootstrapper == null)
            return;

        int count = _bootstrapper.Economy.GetBoosterCount(BoosterEffectType.FuzzyBlast);
        _amount.text = count.ToString();

        bool available = count > 0 && !_bombInProgress && (_board == null || !_board.IsTimerBombActive);
        _feedback?.SetAvailable(available);
    }

    private void HideArmedButtonVfx()
    {
        if (_armedHeldFxInstance == null)
            return;

        Destroy(_armedHeldFxInstance);
        _armedHeldFxInstance = null;
    }

    private void HandleTimerStateChanged(bool active)
    {
        if (active && _armed)
        {
            _feedback?.StopHoldLoop();
            UnarmBomb();
            _feedback?.SetSelected(false);
        }

        RefreshAmount();
    }

}
