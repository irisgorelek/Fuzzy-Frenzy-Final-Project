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

    [Header("Confetti (optional)")]
    [SerializeField] private ParticleSystem confetti;

    //Tween _sequence;
    Sequence _sequence;

    void Reset()
    {
        popupTween = GetComponent<UIPopupTween>();
    }

    public void Show(int finalScore, int coinsEarned, int starsEarned)
    {
        if (stars == null || stars.Length < 3)
        {
            Debug.LogError("LevelClearedPopupUI: Stars array must have 3 elements.");
            return;
        }

        starsEarned = Mathf.Clamp(starsEarned, 1, 3);   // Safety
        int displayScore = finalScore * scoreDisplayMultiplier;

        //if (scoreText != null) scoreText.text = ($"Score: {finalScore.ToString()}");
        if (coinsText != null) coinsText.text = coinsEarned.ToString();

        int shownScore = 0;
        if (scoreText != null) scoreText.text = "0";

        for (int i = 0; i < stars.Length; i++)      // Reset stars
        {
            if (stars[i] == null) continue;
            stars[i].SetActive(false);
            stars[i].transform.localScale = Vector3.one;
        }

        // Stop old sequence/confetti
        _sequence?.Kill();
        if (confetti != null) confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Show popup panel (scale+fade)
        if (popupTween != null) popupTween.Show();
        else gameObject.SetActive(true); // fallback

        // Build reveal sequence AFTER popup finishes
        _sequence = DOTween.Sequence()
            .AppendInterval(GetPopupDurationGuess() + delayAfterPopup);
        
        // Score targets per star (1/3, 2/3, 3/3)
        int[] targets =
        {
            Mathf.RoundToInt(displayScore * (1f/3f)),
            Mathf.RoundToInt(displayScore * (2f/3f)),
            displayScore
        };

        for (int i = 0; i < starsEarned; i++)
        {
            int idx = i;
            _sequence.AppendCallback(() =>          // Turn star on + set initial scale
            {
                var go = stars[idx];
                if (go == null) return;
                go.SetActive(true);
                go.transform.localScale = Vector3.one * starScaleFrom;
            });

            // Star pop animation (owned by sequence/ it's own timeline)
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
                    if (scoreText != null) scoreText.text = shownScore.ToString();
                }, target, scoreChunkDuration).SetEase(Ease.OutCubic)
            );

            _sequence.AppendInterval(timeBetweenStars);     // 3) Small gap before next star
        }

        _sequence.AppendCallback(() =>
        {
            if (confetti != null) confetti.Play();
        });
    }

    // Keeps it decoupled from UIPopupTween internals.
    // for perfect sync can expose a Duration property from UIPopupTween.
    float GetPopupDurationGuess()
    {
        //return UIPopupTween.Duration;
        return 0.28f;
    }
}