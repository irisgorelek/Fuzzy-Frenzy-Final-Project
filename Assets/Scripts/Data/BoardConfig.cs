using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BoardConfig")]
public class BoardConfig : ScriptableObject
{
    public int weidth; //{ get; }
    public int height; //{ get; }
    public List<Animal> animals;
}
