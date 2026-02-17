using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BombPowerUp : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BoardController _board;
    [SerializeField] private BoardView _boardView;
    [SerializeField] private TextMeshProUGUI _amount;

    private GameBootstrapper _bootstrapper;

    private bool _armed;

    private void Awake()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (_bootstrapper == null)
            Debug.LogError("BombPowerUp: GameBootstrapper not found (should be DontDestroyOnLoad).");
    }

    //private void Start()
    //{
    //    //_amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
    //}

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
            ToggleHighlight(false);
        }

        else
        {
            ArmBomb();
            ToggleHighlight(true);
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

        Debug.Log($"Clicked with bomb x: {coord.x}, y:{coord.y}");

        UnarmBomb();

        ToggleHighlight(false);

        TryUseBomb(coord);
    }

    public void TryUseBomb(Vector2Int coord)
    {
        //if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.Bomb, 1))
        //    return;
        if (!_bootstrapper.Economy.TryConsumeBooster(BoosterEffectType.FuzzyBlast, 1)) // or Blast
            return;

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

        _board.TryRemoveCellsFromGrid(affected);
        //_amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
        RefreshAmount();
    }

    public void AddOneToCurrentAmount()
    {
        //SaveManager.Instance.Add(PowerUpType.Bomb);
        //_amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
        _bootstrapper.Economy.AddBooster(BoosterEffectType.FuzzyBlast, 1);
        RefreshAmount();
    }

    public void ToggleHighlight(bool toggle)
    {
        // TODO: Add highlight effect
    }

    private void RefreshAmount()
    {
        int count = _bootstrapper.Economy.GetBoosterCount(BoosterEffectType.FuzzyBlast); // or Blast if you renamed
        _amount.text = count.ToString();
    }

}
