using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ShaderReplacerTool : EditorWindow
{
    private Shader sourceShader;
    private Shader targetShader;
    private Vector2 scrollPosition;
    private List<string> changedMaterialsLog = new List<string>();
    private static List<Shader> allShadersCache;
    private static string[] allShaderDisplayNamesCache;
    private const string NONE_SHADER_DISPLAY_NAME = " (None)";

    [MenuItem("Tools/Material Shader Replacer")]
    public static void ShowWindow()
    {
        GetWindow<ShaderReplacerTool>("Shader Replacer").minSize = new Vector2(350, 300);
    }

    void OnEnable()
    {
        PopulateShaderListIfNeeded();
    }

    private static void PopulateShaderListIfNeeded()
    {
        if (allShadersCache == null || allShadersCache.Count == 0 || allShaderDisplayNamesCache == null || allShaderDisplayNamesCache.Length == 0)
        {
            allShadersCache = new List<Shader>();
            List<string> displayNames = new List<string>();

            allShadersCache.Add(null);
            displayNames.Add(NONE_SHADER_DISPLAY_NAME);

            ShaderInfo[] shaderInfos = ShaderUtil.GetAllShaderInfo();
            var sortedShaderInfos = shaderInfos.OrderBy(s => s.name).ToArray();

            foreach (ShaderInfo info in sortedShaderInfos)
            {
                // if (info.name.StartsWith("Hidden/") || info.name.StartsWith("Internal") || !info.hasSupportedSubshaders) continue;

                Shader shader = Shader.Find(info.name);
                if (shader != null)
                {
                    allShadersCache.Add(shader);
                    displayNames.Add(info.name);
                }
            }
            allShaderDisplayNamesCache = displayNames.ToArray();
        }
    }

    private Shader ShaderSelectorGUI(string label, Shader currentShader)
    {
        PopulateShaderListIfNeeded();

        int currentIndex = 0;
        if (currentShader != null)
        {
            for (int i = 1; i < allShadersCache.Count; i++)
            {
                if (allShadersCache[i] == currentShader)
                {
                    currentIndex = i;
                    break;
                }
            }
        }
        else
        {
            if (allShadersCache.Count > 0 && allShadersCache[0] == null)
            {
                currentIndex = 0;
            }
        }


        int newIndex = EditorGUILayout.Popup(label, currentIndex, allShaderDisplayNamesCache);

        if (newIndex >= 0 && newIndex < allShadersCache.Count)
        {
            if (newIndex != currentIndex)
            {
                return allShadersCache[newIndex];
            }
        }
        return currentShader;
    }

    void OnGUI()
    {
        GUILayout.Label("Mass Shader Replacer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool finds all materials in the project using the 'Source Shader' and changes them to the 'Target Shader'. Make sure to back up your project before running!", MessageType.Info);

        sourceShader = ShaderSelectorGUI("Source Shader", sourceShader);
        targetShader = ShaderSelectorGUI("Target Shader", targetShader);

        EditorGUILayout.Space();

        if (GUILayout.Button("Replace Shaders in Project"))
        {
            if (sourceShader == null)
            {
                EditorUtility.DisplayDialog("Error", "Source Shader not selected!", "OK");
                return;
            }
            if (targetShader == null)
            {
                EditorUtility.DisplayDialog("Error", "Target Shader not selected!", "OK");
                return;
            }
            if (sourceShader == targetShader)
            {
                EditorUtility.DisplayDialog("Error", "Source and Target shaders are the same!", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Confirm Shader Replacement",
                $"Are you sure you want to replace all materials using '{sourceShader.name}' with '{targetShader.name}'?\nThis action cannot be easily undone without version control.",
                "Yes, Replace", "Cancel"))
            {
                ReplaceShadersInProject();
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("Log of Changed Materials:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        if (changedMaterialsLog.Count == 0)
        {
            GUILayout.Label("No materials changed yet.");
        }
        else
        {
            foreach (string logEntry in changedMaterialsLog)
            {
                EditorGUILayout.SelectableLabel(logEntry, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void ReplaceShadersInProject()
    {
        changedMaterialsLog.Clear();
        int count = 0;

        string[] guids = AssetDatabase.FindAssets("t:Material");
        EditorUtility.DisplayProgressBar("Replacing Shaders", "Scanning materials...", 0f);

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                // Update progress bar
                bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                    "Replacing Shaders",
                    $"Processing: {material?.name ?? "Unknown Material"} ({i + 1}/{guids.Length})",
                    (float)i / guids.Length);

                if (cancelled)
                {
                    changedMaterialsLog.Add("Operation cancelled by user.");
                    break;
                }

                if (material != null && material.shader == sourceShader)
                {
                    Undo.RecordObject(material, "Change Shader");
                    material.shader = targetShader;
                    EditorUtility.SetDirty(material);
                    changedMaterialsLog.Add($"Changed: {assetPath}");
                    count++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }


        if (count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Successfully replaced shader on {count} material(s). Check the log in the tool window.", "OK");
        }
        else if (!changedMaterialsLog.Any(log => log.Contains("cancelled")))
        {
            EditorUtility.DisplayDialog("No Changes", "No materials found using the source shader, or operation was cancelled before changes.", "OK");
        }

        Repaint();
    }
}