using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MovesPowerUp : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MoveCounter moves;
    [SerializeField] private TextMeshProUGUI _amount;

    private void Start()
    {
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.ExtraMove).ToString();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.ExtraMove, 1))
            return;

        moves.AddOneMove();
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.ExtraMove).ToString();
    }

    public void AddOneToCurrentAmount()
    {
        SaveManager.Instance.Add(PowerUpType.ExtraMove);
        _amount.text = SaveManager.Instance.GetCount(PowerUpType.ExtraMove).ToString();
    }
}
