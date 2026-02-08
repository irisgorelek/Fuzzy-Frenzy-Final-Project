using Codice.CM.Client.Differences;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TimerPowerUp : MonoBehaviour, IPointerClickHandler
{
    [Header("Board Referrences")]
    [SerializeField] private BoardController _board;
    [SerializeField] private BoardView _boardView;

    [Header("Parameters")]
    [SerializeField] private float _timerLength = 5f;

    [Header("On Screen Texts")]
    [SerializeField] private TextMeshProUGUI _amount;

    private void Start()
    {
        RefreshAmount();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_board == null) return;

        // Don't start it twice
        if (_board.IsTimerBombActive)
            return;

        // Check amount
        if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.TimerBomb, 1))
        {
            RefreshAmount();
            return;
        }

        RefreshAmount();

        // Start the power up
        _board.StartTimerBomb(_timerLength);
    }

    public void AddOneToCurrentAmount()
    {
        SaveManager.Instance.Add(PowerUpType.TimerBomb);
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.TimerBomb).ToString();
    }

    public void ToggleHighlight(bool toggle)
    {
        // TODO: Add highlight effect
    }
    private void RefreshAmount()
    {
        int count = SaveManager.Instance.GetCount(PowerUpType.TimerBomb);
        if (_amount != null)
            _amount.text = count.ToString();
    }
}
