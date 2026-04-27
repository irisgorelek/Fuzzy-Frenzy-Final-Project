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
        public float panDuration = 2.5f;
        public float holdDuration = 0.5f;
        public float panDistance = 180f;
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

            _imageRect.anchoredPosition = new Vector2(-slide.panDistance, 0f);
            trailerImage.color = Color.white;

            fadeOverlay.color = new Color(0f, 0f, 0f, 1f);

            Sequence seq = DOTween.Sequence();

            seq.Join(fadeOverlay.DOFade(0f, fadeDuration));
            seq.Join(_imageRect.DOAnchorPosX(slide.panDistance, slide.panDuration)
                .SetEase(Ease.InOutSine));

            yield return seq.WaitForCompletion();

            if (slide.holdDuration > 0f)
                yield return new WaitForSeconds(slide.holdDuration);

            yield return fadeOverlay.DOFade(1f, fadeDuration).WaitForCompletion();
        }

        SceneManager.LoadScene(nextSceneName);
    }
}