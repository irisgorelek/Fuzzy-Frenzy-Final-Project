using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuDeveloperResetDrawer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform _drawerRoot;

    [Header("Drawer Positions")]
    [Tooltip("Anchored X when hidden off the right edge.")]
    [SerializeField] private float _hiddenAnchoredX = 200f;

    [Tooltip("Anchored X when fully shown.")]
    [SerializeField] private float _shownAnchoredX = -100f;

    [Header("Drawer Behaviour")]
    [Tooltip("If released past this percent toward open, drawer snaps open. Otherwise it snaps closed.")]
    [Range(0f, 1f)]
    [SerializeField] private float _openThreshold = 0.45f;
    [SerializeField] private float _keepOpenThreshold = 0.75f;

    [Tooltip("How quickly the drawer animates to its target after release.")]
    [SerializeField] private float _smoothTime = 0.12f;

    [Header("Reset Behaviour")]
    [SerializeField] private bool _reloadSceneAfterWipe = true;
    [SerializeField] private string _sceneToReload = "MainMenu+Shop";

    private float _dragStartDrawerX;
    private float _dragStartPointerX;

    private float _targetX;
    private float _velocityX;
    private bool _isDragging;
    private bool _isOpen;

    private void Awake()
    {
        if (_drawerRoot == null)
            return;

        SetDrawerXImmediate(_hiddenAnchoredX);
        _targetX = _hiddenAnchoredX;
        _isOpen = false;
    }

    private void Update()
    {
        if (_drawerRoot == null || _isDragging)
            return;

        Vector2 pos = _drawerRoot.anchoredPosition;
        pos.x = Mathf.SmoothDamp(pos.x, _targetX, ref _velocityX, _smoothTime);
        _drawerRoot.anchoredPosition = pos;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_drawerRoot == null)
            return;

        _isDragging = true;
        _velocityX = 0f;

        _dragStartPointerX = eventData.position.x;
        _dragStartDrawerX = _drawerRoot.anchoredPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_drawerRoot == null)
            return;

        float delta = eventData.position.x - _dragStartPointerX;
        float nextX = Mathf.Clamp(_dragStartDrawerX + delta, _shownAnchoredX, _hiddenAnchoredX);

        Vector2 pos = _drawerRoot.anchoredPosition;
        pos.x = nextX;
        _drawerRoot.anchoredPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_drawerRoot == null)
            return;

        _isDragging = false;

        float currentX = _drawerRoot.anchoredPosition.x;
        float openProgress = Mathf.InverseLerp(_hiddenAnchoredX, _shownAnchoredX, currentX);

        if (_isOpen)
        {
            // If drawer was already open, let it close more easily
            if (openProgress >= _keepOpenThreshold)
            {
                _targetX = _shownAnchoredX;
                _isOpen = true;
            }
            else
            {
                _targetX = _hiddenAnchoredX;
                _isOpen = false;
            }
        }
        else
        {
            // If drawer was closed, require enough drag to open it
            if (openProgress >= _openThreshold)
            {
                _targetX = _shownAnchoredX;
                _isOpen = true;
            }
            else
            {
                _targetX = _hiddenAnchoredX;
                _isOpen = false;
            }
        }
    }

    public void WipeEconomyFromButton()
    {
        if (GameBootstrapper.Instance == null)
        {
            Debug.LogWarning("MainMenuDeveloperResetDrawer: no GameBootstrapper.");
            return;
        }

        GameBootstrapper.Instance.Economy.Wipe();

        if (_reloadSceneAfterWipe && !string.IsNullOrEmpty(_sceneToReload))
            //SceneManager.LoadScene(_sceneToReload);
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade(_sceneToReload);
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager missing, loading scene directly.");
            SceneManager.LoadScene(_sceneToReload);
        }
    }

    public void OpenDrawer()
    {
        _targetX = _shownAnchoredX;
        _isOpen = true;
    }

    public void CloseDrawer()
    {
        _targetX = _hiddenAnchoredX;
        _isOpen = false;
    }

    private void SetDrawerXImmediate(float x)
    {
        Vector2 pos = _drawerRoot.anchoredPosition;
        pos.x = x;
        _drawerRoot.anchoredPosition = pos;
    }
}