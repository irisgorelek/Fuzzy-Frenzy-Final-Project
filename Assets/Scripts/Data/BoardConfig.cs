using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PointsOrMatches { points, matches, collectAnimals }
public enum SongName { happy, chilly }

[CreateAssetMenu(fileName = "BoardConfig", menuName = "Scriptable Objects/BoardConfig")]

public class BoardConfig : ScriptableObject
{
    [Header("Level Config")]
    public int levelIndex;
    public int weidth; 
    public int height;
    public int maxMoves;
    public List<Animal> animals;

    //[Header("Level VFX")]
    //[SerializeField] private bool enableRain;
    ////public bool EnableRain => enableRain;

    [Header("Goal Config")]
    public PointsOrMatches goalType = PointsOrMatches.collectAnimals;
    public int goal;
    public List<AnimalGoal> collectGoals;
    

    [Header("Special Pieces")]
    public Animal wolf;
    public Animal sheep;
    public Animal boneBlock;

    [Header("Delayed Spawns")]
    public Animal blackSheep;
    public int blackSheepUnlockAfterMoves = 1;
    public float blackSheepRollChance = 0.2f; // 20% each roll

    [Header("Weather VFX")]
    [SerializeField] private List<VFXToggle> vfxToggles = new();
    public IReadOnlyList<VFXToggle> VfxToggles => vfxToggles;

    [Header("Level Music")]
    public SongName songNumber;

    [Serializable]
    public struct VFXToggle
    {
        public VFXKey key;
        public bool enabled;
    }

    [Header("Art")]
    [SerializeField] private Sprite _backgroundSprite;
    public Sprite BackgroundSprite => _backgroundSprite;
}


[System.Serializable] // Show and edit this class in the inspector
public class AnimalGoal
{
    public Animal animal;
    public int amount;
}
