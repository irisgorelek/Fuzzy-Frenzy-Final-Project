using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BombPowerUp : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BoardController _board;
    [SerializeField] private BoardView _boardView;
    [SerializeField] private TextMeshProUGUI _amount;
    [SerializeField] private PowerUpEventChannelSO _powerUpChannel;
    [SerializeField] private ParticleSystem _bombExplosionPrefab;
    [SerializeField] private Vector3 _bombFxOffset;
    [SerializeField] private float _bombFxLifetime = 2f;

    [Header("Selected Visuals")]
    [SerializeField] private PowerUpButtonFeedback _feedback;

    private GameBootstrapper _bootstrapper;

    private bool _armed;

    private void Awake()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();
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

    //private void OnDisable()
    //{
    //    UnarmBomb();
    //}

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

        _boardView.CellTapped += OnCellTapped;  // subscribe
        Debug.Log("Armed bomb");
    }
    private void UnarmBomb()
    {
        if (!_armed) return;

        _armed = false;
        _boardView.SwapsEnabled = true;

        _boardView.CellTapped -= OnCellTapped;  // unsubscribe
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

    public void TryUseBomb(Vector2Int coord)
    {
        //if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.Bomb, 1))
        //    return;
        if (!_bootstrapper.Economy.TryConsumeBooster(BoosterEffectType.FuzzyBlast, 1)) // or Blast
            return;

        if (_bombExplosionPrefab != null && _boardView != null)
        {
            Vector3 spawnPos = _boardView.GetCellWorldPosition(coord) + _bombFxOffset;

            var fx = Instantiate(
                _bombExplosionPrefab,
                spawnPos,
                Quaternion.identity,
                _boardView.GetFxParent()
            );

            fx.Play();
            Destroy(fx.gameObject, _bombFxLifetime);
        }

        var affected = new List<Vector2Int>(9);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXPitchAdjusted(1, 0.5f); // Play swap sound.
        }

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

        _board.TryRemoveCellsFromGrid(affected);
        _powerUpChannel.RaiseEvent("bomb");
        //_amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
        RefreshAmount();

        _feedback?.PlaySuccess();
        _feedback?.PopAmount();
    }

    public void AddOneToCurrentAmount()
    {
        //SaveManager.Instance.Add(PowerUpType.Bomb);
        //_amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
        _bootstrapper.Economy.AddBooster(BoosterEffectType.FuzzyBlast, 1);
        RefreshAmount();
    }

    private void RefreshAmount()
    {
        int count = _bootstrapper.Economy.GetBoosterCount(BoosterEffectType.FuzzyBlast); // or Blast if you renamed
        _amount.text = count.ToString();
    }

}
