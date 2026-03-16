using UnityEngine;
using UnityEngine.UI;

public class AvatarTabButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;

    public Button Button => button;

    public void Setup(Sprite categoryIcon, string categoryName)
    {
        if (icon != null) icon.sprite = categoryIcon;
        gameObject.name = $"Tab_{categoryName}";
    }

    public void SetActive(bool isActive, Sprite activeSprite, Sprite inactiveSprite)
    {
        if (background != null)
            background.sprite = isActive ? activeSprite : inactiveSprite;
    }
}
