using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingTrailerController : MonoBehaviour
{
    [System.Serializable]
    public class TrailerSlide
    {
        public Sprite image;

        [Header("Timing")]
        public float slideInDuration = 1.2f;
        public float holdDuration = 0.6f;
        public float slideOutDuration = 1.2f;

        [Header("Movement")]
        public float panDistance = 120f;
    }

    [Header("Slides")]
    [SerializeField] private TrailerSlide[] slides;

    [Header("UI")]
    [SerializeField] private Image trailerImage;
    [SerializeField] private Image fadeOverlay;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField] private string nextSceneName = "MainMenu+Shop";

    private RectTransform _imageRect;

    private void Awake()
    {
        _imageRect = trailerImage.rectTransform;
    }

    private IEnumerator Start()
    {
        yield return PlayTrailer();
    }

    private IEnumerator PlayTrailer()
    {
        if (musicSource != null)
            musicSource.Play();

        for (int i = 0; i < slides.Length; i++)
        {
            TrailerSlide slide = slides[i];

            trailerImage.sprite = slide.image;
            trailerImage.color = Color.white;

            // Start slightly left, but still mostly centered
            _imageRect.anchoredPosition = new Vector2(-slide.panDistance, 0f);

            fadeOverlay.color = new Color(0f, 0f, 0f, 1f);

            // Fade in while moving to center
            Sequence intro = DOTween.Sequence();

            intro.Join(fadeOverlay.DOFade(0f, fadeDuration));
            intro.Join(_imageRect.DOAnchorPosX(0f, slide.slideInDuration)
                .SetEase(Ease.InOutSine));

            yield return intro.WaitForCompletion();

            // Hold centered
            if (slide.holdDuration > 0f)
                yield return new WaitForSeconds(slide.holdDuration);

            // Move slightly right while fading out
            Sequence outro = DOTween.Sequence();

            outro.Join(_imageRect.DOAnchorPosX(slide.panDistance, slide.slideOutDuration)
                .SetEase(Ease.InOutSine));
            outro.Join(fadeOverlay.DOFade(1f, fadeDuration));

            yield return outro.WaitForCompletion();
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade("Level");
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager missing, loading scene directly.");
            SceneManager.LoadScene("Level");
        }
    }
}