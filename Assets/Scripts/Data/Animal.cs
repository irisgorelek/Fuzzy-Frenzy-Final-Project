using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Animal")]
public class Animal : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private Sprite _sprite;
    [SerializeField] private int _points;
}
