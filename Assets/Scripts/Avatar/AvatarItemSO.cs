using UnityEngine;

[CreateAssetMenu(menuName = "Game/Avatar/Avatar Item", fileName = "AvatarItem_")]
public class AvatarItemSO : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite avatarSprite;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private int price; // 0 = free/default

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Sprite AvatarSprite => avatarSprite;
    public Color Color => color;
    public int Price => price;
    public bool IsFree => price <= 0;
}
