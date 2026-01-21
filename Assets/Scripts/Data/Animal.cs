using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Animal")]
public class Animal : ScriptableObject
{
    public string _id;
    public Sprite _sprite;
    public int _points;
    public Color color;
}
