using UnityEngine;
using UnityEngine.UI;

public class AvatarItemButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image selectionFrame;
    [SerializeField] private Image background;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private Color _normalColor;
    private bool _locked;
    public Button Button => button;
    public bool Locked => _locked;

    private void Awake()
    {
        if (background != null)
            _normalColor = background.color;
    }

    public void Setup(AvatarItemSO item, bool isColorCategory, bool locked)
    {
        _locked = locked;

        if (isColorCategory)
        {
            icon.sprite = item.Icon;
            icon.color = item.Color;
        }
        else
        {
            icon.sprite = item.Icon;
            icon.color = locked ? lockedColor : Color.white;
        }

        if (lockIcon != null)
            lockIcon.SetActive(locked);

        gameObject.name = $"Item_{item.DisplayName}";
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : _normalColor;
    }
}
