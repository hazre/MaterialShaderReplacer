using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

public class ShaderReplacerTool : EditorWindow
{
    private Shader sourceShader;
    private Shader targetShader;
    private Vector2 scrollPosition;
    private List<string> changedMaterialsLog = new List<string>();

    private static Type shaderSelectionDropdownType;
    private static ConstructorInfo shaderSelectionDropdownConstructor;
    private static MethodInfo shaderSelectionDropdownShowMethod;
    private static Type advancedDropdownStateType;
    private static bool reflectionInitialized = false;
    private static bool reflectionFailed = false;

    private bool isSelectingSourceShader;

    [MenuItem("Tools/Material Shader Replacer")]
    public static void ShowWindow()
    {
        GetWindow<ShaderReplacerTool>("Material Shader Replacer").minSize = new Vector2(350, 300);
    }

    void OnEnable()
    {
        InitializeReflection();
    }

    private static void InitializeReflection()
    {
        if (reflectionInitialized && !reflectionFailed) return;
        if (reflectionFailed) return;

        try
        {
            Assembly editorAssembly = typeof(Editor).Assembly;

            shaderSelectionDropdownType = editorAssembly.GetType("UnityEditor.MaterialEditor+ShaderSelectionDropdown");
            if (shaderSelectionDropdownType == null) throw new Exception("MaterialEditor.ShaderSelectionDropdown type not found.");

            advancedDropdownStateType = editorAssembly.GetType("UnityEditor.IMGUI.Controls.AdvancedDropdownState");
            if (advancedDropdownStateType == null) throw new Exception("AdvancedDropdownState type not found.");

            shaderSelectionDropdownConstructor = shaderSelectionDropdownType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(Shader), typeof(Action<object>) },
                null
            );
            if (shaderSelectionDropdownConstructor == null) throw new Exception("ShaderSelectionDropdown constructor(Shader, Action<object>) not found.");

            Type advancedDropdownType = editorAssembly.GetType("UnityEditor.IMGUI.Controls.AdvancedDropdown");
            if (advancedDropdownType == null) throw new Exception("AdvancedDropdown type not found.");
            shaderSelectionDropdownShowMethod = advancedDropdownType.GetMethod(
                "Show",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(Rect) },
                null
            );
            if (shaderSelectionDropdownShowMethod == null) throw new Exception("AdvancedDropdown.Show(Rect) method not found.");

            reflectionInitialized = true;
            reflectionFailed = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"ShaderReplacerTool: Reflection initialization failed. Falling back to basic shader fields. Error: {e.Message}\n{e.StackTrace}");
            reflectionFailed = true;
            reflectionInitialized = true;
        }
    }

    private void ShowReflectedShaderDropdown(Rect buttonRect, Shader currentShader)
    {
        if (reflectionFailed || !reflectionInitialized)
        {
            Debug.LogError("Cannot show reflected shader dropdown due to reflection initialization failure.");
            return;
        }

        try
        {
            Action<object> onSelectedCallback = OnShaderSelectedFromDropdown;
            object[] constructorArgs = new object[] { currentShader, onSelectedCallback };
            object dropdownInstance = shaderSelectionDropdownConstructor.Invoke(constructorArgs);

            shaderSelectionDropdownShowMethod.Invoke(dropdownInstance, new object[] { buttonRect });
        }
        catch (Exception e)
        {
            bool isExitGUI = false;
            Exception originalException = e;

            if (e is TargetInvocationException && e.InnerException != null)
            {
                originalException = e.InnerException;
            }

            if (originalException is ExitGUIException)
            {
                isExitGUI = true;
            }

            if (isExitGUI)
            {
                throw originalException;
            }

            Debug.LogError($"Error showing reflected shader dropdown: {originalException.GetType().Name}: {originalException.Message}\n{originalException.StackTrace}");
            reflectionFailed = true;
            Repaint();
        }
    }

    private void OnShaderSelectedFromDropdown(object shaderNameObj)
    {
        if (shaderNameObj is string shaderName && !string.IsNullOrEmpty(shaderName))
        {
            Shader selectedShader = Shader.Find(shaderName);
            if (isSelectingSourceShader)
            {
                sourceShader = selectedShader;
            }
            else
            {
                targetShader = selectedShader;
            }
            Repaint();
        }
    }

    private Shader FallbackShaderField(string label, Shader currentShader)
    {
        return EditorGUILayout.ObjectField(label, currentShader, typeof(Shader), false) as Shader;
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("This tool finds all materials in the project using the 'Source Shader' and changes them to the 'Target Shader'. Make sure to back up your project before running!", MessageType.Info);

        if (!reflectionInitialized)
        {
            InitializeReflection();
        }

        if (reflectionFailed)
        {
            EditorGUILayout.HelpBox("Failed to load advanced shader dropdown. Using basic ObjectFields.", MessageType.Warning);
            sourceShader = FallbackShaderField("Source Shader", sourceShader);
            targetShader = FallbackShaderField("Target Shader", targetShader);
        }
        else
        {
            GUIContent sourceButtonContent = new GUIContent(sourceShader != null ? sourceShader.name : "(None)");
            Rect sourceRect = EditorGUILayout.GetControlRect();
            sourceRect = EditorGUI.PrefixLabel(sourceRect, EditorGUIUtility.TrTempContent("Source Shader"));
            if (EditorGUI.DropdownButton(sourceRect, sourceButtonContent, FocusType.Keyboard))
            {
                isSelectingSourceShader = true;
                ShowReflectedShaderDropdown(sourceRect, sourceShader);
            }

            GUIContent targetButtonContent = new GUIContent(targetShader != null ? targetShader.name : "(None)");
            Rect targetRect = EditorGUILayout.GetControlRect();
            targetRect = EditorGUI.PrefixLabel(targetRect, EditorGUIUtility.TrTempContent("Target Shader"));
            if (EditorGUI.DropdownButton(targetRect, targetButtonContent, FocusType.Keyboard))
            {
                isSelectingSourceShader = false;
                ShowReflectedShaderDropdown(targetRect, targetShader);
            }
        }

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