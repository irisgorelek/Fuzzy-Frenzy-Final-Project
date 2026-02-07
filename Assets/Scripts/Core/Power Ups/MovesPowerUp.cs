using TMPro;
using UnityEngine;

public class MovesPowerUp : MonoBehaviour
{
    [SerializeField] private MoveCounter moves;
    [SerializeField] private TextMeshProUGUI _amount;

    private void Start()
    {
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.ExtraMove).ToString();
    }
    public void AddMove()
    {
        if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.ExtraMove, 1))
            return;

        moves.AddOneMove();
    }

    public void UpdateCurrentAmount()
    {
        SaveManager.Instance.Add(PowerUpType.ExtraMove);
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.ExtraMove).ToString();
    }
}
