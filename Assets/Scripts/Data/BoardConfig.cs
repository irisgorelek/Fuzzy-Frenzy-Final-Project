using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BoardConfig")]
public class BoardConfig : ScriptableObject
{
    public int weidth; 
    public int height;
    public int pointGoal;
    public int matchedAnimals;
    public List<Animal> animals;
    public int maxMoves;
}
