using UnityEngine;

[CreateAssetMenu(menuName = "Game/Avatar/Avatar Item", fileName = "AvatarItem_")]
public class AvatarItemSO : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite avatarSprite;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private bool isPurchasable;
    [SerializeField] private bool isAccessory;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Sprite AvatarSprite => avatarSprite;
    public Color Color => color;
    public bool IsPurchasable => isPurchasable;
    public bool IsFree => !isPurchasable;
    public bool IsAccessory => isAccessory;
}
