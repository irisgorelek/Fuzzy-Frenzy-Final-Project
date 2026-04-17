using DG.Tweening;
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
    [SerializeField] private float _heldPowerUpWorldZ = 0f;
    [SerializeField] private Vector3 _heldPowerUpWorldOffset = Vector3.zero;


    [Header("Selected Visuals")]
    [SerializeField] private PowerUpButtonFeedback _feedback;

    [Header("Camera")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private float _fxWorldZ = 0f;

    private GameBootstrapper _bootstrapper;

    private bool _armed;
    private GameObject _armedHeldFxInstance;
    private RectTransform _buttonRect;

    private void Awake()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        _buttonRect = transform as RectTransform;

        if (_bootstrapper == null)
            Debug.LogError("BombPowerUp: GameBootstrapper not found (should be DontDestroyOnLoad).");
    }

    private void OnEnable()
    {
        if (_bootstrapper != null)
            _bootstrapper.Economy.OnChanged += RefreshAmount;

        RefreshAmount();
    }

    private void OnDisable()
    {
        UnarmBomb();

        if (_bootstrapper != null)
            _bootstrapper.Economy.OnChanged -= RefreshAmount;
    } 

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_armed)
        {
            UnarmBomb();
            _feedback?.SetSelected(false);
        }
        else
        {
            ArmBomb();
            _feedback?.SetSelected(true);
        }
    }

    private void ArmBomb()
    {
        if (_armed) return;

        _armed = true;
        _boardView.SwapsEnabled = false;

        _boardView.CellTapped += OnCellTapped;

        ShowArmedButtonVfx();

        Debug.Log("Armed bomb");
    }
    private void UnarmBomb()
    {
        if (!_armed) return;

        _armed = false;
        _boardView.SwapsEnabled = true;

        _boardView.CellTapped -= OnCellTapped;

        HideArmedButtonVfx();

        Debug.Log("Unarmed bomb");
    }

    private void OnCellTapped(Vector2Int coord)
    {
        if (!_armed)
            return;

        UnarmBomb();
        _feedback?.SetSelected(false);

        TryUseBomb(coord);
    }

    public async void TryUseBomb(Vector2Int coord)
    {
        if (!_bootstrapper.Economy.TryConsumeBooster(BoosterEffectType.FuzzyBlast, 1)) // or Blast
            return;

        _boardView.SwapsEnabled = false;
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

        await _boardView.AnimateBombWarning(coord, 1.5f);

        PlayVFX(coord, _bombExplosionPrefab); // Bomb vfx

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXPitchAdjusted(1, 0.5f); // Play swap sound.
        }

        await _boardView.AnimateBombImpact(affected, 0.28f); // Bomb impact

        _board.TryRemoveCellsFromGrid(affected);
        _powerUpChannel.RaiseEvent("bomb");
        RefreshAmount();

        _feedback?.PlaySuccess();
        _feedback?.PopAmount();
        _boardView.SwapsEnabled = true;

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
        int count = _bootstrapper.Economy.GetBoosterCount(BoosterEffectType.FuzzyBlast); // or Blast if you renamed
        _amount.text = count.ToString();
    }

    private void ShowArmedButtonVfx()
    {
        if (_heldPowerUpPrefab == null || _worldCamera == null || _buttonRect == null)
            return;

        RectTransform anchor = _armedVfxAnchor != null ? _armedVfxAnchor : transform as RectTransform;
        if (anchor == null)
            return;

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);

        float camDistance = Mathf.Abs(_worldCamera.transform.position.z - _heldPowerUpWorldZ);
        Vector3 worldPoint = _worldCamera.ScreenToWorldPoint(
            new Vector3(screenPoint.x, screenPoint.y, camDistance)
        );

        worldPoint.z = _heldPowerUpWorldZ;
        worldPoint += _heldPowerUpWorldOffset;

        _armedHeldFxInstance = Instantiate(_heldPowerUpPrefab, worldPoint, Quaternion.identity);

        RestartParticleSystems(_armedHeldFxInstance);
    }

    private void HideArmedButtonVfx()
    {
        if (_armedHeldFxInstance == null)
            return;

        Destroy(_armedHeldFxInstance);
        _armedHeldFxInstance = null;
    }

    private void RestartParticleSystems(GameObject fxRoot)
    {
        if (fxRoot == null)
            return;

        var systems = fxRoot.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);
        }
    }

}
