using FuzzyFrenzy.Domain;
using UnityEngine;

[CreateAssetMenu(fileName = "Animal", menuName = "Scriptable Objects/Animal")]
public class Animal : ScriptableObject
{
    public string _id;
    public Sprite _sprite;
    public int _points;
    public Color color;

    [Header("Gameplay")]
    [Min(0f)] public float _spawnWeight = 1f; // The chance when randomly spawning (Min 0) - Making the wolf and the bones rarer
    public bool _canMatch = true;             // Can be part of a 3+ match
    public bool _canSwap = true;              // Can be swapped by the player
    public bool _affectedByGravity = true;    // Falls during gravity

    [Header("Abilities")]
    [Tooltip("Behaviours rules look for. Bones: Breaks Next To Clear, Resists Blasts, No Refill. Black sheep: Line Blast On Swap, No Refill.")]
    public TileTraits _traits;
    [Tooltip("The tile this animal eats when they are next to each other on a settled board (the wolf eats sheep).")]
    public Animal _eats;
    [Tooltip("What an eaten tile turns into (the wolf leaves bones). Empty leaves the cell empty for refill.")]
    public Animal _leavesBehind;

    [Header("Audio")]
    public int _speakSfxId;
}
