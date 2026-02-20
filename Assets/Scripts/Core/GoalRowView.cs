using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalRowView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _text;

    public void Set(Sprite icon, string text, Color color)
    {
        _icon.sprite = icon;
        _icon.color = color;
        _icon.enabled = icon != null; // hide icon if null (useful for points)
        _text.text = text;
    }
}