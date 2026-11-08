using System;
using UnityEngine;

[Serializable]
public struct VFXRoot
{
    public VFXKey key;
    public GameObject root;
    public bool defaultStateIfMissing;
}
