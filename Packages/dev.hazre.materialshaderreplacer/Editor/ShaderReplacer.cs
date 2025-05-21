using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

public class ShaderReplacerTool : EditorWindow
{
    private enum ProcessingScope
    {
        EntireProject,
        GameObjectAndChildren
    }

    private ProcessingScope currentScope = ProcessingScope.EntireProject;
    private GameObject rootGameObjectForScope;

    [SerializeField]
    private List<GameObject> excludedGameObjects = new List<GameObject>();
    private SerializedObject serializedObjectInstance;
    private SerializedProperty excludedGameObjectsProp;

    private Shader sourceShader;
    private Shader targetShader;
    private Vector2 logScrollPosition;
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
        serializedObjectInstance = new SerializedObject(this);
        excludedGameObjectsProp = serializedObjectInstance.FindProperty("excludedGameObjects");
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
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                new Type[] { typeof(Shader), typeof(Action<object>) }, null);
            if (shaderSelectionDropdownConstructor == null) throw new Exception("ShaderSelectionDropdown constructor(Shader, Action<object>) not found.");
            Type advancedDropdownType = editorAssembly.GetType("UnityEditor.IMGUI.Controls.AdvancedDropdown");
            if (advancedDropdownType == null) throw new Exception("AdvancedDropdown type not found.");
            shaderSelectionDropdownShowMethod = advancedDropdownType.GetMethod("Show",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                new Type[] { typeof(Rect) }, null);
            if (shaderSelectionDropdownShowMethod == null) throw new Exception("AdvancedDropdown.Show(Rect) method not found.");
            reflectionInitialized = true;
            reflectionFailed = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"ShaderReplacerTool: Reflection initialization failed. Using basic fields. Error: {e.Message}");
            reflectionFailed = true;
            reflectionInitialized = true;
        }
    }

    private void ShowReflectedShaderDropdown(Rect buttonRect, Shader currentShader)
    {
        if (reflectionFailed || !reflectionInitialized) { Debug.LogError("Reflection not ready for shader dropdown."); return; }
        try
        {
            Action<object> onSelectedCallback = OnShaderSelectedFromDropdown;
            object[] constructorArgs = new object[] { currentShader, onSelectedCallback };
            object dropdownInstance = shaderSelectionDropdownConstructor.Invoke(constructorArgs);
            shaderSelectionDropdownShowMethod.Invoke(dropdownInstance, new object[] { buttonRect });
        }
        catch (Exception e)
        {
            Exception originalException = (e is TargetInvocationException && e.InnerException != null) ? e.InnerException : e;
            if (originalException is ExitGUIException) { throw originalException; }
            Debug.LogError($"Error showing reflected shader dropdown: {originalException.GetType().Name}: {originalException.Message}\n{originalException.StackTrace}");
            reflectionFailed = true; Repaint();
        }
    }

    private void OnShaderSelectedFromDropdown(object shaderNameObj)
    {
        if (shaderNameObj is string shaderName && !string.IsNullOrEmpty(shaderName))
        {
            Shader selectedShader = Shader.Find(shaderName);
            if (isSelectingSourceShader) sourceShader = selectedShader;
            else targetShader = selectedShader;
            Repaint();
        }
    }

    private Shader FallbackShaderField(string label, Shader currentShader)
    {
        return EditorGUILayout.ObjectField(label, currentShader, typeof(Shader), false) as Shader;
    }

    void OnGUI()
    {
        if (serializedObjectInstance == null)
        {
            serializedObjectInstance = new SerializedObject(this);
            excludedGameObjectsProp = serializedObjectInstance.FindProperty("excludedGameObjects");
        }
        serializedObjectInstance.Update();
        EditorGUILayout.HelpBox("Replaces shaders on materials. Always back up your project first!", MessageType.Info);

        if (!reflectionInitialized) InitializeReflection();

        EditorGUILayout.Space();
        GUILayout.Label("Processing Scope", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        currentScope = (ProcessingScope)EditorGUILayout.EnumPopup("Scope", currentScope);
        bool scopeChanged = EditorGUI.EndChangeCheck();

        if (currentScope == ProcessingScope.GameObjectAndChildren)
        {
            rootGameObjectForScope = EditorGUILayout.ObjectField("Root GameObject", rootGameObjectForScope, typeof(GameObject), true) as GameObject;
        }
        EditorGUILayout.Space();

        GUILayout.Label("Shader Selection", EditorStyles.boldLabel);
        if (reflectionFailed)
        {
            EditorGUILayout.HelpBox("Advanced shader dropdown failed. Using basic ObjectFields.", MessageType.Warning);
            sourceShader = FallbackShaderField("Source Shader", sourceShader);
            targetShader = FallbackShaderField("Target Shader", targetShader);
        }
        else
        {
            DrawShaderDropdown("Source Shader", ref sourceShader, true);
            DrawShaderDropdown("Target Shader", ref targetShader, false);
        }
        EditorGUILayout.Space();

        GUILayout.Label("GameObject Exclusion Filter", EditorStyles.boldLabel);
        
        bool exclusionUIEnabled = currentScope == ProcessingScope.GameObjectAndChildren;
        using (new EditorGUI.DisabledScope(!exclusionUIEnabled))
        {
            EditorGUILayout.PropertyField(excludedGameObjectsProp, new GUIContent("Excluded GameObjects"), true);
        }

        if (currentScope == ProcessingScope.EntireProject)
        {
             EditorGUILayout.HelpBox("Exclusion filter is disabled for 'Entire Project' scope. It primarily affects 'GameObject And Children' scope.", MessageType.None);
        }
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Replace Shaders", GUILayout.Height(30)))
        {
            if (ValidateInputs())
            {
                if (EditorUtility.DisplayDialog("Confirm Shader Replacement",
                    $"Are you sure you want to replace all materials using '{sourceShader.name}' with '{targetShader.name}' " +
                    (currentScope == ProcessingScope.EntireProject ? "in the entire project" : $"under '{rootGameObjectForScope?.name ?? "Unassigned Root"}'") + "?\n" +
                    "This action can be undone for assets, but scene changes need scene undo/revert. BACKUP YOUR PROJECT.",
                    "Yes, Replace", "Cancel"))
                {
                    PerformShaderReplacement();
                }
            }
        }
        EditorGUILayout.Space();

        GUILayout.Label("Log of Changed Materials:", EditorStyles.boldLabel);
        logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.ExpandHeight(true));
        if (changedMaterialsLog.Count == 0) GUILayout.Label("No materials changed yet.");
        else foreach (string logEntry in changedMaterialsLog) EditorGUILayout.SelectableLabel(logEntry, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndScrollView();

        serializedObjectInstance.ApplyModifiedProperties();
    }

    private void DrawShaderDropdown(string label, ref Shader shader, bool isSource)
    {
        GUIContent buttonContent = new GUIContent(shader != null ? shader.name : "(None)");
        Rect rect = EditorGUILayout.GetControlRect();
        rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTempContent(label));
        if (EditorGUI.DropdownButton(rect, buttonContent, FocusType.Keyboard))
        {
            isSelectingSourceShader = isSource;
            ShowReflectedShaderDropdown(rect, shader);
        }
    }

    private bool ValidateInputs()
    {
        if (sourceShader == null) { EditorUtility.DisplayDialog("Error", "Source Shader not selected!", "OK"); return false; }
        if (targetShader == null) { EditorUtility.DisplayDialog("Error", "Target Shader not selected!", "OK"); return false; }
        if (sourceShader == targetShader) { EditorUtility.DisplayDialog("Error", "Source and Target shaders are the same!", "OK"); return false; }
        if (currentScope == ProcessingScope.GameObjectAndChildren && rootGameObjectForScope == null)
        {
            EditorUtility.DisplayDialog("Error", "Root GameObject not selected for the current scope!", "OK");
            return false;
        }
        return true;
    }

    private void PerformShaderReplacement()
    {
        changedMaterialsLog.Clear();
        int materialsChangedCount = 0;
        HashSet<UnityEngine.SceneManagement.Scene> modifiedScenes = new HashSet<UnityEngine.SceneManagement.Scene>();

        try
        {
            if (currentScope == ProcessingScope.EntireProject)
            {
                materialsChangedCount = ProcessProjectMaterials();
            }
            else
            {
                materialsChangedCount = ProcessGameObjectMaterials(rootGameObjectForScope, modifiedScenes);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (materialsChangedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string sceneMessage = "";
            if (modifiedScenes.Count > 0)
            {
                foreach (var scene in modifiedScenes)
                {
                    if (scene.IsValid() && scene.isLoaded) EditorSceneManager.MarkSceneDirty(scene);
                }
                sceneMessage = $"\n{modifiedScenes.Count} scene(s) were modified. Remember to save them.";
            }
            EditorUtility.DisplayDialog("Success", $"Successfully changed shader on {materialsChangedCount} material instance(s).{sceneMessage}\nCheck the log in the tool window.", "OK");
        }
        else if (!changedMaterialsLog.Any(log => log.Contains("Operation cancelled")))
        {
            EditorUtility.DisplayDialog("No Changes", "No materials found matching the criteria, or operation was cancelled.", "OK");
        }
        Repaint();
    }

    private int ProcessProjectMaterials()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            bool cancelled = EditorUtility.DisplayCancelableProgressBar("Replacing Shaders (Project)", $"Processing Asset: {material?.name ?? "Unknown"}", (float)i / guids.Length);
            if (cancelled) { changedMaterialsLog.Add("Operation cancelled by user."); break; }

            if (material != null && material.shader == sourceShader)
            {
                Undo.RecordObject(material, "Change Shader on Asset");
                material.shader = targetShader;
                EditorUtility.SetDirty(material);
                changedMaterialsLog.Add($"Changed ASSET: {assetPath} (Shader: {sourceShader.name} -> {targetShader.name})");
                count++;
            }
        }
        return count;
    }

    private int ProcessGameObjectMaterials(GameObject root, HashSet<UnityEngine.SceneManagement.Scene> modifiedScenes)
    {
        int count = 0;
        if (root == null) return 0;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            bool cancelled = EditorUtility.DisplayCancelableProgressBar("Replacing Shaders (GameObject Scope)", $"Processing Renderer: {renderer.name}", (float)i / renderers.Length);
            if (cancelled) { changedMaterialsLog.Add("Operation cancelled by user."); break; }

            if (IsGameObjectExcluded(renderer.gameObject))
            {
                changedMaterialsLog.Add($"SKIPPED (Excluded): {GetGameObjectPath(renderer.gameObject)}");
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            for (int j = 0; j < sharedMaterials.Length; j++)
            {
                Material mat = sharedMaterials[j];
                if (mat != null && mat.shader == sourceShader)
                {
                    Undo.RecordObject(mat, "Change Shader on Material Instance/Asset");
                    mat.shader = targetShader;
                    EditorUtility.SetDirty(mat);

                    if (!EditorUtility.IsPersistent(mat) && renderer.gameObject.scene.IsValid())
                    {
                        EditorUtility.SetDirty(renderer);
                        modifiedScenes.Add(renderer.gameObject.scene);
                    }
                    
                    changedMaterialsLog.Add($"Changed on '{GetGameObjectPath(renderer.gameObject)}' (Material: {mat.name}, Index: {j}): {sourceShader.name} -> {targetShader.name}");
                    count++;
                }
            }
        }
        return count;
    }

    private bool IsGameObjectExcluded(GameObject go)
    {
        if (currentScope == ProcessingScope.EntireProject) return false; 

        if (go == null || excludedGameObjects == null || excludedGameObjects.Count == 0)
            return false;

        List<GameObject> validExclusions = excludedGameObjects.Where(e => e != null).ToList();
        if (validExclusions.Count == 0) return false;

        Transform currentTransform = go.transform;
        while (currentTransform != null)
        {
            if (validExclusions.Contains(currentTransform.gameObject))
            {
                return true;
            }

            if (currentTransform == rootGameObjectForScope?.transform && !validExclusions.Contains(rootGameObjectForScope))
            {
                break;
            }
            currentTransform = currentTransform.parent;
        }
        return false;
    }

    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return string.Empty;
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}