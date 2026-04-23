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

    [Header("Frost")]
    [SerializeField] private FrostOverlay _frostOverlay;
    [SerializeField] private float _frostFadeDuration = 0.35f;
    [SerializeField] private float _frostAmountWhenActive = 0.65f;

    private GameBootstrapper _bootstrapper;

    private void Awake()
    {
        _bootstrapper = GameBootstrapper.Instance;
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


        if (_board != null)
            _board.OnTimerBombStateChanged += HandleTimerStateChanged;

        RefreshAmount();
    }

    private void OnDisable()
    {
        if (_bootstrapper != null)
            _bootstrapper.Economy.OnChanged -= RefreshAmount;


        if (_board != null)
            _board.OnTimerBombStateChanged -= HandleTimerStateChanged;

        if (_frostOverlay != null)
            _frostOverlay.SetAmountImmediate(0f);
    }

    private void HandleTimerStateChanged(bool active)
    {
        RefreshAmount();

        if (_frostOverlay == null)
            return;

        _frostOverlay.AnimateTo(
            active ? _frostAmountWhenActive : 0f,
            _frostFadeDuration
        );
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (_board == null) 
            return;

        if (_board.IsSpeechBubbleInputBlocked)
            return;

        // Don't start it twice
        if (_board.IsTimerBombActive)
            return;

        if (_bootstrapper.Economy.GetBoosterCount(BoosterEffectType.TimerBomb) <= 0)
        {
            RefreshAmount();
            return;
        }

        _feedback?.PlayPress();

        // Check amount
        //if (!SaveManager.Instance.TryUsePowerUp(PowerUpType.TimerBomb, 1))
        if (!_bootstrapper.Economy.TryConsumeBooster(BoosterEffectType.TimerBomb, 1))
        {
            RefreshAmount();
            return;
        }

        _board.StartTimerBomb(_timerLength);

        RefreshAmount();

        // TODO: Add frost effect

        _feedback?.PlaySuccess();
        _feedback?.PopAmount();
    }

    public void AddOneToCurrentAmount()
    {
        //SaveManager.Instance.Add(PowerUpType.TimerBomb);
        //_amount.text = SaveManager.Instance.GetCount(PowerUpType.TimerBomb).ToString();
        _bootstrapper.Economy.AddBooster(BoosterEffectType.TimerBomb, 1);
        RefreshAmount();
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
