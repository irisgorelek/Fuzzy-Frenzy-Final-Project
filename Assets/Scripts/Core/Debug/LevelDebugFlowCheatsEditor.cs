using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelDebugFlowCheats))]
public class LevelDebugFlowCheatsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        LevelDebugFlowCheats cheats = (LevelDebugFlowCheats)target;

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("Force Win"))
            cheats.ForceWin();

        if (GUILayout.Button("Force Lose"))
            cheats.ForceLose();

        GUILayout.Space(10);

        if (GUILayout.Button("Add 1 Bomb Booster"))
            cheats.AddOneBombBooster();

        if (GUILayout.Button("Add 1 Timer Booster"))
            cheats.AddOneTimerBooster();

        if (GUILayout.Button("Add 1 Extra Move"))
            cheats.AddOneExtraMove();

        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use debug buttons.", MessageType.Info);
        }
    }
}