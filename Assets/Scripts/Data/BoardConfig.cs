using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum PointsOrMatches { points, matches, collectAnimals }

[CreateAssetMenu(fileName = "BoardConfig", menuName = "Scriptable Objects/BoardConfig")]

public class BoardConfig : ScriptableObject
{
    [Header("Level Config")]
    public int levelIndex;
    public int weidth; 
    public int height;
    public int maxMoves;
    
    //[Header("Level VFX")]
    //[SerializeField] private bool enableRain;
    ////public bool EnableRain => enableRain;

    [Header("Goal Config")]
    public PointsOrMatches goalType = PointsOrMatches.points;
    public int goal;
    public List<Animal> animals;
    public List<AnimalGoal> collectGoals;

    [Header("Special Pieces")]
    public Animal wolf;
    public Animal sheep;
    public Animal boneBlock;
    
    [SerializeField] private List<VFXToggle> vfxToggles = new();
    public IReadOnlyList<VFXToggle> VfxToggles => vfxToggles;

    [Serializable]
    public struct VFXToggle
    {
        public VFXKey key;
        public bool enabled;
    }
}


[System.Serializable] // Show and edit this class in the inspector
public class AnimalGoal
{
    public Animal animal;
    public int amount;
}
