using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MovesPowerUp : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private MoveCounter moves;
    [SerializeField] private TextMeshProUGUI _amount;

    [SerializeField] private ShopItemDefinition extraMoveItem;

    private GameBootstrapper _bootstrapper;

    private void Awake()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (_bootstrapper == null)
            Debug.LogError("ExtraMove: GameBootstrapper not found (should be DontDestroyOnLoad).");
    }

    private void OnEnable()
    {
        if (_bootstrapper != null)
            _bootstrapper.Economy.OnChanged += RefreshAmount;

        RefreshAmount();
    }

    private void OnDisable()
    {
        if (_bootstrapper != null)
            _bootstrapper.Economy.OnChanged -= RefreshAmount;
    }

    //private void Start()
    //{
    //    //_amount.text = SaveManager.Instance.GetCount(PowerUpType.ExtraMove).ToString();
    //}

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_bootstrapper == null) return;
        //if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.ExtraMove, 1))
        //    return;

        if (!_bootstrapper.Economy.TryConsumeExtraMove(1))
            return;

        //moves.AddOneMove();
        //_amount.text = SaveManager.Instance.GetCount(PowerUpType.ExtraMove).ToString();
        int movesToAdd = extraMoveItem.ExtraMovesGranted;
        moves.AddMoves(movesToAdd);

        RefreshAmount();
    }

    public void AddOneToCurrentAmount()
    {
        _bootstrapper.Economy.AddExtraMove(1);
        RefreshAmount();
    }

    private void RefreshAmount()
    {
        int count = _bootstrapper.Economy.GetExtraMoveCount(); // or Blast if you renamed
        _amount.text = count.ToString();
    }
}
