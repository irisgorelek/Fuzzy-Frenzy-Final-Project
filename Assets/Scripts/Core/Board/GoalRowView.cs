using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalRowView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Image _finishedImage;

    public void Set(Sprite icon, string text, Color color, bool isComplete = false)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.color = color;
            _icon.enabled = icon != null;
        }

        if (_text != null)
        {
            _text.text = text;
            _text.gameObject.SetActive(!isComplete);
        }

        if (_finishedImage != null)
            _finishedImage.gameObject.SetActive(isComplete);
    }
}