using UnityEngine;

[CreateAssetMenu(menuName = "Game/Avatar/Avatar Item", fileName = "AvatarItem_")]
public class AvatarItemSO : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite avatarSprite;
    [SerializeField] private Color color = Color.white;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Sprite AvatarSprite => avatarSprite;
    public Color Color => color;
}
