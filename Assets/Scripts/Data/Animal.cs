using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Animal", menuName = "Scriptable Objects/Animal")]
public class Animal : ScriptableObject
{
    public string _id;
    public Sprite _sprite;
    public int _points;
    public Color color;

    [Header("Gameplay")]
    [Min(0f)] public float _spawnWeight = 1f; // The chance when randomly spawning (Min 0) - Making the wolf rarer
    public bool _canMatch = true;             // Can be part of a 3+ match
    public bool _canSwap = true;              // Can be swapped by the player
    public bool _affectedByGravity = true;    // Falls during gravity
}
