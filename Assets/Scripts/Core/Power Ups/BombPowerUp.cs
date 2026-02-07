using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BombPowerUp : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BoardController _board;
    [SerializeField] private BoardView _boardView;
    [SerializeField] private TextMeshProUGUI _amount;

    private bool _armed;

    private void Start()
    {
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
    }
    private void OnDisable()
    {
        UnarmBomb();
    }

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

    public void ArmBomb()
    {
        if (_armed) return;

        _armed = true;
        _boardView.SwapsEnabled = false;

        _boardView.CellTapped += OnCellTapped;  // subscribe
        Debug.Log("Armed bomb");
    }
    public void UnarmBomb()
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

        _armed = false;
        _boardView.CellTapped -= OnCellTapped; // unsubscribe
        TryUseBomb(coord);
    }

    public void TryUseBomb(Vector2Int coord)
    {
        if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.Bomb, 1))
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
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
    }

    public void UpdateCurrentAmount()
    {
        SaveManager.Instance.Add(PowerUpType.Bomb);
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.Bomb).ToString();
    }

    public void ToggleHighlight(bool toggle)
    {
        // TODO: Add highlight effect
    }
}
