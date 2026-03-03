using DG.Tweening;
using TMPro;
using UnityEngine;

public class LevelClearedPopupUI : MonoBehaviour
{
    [Header("Popup Animation (your panel tween)")]
    [SerializeField] private UIPopupTween popupTween;

    [Header("Texts")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;

    [Header("Stars (GameObjects)")]
    [SerializeField] private GameObject[] stars; // size 3, order: 0..2

    [Header("Star Animation")]
    [SerializeField] private float delayAfterPopup = 0.26f;      //=>0.05f
    [SerializeField] private float starPopDuration = 0.18f;      //=>0.18f
    [SerializeField] private float timeBetweenStars = 0.10f;     //=>0.15f
    [SerializeField] private float starScaleFrom = 0.2f;         //=>0.2f

    [Header("Score Display Juice")]
    [SerializeField] private int scoreDisplayMultiplier = 10;
    [SerializeField] private float scoreChunkDuration = 0.15f; // per star chunk , =>0.25f

    [Header("Fireworks")]
    [SerializeField] private GameObject fireworksRoot;

        Sequence _sequence;

    void Reset()
    {
        popupTween = GetComponent<UIPopupTween>();
    }

    public void Show(int finalScore, int coinsEarned, int starsEarned)
    {
        // Safety / assumptions
        starsEarned = Mathf.Clamp(starsEarned, 1, 3);
        if (stars == null || stars.Length < 3)
        {
            Debug.LogError("LevelClearedPopupUI: Stars array must have 3 elements.");
            return;
        }

        int displayScore = finalScore * scoreDisplayMultiplier;

        // 1) Reset state
        ResetUIState(coinsEarned);

        // 2) Show popup
        ShowPopup();

        // 3) Start celebration
        SetFireworksActive(true);

        // 4) Animate results (stars + score)
        PlayResultsAnimation(displayScore, starsEarned);
    }

    private void ResetUIState(int coinsEarned)
    {
        // Stop previous animation + turn off celebration visuals
        _sequence?.Kill();
        SetFireworksActive(false);

        // Set coins immediately
        if (coinsText != null)
            coinsText.text = coinsEarned.ToString();

        // Reset score
        if (scoreText != null)
            scoreText.text = "0";

        // Reset stars
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            stars[i].SetActive(false);
            stars[i].transform.localScale = Vector3.one;
        }
    }

    private void ShowPopup()
    {
        if (popupTween != null)
            popupTween.Show();
        else
            gameObject.SetActive(true);
    }

    private void PlayResultsAnimation(int displayScore, int starsEarned)
    {
        int shownScore = 0;

        // Build reveal sequence AFTER popup finishes
        _sequence = DOTween.Sequence()
            .AppendInterval(GetPopupDurationGuess() + delayAfterPopup);

        // Score targets per star (1/3, 2/3, 3/3)
        int[] targets =
        {
            Mathf.RoundToInt(displayScore * (1f / 3f)),
            Mathf.RoundToInt(displayScore * (2f / 3f)),
            displayScore
        };

        for (int i = 0; i < starsEarned; i++)
        {
            int idx = i;

            // Turn star on + set initial scale
            _sequence.AppendCallback(() =>
            {
                var go = stars[idx];
                if (go == null) return;

                go.SetActive(true);
                go.transform.localScale = Vector3.one * starScaleFrom;
            });

            // Star pop animation (owned by sequence)
            if (stars[idx] != null)
            {
                _sequence.Append(stars[idx].transform
                    .DOScale(1f, starPopDuration)
                    .SetEase(Ease.OutBack));
            }

            // Score count-up to the next chunk target
            int target = targets[idx];

            _sequence.Append(
                DOTween.To(() => shownScore, x =>
                {
                    shownScore = x;
                    if (scoreText != null)
                        scoreText.text = shownScore.ToString();
                }, target, scoreChunkDuration)
                .SetEase(Ease.OutCubic)
            );

            // Small gap before next star
            _sequence.AppendInterval(timeBetweenStars);
        }
    }

    private float GetPopupDurationGuess()
    {
        return 0.28f;
    }

    private void SetFireworksActive(bool active)
    {
        if (fireworksRoot == null)
            return;

        fireworksRoot.SetActive(active);
    }
}