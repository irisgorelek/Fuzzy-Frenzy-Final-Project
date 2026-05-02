using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubblePresenter : MonoBehaviour
{
    [System.Serializable]
    private class BubbleModeRefs
    {
        public GameObject root;
        public RectTransform bubbleRect;
        public Image bubbleImage;
        public Sprite leftSprite;
        public Sprite rightSprite;
        public TextMeshProUGUI speechText;
        public Button continueButton;
        [Tooltip("Fallback if left/right are not assigned.")]
        public Image speakerImage;
        [Tooltip("Visual-novel left slot (place at bottom-left).")]
        public Image speakerImageLeft;
        [Tooltip("Visual-novel right slot (place at bottom-right).")]
        public Image speakerImageRight;
        public UIPopupTween popupTween;
        public UISlideInTween slideTween;
    }

    public System.Action<int> OnLineShown;
    public System.Action OnSpeechFinished;

    [Header("Canvas Space")]
    [SerializeField] private RectTransform _screenBoundsRect;
    [SerializeField] private Camera _uiCamera;
    [SerializeField] private float _normalYOffset = 140f;
    [SerializeField] private float _screenPadding = 24f;

    [Header("Modes")]
    [SerializeField] private BubbleModeRefs _tutorial;
    [SerializeField] private BubbleModeRefs _normal;
    [SerializeField] private BubbleModeRefs _triggered;

    private TaskCompletionSource<bool> _clickTcs;

    private void Awake()
    {
        BindButton(_tutorial);
        BindButton(_normal);
        BindButton(_triggered);
        HideImmediate();
    }

    private void OnDestroy()
    {
        UnbindButton(_tutorial);
        UnbindButton(_normal);
        UnbindButton(_triggered);
    }

    /// <param name="useRightSide">Random per step: left bubble + left speaker vs right bubble + right speaker.</param>
    public async Task ShowTutorialAsync(Sprite speakerSprite, IList<string> lines, bool useRightSide)
    {
        if (lines == null || lines.Count == 0)
            return;

        SetupModeVisuals(_tutorial, useRightSide, showSpeaker: true, speakerSprite);
        PlayShow(_tutorial, fromRight: useRightSide);

        for (int i = 0; i < lines.Count; i++)
        {
            OnLineShown?.Invoke(i);
            string line = string.IsNullOrWhiteSpace(lines[i]) ? "..." : lines[i];
            _tutorial.speechText.text = line;
            _clickTcs = new TaskCompletionSource<bool>();
            await _clickTcs.Task;
        }

        OnSpeechFinished?.Invoke();
        PlayHide(_tutorial, toRight: useRightSide, deactivateAfterHide: true);
    }

    public async Task ShowNormalAsync(IList<string> lines, Vector3 speakerWorldPosition, bool useRightSide, float totalSeconds)
    {
        if (lines == null || lines.Count == 0)
            return;

        SetupModeVisuals(_normal, useRightSide, showSpeaker: false, null);
        PositionNormalBubbleAboveSpeaker(speakerWorldPosition);
        PlayShow(_normal, fromRight: useRightSide);
        await RunAutoLineSequence(_normal.speechText, lines, totalSeconds);
        PlayHide(_normal, toRight: useRightSide, deactivateAfterHide: true);
    }

    public async Task ShowTriggeredAsync(Sprite speakerSprite, IList<string> lines, bool useRightSide, float totalSeconds)
    {
        if (lines == null || lines.Count == 0)
            return;

        SetupModeVisuals(_triggered, useRightSide, showSpeaker: true, speakerSprite);
        PlayShow(_triggered, fromRight: useRightSide);
        await RunAutoLineSequence(_triggered.speechText, lines, totalSeconds);
        PlayHide(_triggered, toRight: useRightSide, deactivateAfterHide: true);
    }

    private async Task RunAutoLineSequence(TextMeshProUGUI textField, IList<string> lines, float totalSeconds)
    {
        if (textField == null || lines == null || lines.Count == 0)
            return;

        float total = Mathf.Max(0.2f, totalSeconds);
        float perLine = Mathf.Max(0.2f, total / lines.Count);

        for (int i = 0; i < lines.Count; i++)
        {
            textField.text = string.IsNullOrWhiteSpace(lines[i]) ? "..." : lines[i];
            await Task.Delay(Mathf.RoundToInt(perLine * 1000f));
        }
    }

    private void HandleButtonClicked()
    {
        _clickTcs?.TrySetResult(true);
    }

    public void HideImmediate()
    {
        _clickTcs?.TrySetCanceled();
        _clickTcs = null;
        HideModeImmediate(_tutorial);
        HideModeImmediate(_normal);
        HideModeImmediate(_triggered);
    }

    private void HideModeImmediate(BubbleModeRefs mode)
    {
        if (mode == null || mode.root == null)
            return;

        mode.root.SetActive(false);
    }

    private void SetupModeVisuals(BubbleModeRefs mode, bool useRightSide, bool showSpeaker, Sprite speakerSprite)
    {
        if (mode == null || mode.root == null)
            return;

        mode.root.SetActive(true);

        if (mode.bubbleImage != null)
        {
            if (useRightSide && mode.rightSprite != null)
                mode.bubbleImage.sprite = mode.rightSprite;
            else if (!useRightSide && mode.leftSprite != null)
                mode.bubbleImage.sprite = mode.leftSprite;
            else if (mode.leftSprite != null)
                mode.bubbleImage.sprite = mode.leftSprite;
        }

        SetupSpeakerSlots(mode, speakerSprite, useRightSide, showSpeaker);
    }

    private static void SetupSpeakerSlots(BubbleModeRefs mode, Sprite speakerSprite, bool useRightSide, bool showSpeaker)
    {
        if (mode == null)
            return;

        if (!showSpeaker)
        {
            if (mode.speakerImageLeft != null) mode.speakerImageLeft.enabled = false;
            if (mode.speakerImageRight != null) mode.speakerImageRight.enabled = false;
            if (mode.speakerImage != null) mode.speakerImage.enabled = false;
            return;
        }

        bool hasDual = mode.speakerImageLeft != null && mode.speakerImageRight != null;
        if (hasDual)
        {
            if (mode.speakerImageLeft != null)
            {
                mode.speakerImageLeft.enabled = !useRightSide && speakerSprite != null;
                mode.speakerImageLeft.sprite = speakerSprite;
            }

            if (mode.speakerImageRight != null)
            {
                mode.speakerImageRight.enabled = useRightSide && speakerSprite != null;
                mode.speakerImageRight.sprite = speakerSprite;
            }

            if (mode.speakerImage != null)
                mode.speakerImage.enabled = false;
        }
        else if (mode.speakerImage != null)
        {
            mode.speakerImage.enabled = speakerSprite != null;
            mode.speakerImage.sprite = speakerSprite;
        }
    }

    private void PositionNormalBubbleAboveSpeaker(Vector3 speakerWorldPosition)
    {
        if (_normal == null || _normal.bubbleRect == null || _screenBoundsRect == null)
            return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_uiCamera, speakerWorldPosition);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_screenBoundsRect, screenPoint, _uiCamera, out var localPoint))
            return;

        Vector2 size = _normal.bubbleRect.rect.size;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        float minX = -_screenBoundsRect.rect.width * 0.5f + halfW + _screenPadding;
        float maxX = _screenBoundsRect.rect.width * 0.5f - halfW - _screenPadding;
        float minY = -_screenBoundsRect.rect.height * 0.5f + halfH + _screenPadding;
        float maxY = _screenBoundsRect.rect.height * 0.5f - halfH - _screenPadding;

        float targetX = Mathf.Clamp(localPoint.x, minX, maxX);
        float targetY = Mathf.Clamp(localPoint.y + _normalYOffset, minY, maxY);

        _normal.bubbleRect.anchoredPosition = new Vector2(targetX, targetY);
    }

    private void PlayShow(BubbleModeRefs mode, bool fromRight)
    {
        if (mode == null)
            return;

        if (mode.slideTween != null)
        {
            SetSlideDirection(mode.slideTween, fromRight);
            mode.slideTween.PlayIn();
            return;
        }

        mode.popupTween?.Show();
    }

    private void PlayHide(BubbleModeRefs mode, bool toRight, bool deactivateAfterHide)
    {
        if (mode == null)
            return;

        if (mode.slideTween != null)
        {
            SetSlideDirection(mode.slideTween, toRight);
            mode.slideTween.PlayOut(deactivateAfterHide);
            return;
        }

        if (mode.popupTween != null)
            mode.popupTween.Hide(deactivateAfterHide);
        else if (deactivateAfterHide && mode.root != null)
            mode.root.SetActive(false);
    }

    private void SetSlideDirection(UISlideInTween slideTween, bool fromRight)
    {
        if (slideTween == null)
            return;

        slideTween.SetFromSide(fromRight ? UISlideInTween.FromSide.Right : UISlideInTween.FromSide.Left);
    }

    private void BindButton(BubbleModeRefs mode)
    {
        if (mode != null && mode.continueButton != null)
            mode.continueButton.onClick.AddListener(HandleButtonClicked);
    }

    private void UnbindButton(BubbleModeRefs mode)
    {
        if (mode != null && mode.continueButton != null)
            mode.continueButton.onClick.RemoveListener(HandleButtonClicked);
    }
}
