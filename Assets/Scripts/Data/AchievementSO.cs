using UnityEngine;

public enum AchievementCategory
{
    Level,
    Animal,
    PowerUp
}


[CreateAssetMenu(fileName = "AchievementSO", menuName = "Scriptable Objects/AchievementSO")]
public class AchievementSO : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _title;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private AchievementCategory _category;
    [SerializeField] private int _goal;

    public string Id => _id;
    public string Title => _title;
    public string Description => _description;
    public Sprite Icon => _icon;
    public AchievementCategory Category => _category;
    public int Goal => _goal;
}
