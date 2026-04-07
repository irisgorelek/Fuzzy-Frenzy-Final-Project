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

    [Header("Feedback")]
    [SerializeField] private PowerUpButtonFeedback _feedback;

    private GameBootstrapper _bootstrapper;

    private void Awake()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (_bootstrapper == null)
            Debug.LogError("TimerBomb: GameBootstrapper not found (should be DontDestroyOnLoad).");
    }

    //private void Start()
    //{
    //    RefreshAmount();
    //}
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


    public void OnPointerClick(PointerEventData eventData)
    {
        if (_board == null) return;

        _feedback?.PlayPress();

        // Don't start it twice
        if (_board.IsTimerBombActive)
            return;

        // Check amount
        //if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.TimerBomb, 1))
        if (!_bootstrapper.Economy.TryConsumeBooster(BoosterEffectType.TimerBomb, 1))
        {
            RefreshAmount();
            return;
        }

        RefreshAmount();

        _feedback?.PlaySuccess();
        _feedback?.PopAmount();

        // Start the power up
        _board.StartTimerBomb(_timerLength);
    }

    public void AddOneToCurrentAmount()
    {
        //SaveManager.Instance.Add(PowerUpType.TimerBomb);
        //_amount.text = SaveManager.Instance.GetCount(PowerUpType.TimerBomb).ToString();
        _bootstrapper.Economy.AddBooster(BoosterEffectType.TimerBomb, 1);
        RefreshAmount();
    }

    public void ToggleHighlight(bool toggle)
    {
        // TODO: Add highlight effect
    }
    private void RefreshAmount()
    {
        //int count = SaveManager.Instance.GetCount(PowerUpType.TimerBomb);
        //if (_amount != null)
        //    _amount.text = count.ToString();
        int count = _bootstrapper.Economy.GetBoosterCount(BoosterEffectType.TimerBomb); // or Blast if you renamed
        _amount.text = count.ToString();

        bool available = count > 0 && (_board == null || !_board.IsTimerBombActive);
        _feedback?.SetAvailable(available);
    }
}
