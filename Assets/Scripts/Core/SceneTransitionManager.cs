using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject _transitionRoot;
    [SerializeField] private Image _fadeImage;

    [Header("Settings")]
    [SerializeField] private float _fadeDuration = 0.8f;
    [SerializeField] private bool _fadeInOnStart = true;

    private bool _isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_fadeInOnStart)
            StartCoroutine(FadeFromBlackRoutine());
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (_isTransitioning)
            return;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isTransitioning = true;

        yield return FadeToBlackRoutine();

        SceneManager.LoadScene(sceneName);

        yield return null;

        yield return FadeFromBlackRoutine();

        _isTransitioning = false;
    }

    private IEnumerator FadeToBlackRoutine()
    {
        if (_transitionRoot != null)
            _transitionRoot.SetActive(true);

        yield return FadeRoutine(0f, 1f);
    }

    private IEnumerator FadeFromBlackRoutine()
    {
        if (_transitionRoot != null)
            _transitionRoot.SetActive(true);

        yield return FadeRoutine(1f, 0f);

        if (_transitionRoot != null)
            _transitionRoot.SetActive(false);
    }

    private IEnumerator FadeRoutine(float from, float to)
    {
        if (_fadeImage == null)
            yield break;

        Color color = _fadeImage.color;
        color.a = from;
        _fadeImage.color = color;

        float time = 0f;

        while (time < _fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / _fadeDuration;

            color.a = Mathf.Lerp(from, to, t);
            _fadeImage.color = color;

            yield return null;
        }

        color.a = to;
        _fadeImage.color = color;
    }
}