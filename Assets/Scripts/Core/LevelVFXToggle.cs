//using UnityEngine;

//public class LevelVFXToggle : MonoBehaviour
//{
//    [SerializeField] private BoardController boardController;
//    [SerializeField] private GameObject rainRoot;

//    private void Start()
//    {
//        if (boardController == null || rainRoot == null)
//        {
//            Debug.LogError("LevelVFXToggle: Missing references.");
//            return;
//        }

//        var cfg = boardController.Config; // we’ll add this property
//        rainRoot.SetActive(cfg.EnableRain);
//    }
//}
using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelVFXToggle : MonoBehaviour
{
    [SerializeField] private BoardController boardController;
    [SerializeField] private List<VFXRoot> roots = new();

    [Serializable]
    public struct VFXRoot
    {
        public VFXKey key;
        public GameObject root;
        public bool defaultStateIfMissing;
    }

    private void Start() => ApplyFromConfig();

    public void ApplyFromConfig()
    {
        if (boardController == null)
        {
            Debug.LogError("LevelVFXToggle: Missing BoardController reference.");
            return;
        }

        var cfg = boardController.Config;
        if (cfg == null)
        {
            Debug.LogError("LevelVFXToggle: BoardController.Config is null.");
            return;
        }

        // Build lookup from config
        var enabledByKey = new Dictionary<VFXKey, bool>();
        foreach (var t in cfg.VfxToggles)
        {
            if (t.key == null) continue;
            enabledByKey[t.key] = t.enabled;
        }

        // Apply to scene roots
        foreach (var r in roots)
        {
            if (r.root == null) continue;

            bool enabled = r.defaultStateIfMissing;
            if (r.key != null && enabledByKey.TryGetValue(r.key, out var cfgEnabled))
                enabled = cfgEnabled;

            r.root.SetActive(enabled);
        }
    }
}
