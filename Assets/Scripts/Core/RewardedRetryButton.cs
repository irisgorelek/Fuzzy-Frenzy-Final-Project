using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class RewardedRetryButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button retryWithAdButton;
    [SerializeField] private GameObject adCanvasRoot;   // whole ad canvas / panel
    [SerializeField] private Image slideImage;          // image that changes
    [SerializeField] private Sprite[] slides;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Timing")]
    [SerializeField] private float totalAdDuration = 10f;

    private GameBootstrapper _bootstrapper;
    private bool _isRunning;

    private void Start()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();

        if (adCanvasRoot != null)
            adCanvasRoot.SetActive(false);

        if (retryWithAdButton != null)
            retryWithAdButton.onClick.AddListener(StartRewardedRetry);
    }

    private void OnDestroy()
    {
        if (retryWithAdButton != null)
            retryWithAdButton.onClick.RemoveListener(StartRewardedRetry);
    }

    private void StartRewardedRetry()
    {
        if (_isRunning) return;
        StartCoroutine(PlayFakeAdAndRetry());
    }

    private IEnumerator PlayFakeAdAndRetry()
    {
        _isRunning = true;

        if (retryWithAdButton != null)
            retryWithAdButton.interactable = false;

        if (adCanvasRoot != null)
            adCanvasRoot.SetActive(true);

        float elapsed = 0f;
        float remaining = totalAdDuration;

        int slideCount = (slides != null && slides.Length > 0) ? slides.Length : 0;
        float timePerSlide = (slideCount > 0) ? totalAdDuration / slideCount : totalAdDuration;

        int currentSlide = -1;

        while (elapsed < totalAdDuration)
        {
            // Update countdown (whole seconds)
            remaining = totalAdDuration - elapsed;
            if (countdownText != null)
            {
                int seconds = Mathf.CeilToInt(remaining);
                countdownText.text = $"Continue in {seconds}";
            }

            // Slide switching
            if (slideCount > 0 && slideImage != null)
            {
                int newSlide = Mathf.Min((int)(elapsed / timePerSlide), slideCount - 1);
                if (newSlide != currentSlide)
                {
                    currentSlide = newSlide;
                    slideImage.sprite = slides[currentSlide];
                }
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Final 0 display (optional)
        if (countdownText != null)
            countdownText.text = "0";

        // Reward player
        if (_bootstrapper != null)
        {
            _bootstrapper.Economy.AddLives(1);
        }

        if (adCanvasRoot != null)
            adCanvasRoot.SetActive(false);

        _bootstrapper.Economy.InitializeLivesIfNeeded();
        SceneManager.LoadScene("Level");
    }
}