using UnityEngine;
using UnityEngine.UI;

public class AvatarItemButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image selectionFrame;
    [SerializeField] private Image background;
    [SerializeField] private Color selectedColor = Color.white;

    private Color _normalColor;
    public Button Button => button;
    private void Awake()
    {
        if (background != null)
            _normalColor = background.color;
    }
    public void Setup(AvatarItemSO item, bool isColorCategory)
    {
        if (isColorCategory)
        {
            icon.sprite = item.Icon;
            icon.color = item.Color;
        }
        else
        {
            icon.sprite = item.Icon;
            icon.color = Color.white;
        }

        gameObject.name = $"Item_{item.DisplayName}";
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : _normalColor;
    }
}
