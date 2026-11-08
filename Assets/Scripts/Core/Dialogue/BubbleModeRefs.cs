using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
internal class BubbleModeRefs
{
    public GameObject root;
    public RectTransform bubbleRect;
    public Image bubbleImage;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public TextMeshProUGUI speechText;
    [Tooltip("Fallback if left/right are not assigned.")]
    public Image speakerImage;
    [Tooltip("Visual-novel left slot (place at bottom-left).")]
    public Image speakerImageLeft;
    [Tooltip("Visual-novel right slot (place at bottom-right).")]
    public Image speakerImageRight;
    public UIPopupTween popupTween;
    public UISlideInTween slideTween;
}
