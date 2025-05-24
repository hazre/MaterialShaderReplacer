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

namespace dev.hazre.materialshaderreplacer
{
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
		private bool sourceShaderIsEverything = false;
		private Vector2 logScrollPosition;
		private List<string> changedMaterialsLog = new List<string>();

		private static Type shaderSelectionDropdownType;
		private static ConstructorInfo shaderSelectionDropdownConstructor;
		private static MethodInfo shaderSelectionDropdownShowMethod;
		private static Type advancedDropdownStateType;
		private static bool reflectionInitialized = false;
		private static bool reflectionFailed = false;

		private bool isSelectingSourceShader;
		private bool showAdvancedOptions = false;

		[MenuItem("Tools/Material Shader Replacer")]
		public static void ShowWindow()
		{
			GetWindow<ShaderReplacerTool>(MSRL10N.Tr("MaterialShaderReplacer:Window:Title")).minSize = new Vector2(350, 300);
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
				Assembly editorAssembly = typeof(EditorGUI).Assembly;
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
				Debug.LogError(MSRL10N.Tr("MaterialShaderReplacer:Error:ReflectionInitFailed") + e.Message);
				reflectionFailed = true;
				reflectionInitialized = true;
			}
		}

		private void ShowReflectedShaderDropdown(Rect buttonRect, Shader currentShader)
		{
			if (reflectionFailed || !reflectionInitialized) { Debug.LogError(MSRL10N.Tr("MaterialShaderReplacer:Error:ReflectionNotReady")); return; }
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
				Debug.LogError(MSRL10N.Tr("MaterialShaderReplacer:Error:ShowShaderDropdownFailed") + $"{originalException.GetType().Name}: {originalException.Message}\n{originalException.StackTrace}");
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
			EditorGUILayout.HelpBox(MSRL10N.Tr("MaterialShaderReplacer:HelpBox:BackupProject"), MessageType.Info);

			MSRL10N.DrawLanguagePicker();

			if (!reflectionInitialized) InitializeReflection();

			EditorGUILayout.Space();
			GUILayout.Label(MSRL10N.Tr("MaterialShaderReplacer:Label:ProcessingScope"), EditorStyles.boldLabel);
			EditorGUI.BeginChangeCheck();

			string[] scopeOptions = new string[]
			{
								MSRL10N.Tr("MaterialShaderReplacer:Enum:ProcessingScope:EntireProject"),
								MSRL10N.Tr("MaterialShaderReplacer:Enum:ProcessingScope:GameObjectAndChildren")
			};
			int currentScopeIndex = (int)currentScope;
			int newScopeIndex = EditorGUILayout.Popup(MSRL10N.Tr("MaterialShaderReplacer:Label:Scope"), currentScopeIndex, scopeOptions);

			bool scopeChanged = EditorGUI.EndChangeCheck();
			if (newScopeIndex != currentScopeIndex)
			{
				currentScope = (ProcessingScope)newScopeIndex;
				scopeChanged = true;
			}

			if (currentScope == ProcessingScope.GameObjectAndChildren)
			{
				rootGameObjectForScope = EditorGUILayout.ObjectField(MSRL10N.Tr("MaterialShaderReplacer:Label:RootGameObject"), rootGameObjectForScope, typeof(GameObject), true) as GameObject;
			}
			EditorGUILayout.Space();

			GUILayout.Label(MSRL10N.Tr("MaterialShaderReplacer:Label:ShaderSelection"), EditorStyles.boldLabel);

			if (reflectionFailed)
			{
				EditorGUILayout.HelpBox(MSRL10N.Tr("MaterialShaderReplacer:HelpBox:AdvancedDropdownFailed"), MessageType.Warning);
				using (new EditorGUI.DisabledScope(sourceShaderIsEverything))
				{
					sourceShader = FallbackShaderField(MSRL10N.Tr("MaterialShaderReplacer:Label:SourceShader"), sourceShader);
				}
				targetShader = FallbackShaderField(MSRL10N.Tr("MaterialShaderReplacer:Label:TargetShader"), targetShader);
			}
			else
			{
				DrawShaderDropdown(MSRL10N.Tr("MaterialShaderReplacer:Label:SourceShader"), ref sourceShader, true);
				DrawShaderDropdown(MSRL10N.Tr("MaterialShaderReplacer:Label:TargetShader"), ref targetShader, false);
			}

			showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, MSRL10N.Tr("MaterialShaderReplacer:Label:AdvancedOptions"));
			if (showAdvancedOptions)
			{
				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				sourceShaderIsEverything = EditorGUILayout.ToggleLeft(
						new GUIContent(MSRL10N.Tr("MaterialShaderReplacer:Toggle:SourceAllMaterials"), MSRL10N.Tr("MaterialShaderReplacer:Tooltip:SourceAllMaterials")),
						sourceShaderIsEverything
				);
				if (EditorGUI.EndChangeCheck())
				{
					if (sourceShaderIsEverything)
					{
						sourceShader = null;
					}
					Repaint();
				}

				if (sourceShaderIsEverything)
				{
					EditorGUILayout.HelpBox(MSRL10N.Tr("MaterialShaderReplacer:Warning:SourceAllMaterials"), MessageType.Warning);
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();

			GUILayout.Label(MSRL10N.Tr("MaterialShaderReplacer:Label:GameObjectExclusionFilter"), EditorStyles.boldLabel);

			bool exclusionUIEnabled = currentScope == ProcessingScope.GameObjectAndChildren;
			using (new EditorGUI.DisabledScope(!exclusionUIEnabled))
			{
				EditorGUILayout.PropertyField(excludedGameObjectsProp, new GUIContent(MSRL10N.Tr("MaterialShaderReplacer:Label:ExcludedGameObjects")), true);
			}

			if (currentScope == ProcessingScope.EntireProject)
			{
				EditorGUILayout.HelpBox(MSRL10N.Tr("MaterialShaderReplacer:HelpBox:ExclusionFilterDisabled"), MessageType.None);
			}

			EditorGUILayout.Space();

			if (GUILayout.Button(MSRL10N.Tr("MaterialShaderReplacer:Button:ReplaceShaders"), GUILayout.Height(30)))
			{
				if (ValidateInputs())
				{
					string dialogTitle = MSRL10N.Tr("MaterialShaderReplacer:Dialog:ConfirmReplacement:Title");
					string scopeDescription = currentScope == ProcessingScope.EntireProject ?
							MSRL10N.Tr("MaterialShaderReplacer:Dialog:ConfirmReplacement:ScopeProject") :
							string.Format(MSRL10N.Tr("MaterialShaderReplacer:Dialog:ConfirmReplacement:ScopeGameObjectFormat"), rootGameObjectForScope?.name ?? MSRL10N.Tr("MaterialShaderReplacer:Text:UnassignedRoot"));

					string query;
					string warning;

					if (sourceShaderIsEverything)
					{
						string targetShaderName = targetShader != null ? targetShader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None");
						query = string.Format(MSRL10N.Tr("MaterialShaderReplacer:Dialog:ConfirmReplacement:QueryEverythingFormat"), targetShaderName, scopeDescription);
						warning = MSRL10N.Tr("MaterialShaderReplacer:Dialog:ConfirmReplacement:WarningEverything");
					}
					else
					{
						string sourceShaderName = sourceShader != null ? sourceShader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None");
						string targetShaderName = targetShader != null ? targetShader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None");
						query = string.Format(MSRL10N.Tr("MaterialShaderReplacer:Dialog:ConfirmReplacement:QueryFormat"), sourceShaderName, targetShaderName, scopeDescription);
						warning = MSRL10N.Tr("MaterialShaderReplacer:Dialog:ConfirmReplacement:Warning");
					}
					string message = query + "\n\n" + warning;

					if (EditorUtility.DisplayDialog(dialogTitle,
							message,
							MSRL10N.Tr("MaterialShaderReplacer:Button:YesReplace"), MSRL10N.Tr("MaterialShaderReplacer:Button:Cancel")))
					{
						PerformShaderReplacement();
					}
				}
			}
			EditorGUILayout.Space();

			GUILayout.Label(MSRL10N.Tr("MaterialShaderReplacer:Label:LogChangedMaterials"), EditorStyles.boldLabel);
			logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.ExpandHeight(true));
			if (changedMaterialsLog.Count == 0) GUILayout.Label(MSRL10N.Tr("MaterialShaderReplacer:Label:NoMaterialsChanged"));
			else foreach (string logEntry in changedMaterialsLog) EditorGUILayout.SelectableLabel(logEntry, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
			EditorGUILayout.EndScrollView();

			serializedObjectInstance.ApplyModifiedProperties();
		}

		private void DrawShaderDropdown(string label, ref Shader shader, bool isSource)
		{
			GUIContent buttonContent;
			bool isEverythingSourceMode = isSource && sourceShaderIsEverything;

			if (isEverythingSourceMode)
			{
				buttonContent = new GUIContent(MSRL10N.Tr("MaterialShaderReplacer:Text:SourceEverything"));
			}
			else
			{
				buttonContent = new GUIContent(shader != null ? shader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None"));
			}

			Rect rect = EditorGUILayout.GetControlRect();
			rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTempContent(label));

			using (new EditorGUI.DisabledScope(isEverythingSourceMode))
			{
				if (EditorGUI.DropdownButton(rect, buttonContent, FocusType.Keyboard))
				{
					if (!isEverythingSourceMode)
					{
						isSelectingSourceShader = isSource;
						ShowReflectedShaderDropdown(rect, shader);
					}
				}
			}
		}

		private bool ValidateInputs()
		{
			string errorTitle = MSRL10N.Tr("MaterialShaderReplacer:Dialog:Error:Title");
			string okButton = MSRL10N.Tr("MaterialShaderReplacer:Button:OK");

			if (!sourceShaderIsEverything && sourceShader == null)
			{
				EditorUtility.DisplayDialog(errorTitle, MSRL10N.Tr("MaterialShaderReplacer:Dialog:Error:SourceShaderMissing"), okButton); return false;
			}
			if (targetShader == null)
			{
				EditorUtility.DisplayDialog(errorTitle, MSRL10N.Tr("MaterialShaderReplacer:Dialog:Error:TargetShaderMissing"), okButton); return false;
			}
			if (!sourceShaderIsEverything && sourceShader == targetShader)
			{
				EditorUtility.DisplayDialog(errorTitle, MSRL10N.Tr("MaterialShaderReplacer:Dialog:Error:ShadersSame"), okButton); return false;
			}

			if (currentScope == ProcessingScope.GameObjectAndChildren && rootGameObjectForScope == null)
			{
				EditorUtility.DisplayDialog(errorTitle, MSRL10N.Tr("MaterialShaderReplacer:Dialog:Error:RootGameObjectMissing"), okButton);
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
					sceneMessage = string.Format(MSRL10N.Tr("MaterialShaderReplacer:Message:ScenesModifiedFormat"), modifiedScenes.Count);
				}
				EditorUtility.DisplayDialog(MSRL10N.Tr("MaterialShaderReplacer:Dialog:Success:Title"),
																		string.Format(MSRL10N.Tr("MaterialShaderReplacer:Dialog:Success:MessageFormat"), materialsChangedCount, sceneMessage),
																		MSRL10N.Tr("MaterialShaderReplacer:Button:OK"));
			}
			else if (!changedMaterialsLog.Any(log => log == MSRL10N.Tr("MaterialShaderReplacer:Log:OperationCancelledByUser")))
			{
				EditorUtility.DisplayDialog(MSRL10N.Tr("MaterialShaderReplacer:Dialog:NoChanges:Title"), MSRL10N.Tr("MaterialShaderReplacer:Dialog:NoChanges:Message"), MSRL10N.Tr("MaterialShaderReplacer:Button:OK"));
			}
			Repaint();
		}

		private int ProcessProjectMaterials()
		{
			int count = 0;
			string[] guids = AssetDatabase.FindAssets("t:Material");
			string progressBarTitle = MSRL10N.Tr("MaterialShaderReplacer:ProgressBar:ReplacingProject:Title");
			string operationCancelledMsg = MSRL10N.Tr("MaterialShaderReplacer:Log:OperationCancelledByUser");

			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

				string progressBarInfo = string.Format(MSRL10N.Tr("MaterialShaderReplacer:ProgressBar:ProcessingAsset:Format"), material?.name ?? MSRL10N.Tr("MaterialShaderReplacer:Text:Unknown"));
				bool cancelled = EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo, (float)i / guids.Length);
				if (cancelled) { changedMaterialsLog.Add(operationCancelledMsg); break; }

				if (material != null && (sourceShaderIsEverything || material.shader == sourceShader))
				{
					Undo.RecordObject(material, MSRL10N.Tr("MaterialShaderReplacer:Undo:ChangeShaderAsset"));
					material.shader = targetShader;
					EditorUtility.SetDirty(material);
					string sourceShaderNameForLog = sourceShaderIsEverything ? MSRL10N.Tr("MaterialShaderReplacer:Text:LogSourceEverything") : (sourceShader != null ? sourceShader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None"));
					string targetShaderNameForLog = targetShader != null ? targetShader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None");
					changedMaterialsLog.Add(string.Format(MSRL10N.Tr("MaterialShaderReplacer:Log:ChangedAsset:Format"), assetPath, sourceShaderNameForLog, targetShaderNameForLog));
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
			string progressBarTitle = MSRL10N.Tr("MaterialShaderReplacer:ProgressBar:ReplacingGameObject:Title");
			string operationCancelledMsg = MSRL10N.Tr("MaterialShaderReplacer:Log:OperationCancelledByUser");

			for (int i = 0; i < renderers.Length; i++)
			{
				Renderer renderer = renderers[i];
				string progressBarInfo = string.Format(MSRL10N.Tr("MaterialShaderReplacer:ProgressBar:ProcessingRenderer:Format"), renderer.name);
				bool cancelled = EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo, (float)i / renderers.Length);
				if (cancelled) { changedMaterialsLog.Add(operationCancelledMsg); break; }

				if (IsGameObjectExcluded(renderer.gameObject))
				{
					changedMaterialsLog.Add(string.Format(MSRL10N.Tr("MaterialShaderReplacer:Log:SkippedExcluded:Format"), GetGameObjectPath(renderer.gameObject)));
					continue;
				}

				Material[] sharedMaterials = renderer.sharedMaterials;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					Material mat = sharedMaterials[j];
					if (mat != null && (sourceShaderIsEverything || mat.shader == sourceShader))
					{
						Undo.RecordObject(mat, MSRL10N.Tr("MaterialShaderReplacer:Undo:ChangeShaderInstanceOrAsset"));
						mat.shader = targetShader;
						EditorUtility.SetDirty(mat);

						if (!EditorUtility.IsPersistent(mat) && renderer.gameObject.scene.IsValid())
						{
							EditorUtility.SetDirty(renderer);
							modifiedScenes.Add(renderer.gameObject.scene);
						}

						string sourceShaderNameForLog = sourceShaderIsEverything ? MSRL10N.Tr("MaterialShaderReplacer:Text:LogSourceEverything") : (sourceShader != null ? sourceShader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None"));
						string targetShaderNameForLog = targetShader != null ? targetShader.name : MSRL10N.Tr("MaterialShaderReplacer:Text:None");
						changedMaterialsLog.Add(string.Format(MSRL10N.Tr("MaterialShaderReplacer:Log:ChangedOnGameObject:Format"), GetGameObjectPath(renderer.gameObject), mat.name, j, sourceShaderNameForLog, targetShaderNameForLog));
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
}