using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A full-screen raycast target consumes tutorial taps before they reach the board or boosters.</summary>
public sealed class DialogueTapSurface : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SpeechBubblePresenter _presenter;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _presenter.ContinueFromScreenTap();
        eventData.Use();
    }
}
