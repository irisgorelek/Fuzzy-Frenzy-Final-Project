using UnityEngine;

public class LevelLostPopupUI : MonoBehaviour
{
    [SerializeField] private UIPopupTween popupTween;

    private void Awake()
    {
        if (popupTween == null)
            popupTween = GetComponent<UIPopupTween>();
    }

    public void Show()
    {
        if (popupTween != null)
            popupTween.Show();
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (popupTween != null)
            popupTween.Hide();
        else
            gameObject.SetActive(false);
    }
}