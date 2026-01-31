using TMPro;
using UnityEngine;

public class TopBarUI : MonoBehaviour
{
    [SerializeField] private GameBootstrapper bootstrapper;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI lifeTimerText;

    private bool subscribed = false;

    private float timer;

    private void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f)
        {
            timer = 0f;
            Refresh();
        }
    }

    private void Start()
    {
        TrySubscribeAndRefresh();
    }

    private void OnDisable()
    {
        if (subscribed && bootstrapper != null && bootstrapper.Economy != null)
        {
            bootstrapper.Economy.OnChanged -= Refresh;
            subscribed = false;
        }
    }

    private void TrySubscribeAndRefresh()
    {
        if (bootstrapper == null)
        {
            Debug.LogError("TopBarUI: Bootstrapper not assigned.");
            return;
        }

        if (bootstrapper.Economy == null)
        {
            Debug.LogError("TopBarUI: Bootstrapper.Economy is null (created in Awake?).");
            return;
        }

        bootstrapper.Economy.OnChanged += Refresh;
        subscribed = true;
        Refresh();
    }

    private void Refresh()
    {
        var s = bootstrapper.Economy.State;

        coinsText.text = $"Coins: {s.coins}";
        livesText.text = $"Energy: {s.currentLives}/{s.maxLives}";

        if (bootstrapper.Economy.TryGetTimeUntilNextLife(out int seconds))
        {
            int minutes = seconds / 60;
            int secs = seconds % 60;
            lifeTimerText.text = $"{minutes:00}:{secs:00}";
        }
        else
        {
            lifeTimerText.text = "Full";
        }
    }

}
