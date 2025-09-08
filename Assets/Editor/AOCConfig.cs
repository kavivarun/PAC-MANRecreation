#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class GhostAOCQuick : EditorWindow
{
    [Header("Base setup")]
    public RuntimeAnimatorController baseController; // your base controller
    public AnimationClip baseNormal;                 // assign GreenGhostNormal

    [Header("Other ghosts")]
    public AnimationClip redNormal;
    public AnimationClip pinkNormal;
    public AnimationClip blueOrOrangeNormal;         // use whichever you have

    public string outputFolder = "Assets/Ghosts/AOCs";

    [MenuItem("Pacman/Ghost AOC Quick")]
    static void Open() => GetWindow<GhostAOCQuick>("Ghost AOC Quick");

    void OnGUI()
    {
        GUILayout.Label("Base", EditorStyles.boldLabel);
        baseController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Base Controller", baseController, typeof(RuntimeAnimatorController), false);
        baseNormal = (AnimationClip)EditorGUILayout.ObjectField("Base Normal (Green)", baseNormal, typeof(AnimationClip), false);

        GUILayout.Space(6);
        GUILayout.Label("Other Ghosts", EditorStyles.boldLabel);
        redNormal = (AnimationClip)EditorGUILayout.ObjectField("Red Normal", redNormal, typeof(AnimationClip), false);
        pinkNormal = (AnimationClip)EditorGUILayout.ObjectField("Pink Normal", pinkNormal, typeof(AnimationClip), false);
        blueOrOrangeNormal = (AnimationClip)EditorGUILayout.ObjectField("Blue/Orange", blueOrOrangeNormal, typeof(AnimationClip), false);

        GUILayout.Space(6);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        using (new EditorGUI.DisabledScope(!CanGenerate()))
        {
            if (GUILayout.Button("Generate 3 AOCs")) GenerateAll();
        }

        if (!CanGenerate())
            EditorGUILayout.HelpBox("Assign base controller, Green clip, and at least one replacement clip.", MessageType.Info);
    }

    bool CanGenerate()
    {
        return baseController != null && baseNormal != null &&
               (redNormal != null || pinkNormal != null || blueOrOrangeNormal != null);
    }

    void GenerateAll()
    {
        EnsureFolder(outputFolder);
        CreateAOC("AOC_Red", redNormal);
        CreateAOC("AOC_Pink", pinkNormal);
        CreateAOC("AOC_Blue", blueOrOrangeNormal); // rename later if you prefer Orange
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Ghost AOCs", "Created the remaining AOCs.", "OK");
    }

    void CreateAOC(string name, AnimationClip replacement)
    {
        if (replacement == null) return;

        var aoc = new AnimatorOverrideController(baseController);
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);
        aoc.GetOverrides(pairs);

        for (int i = 0; i < pairs.Count; i++)
        {
            var baseClip = pairs[i].Key;
            var current = pairs[i].Value;

            // Replace only the Green clip used by Normal state
            if (current == baseNormal || baseClip == baseNormal)
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(baseClip, replacement);
        }

        aoc.ApplyOverrides(pairs);
        var path = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{name}.overrideController");
        AssetDatabase.CreateAsset(aoc, path);
        Debug.Log($"Created {name} at {path}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
