using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
namespace RuaWorks.EditorExtensionTools
{
    public class AssetReferenceSearcher : EditorWindow
    {
        private string targetFolder = "Assets/";
        private Vector2 scrollPos;
        private List<ReferenceInfo> foundReferences = new List<ReferenceInfo>();
        private bool isScanned = false;
        private bool showOnlyReferenced = false;

        private class ReferenceInfo
        {
            public GameObject gameObject;
            public Component component;
            public string propertyPath;
            public Object referencedAsset;
            public string assetPath;
        }

        [MenuItem("Tools/RuaWorks/Asset Reference Searcher")]
        static void Init()
        {
            AssetReferenceSearcher window = (AssetReferenceSearcher)EditorWindow.GetWindow(typeof(AssetReferenceSearcher));
            window.titleContent = new GUIContent("Asset Reference Searcher");
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Asset Reference Searcher", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("指定したフォルダ内のアセットを参照しているシーン上のオブジェクトを検索します。", MessageType.Info);
            EditorGUILayout.Space();

            // フォルダ選択
            EditorGUILayout.BeginHorizontal();
            targetFolder = EditorGUILayout.TextField("Target Folder", targetFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        targetFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Please select a folder inside the Assets directory", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // スキャンボタン
            if (GUILayout.Button("Scan Scene References", GUILayout.Height(30)))
            {
                ScanSceneReferences();
            }

            EditorGUILayout.Space();

            // 結果表示
            if (isScanned)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Found {foundReferences.Count} reference(s)", EditorStyles.boldLabel);

                EditorGUILayout.Space();

                showOnlyReferenced = EditorGUILayout.ToggleLeft("Referenced Only", showOnlyReferenced, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();

                if (foundReferences.Count > 0)
                {
                    EditorGUILayout.Space();

                    // エクスポートボタン
                    if (GUILayout.Button("Export to CSV"))
                    {
                        ExportToCSV();
                    }

                    EditorGUILayout.Space();

                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                    var displayReferences = showOnlyReferenced
                        ? foundReferences
                        : foundReferences;

                    foreach (var refInfo in displayReferences)
                    {
                        EditorGUILayout.BeginVertical(GUI.skin.box);

                        // GameObject
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("GameObject:", GUILayout.Width(100));
                        EditorGUILayout.ObjectField(refInfo.gameObject, typeof(GameObject), true);
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeGameObject = refInfo.gameObject;
                            EditorGUIUtility.PingObject(refInfo.gameObject);
                        }
                        EditorGUILayout.EndHorizontal();

                        // Component
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Component:", GUILayout.Width(100));
                        EditorGUILayout.ObjectField(refInfo.component, refInfo.component.GetType(), true);
                        EditorGUILayout.EndHorizontal();

                        // Property
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Property:", GUILayout.Width(100));
                        GUILayout.Label(refInfo.propertyPath, EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();

                        // Referenced Asset
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Asset:", GUILayout.Width(100));
                        EditorGUILayout.ObjectField(refInfo.referencedAsset, typeof(Object), false);
                        if (GUILayout.Button("Ping", GUILayout.Width(60)))
                        {
                            EditorGUIUtility.PingObject(refInfo.referencedAsset);
                        }
                        EditorGUILayout.EndHorizontal();

                        // Asset Path
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Path:", GUILayout.Width(100));
                        EditorGUILayout.SelectableLabel(refInfo.assetPath, EditorStyles.miniLabel, GUILayout.Height(16));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }

                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("指定したフォルダ内のアセットへの参照は見つかりませんでした。", MessageType.Info);
                }
            }
        }

        void ScanSceneReferences()
        {
            foundReferences.Clear();
            isScanned = false;

            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                EditorUtility.DisplayDialog("Error", "指定されたフォルダが存在しません。", "OK");
                return;
            }

            // 現在のシーンのすべてのGameObjectを取得
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            List<GameObject> allObjects = new List<GameObject>();

            foreach (var root in rootObjects)
            {
                allObjects.Add(root);
                allObjects.AddRange(root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));
            }

            int totalObjects = allObjects.Count;
            int current = 0;

            // 各オブジェクトの参照をチェック
            foreach (var obj in allObjects)
            {
                current++;
                EditorUtility.DisplayProgressBar("Scanning Scene", $"Checking {obj.name}...", (float)current / totalObjects);

                Component[] components = obj.GetComponents<Component>();

                foreach (var component in components)
                {
                    if (component == null) continue;

                    SerializedObject so = new SerializedObject(component);
                    SerializedProperty prop = so.GetIterator();

                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (prop.objectReferenceValue != null)
                            {
                                string assetPath = AssetDatabase.GetAssetPath(prop.objectReferenceValue);

                                // 指定フォルダ内のアセットかチェック
                                if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(targetFolder))
                                {
                                    ReferenceInfo refInfo = new ReferenceInfo
                                    {
                                        gameObject = obj,
                                        component = component,
                                        propertyPath = prop.propertyPath,
                                        referencedAsset = prop.objectReferenceValue,
                                        assetPath = assetPath
                                    };

                                    foundReferences.Add(refInfo);
                                }
                            }
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            isScanned = true;

            Debug.Log($"Scan completed. Found {foundReferences.Count} reference(s) to assets in '{targetFolder}'.");
        }

        void ExportToCSV()
        {
            string path = EditorUtility.SaveFilePanel("Export References to CSV", "", "asset_references.csv", "csv");

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
                {
                    // ヘッダー
                    writer.WriteLine("GameObject,Component,Property,Asset Type,Asset Path");

                    // データ
                    foreach (var refInfo in foundReferences)
                    {
                        string gameObjectName = refInfo.gameObject.name;
                        string componentName = refInfo.component.GetType().Name;
                        string propertyPath = refInfo.propertyPath;
                        string assetType = refInfo.referencedAsset.GetType().Name;
                        string assetPath = refInfo.assetPath;

                        writer.WriteLine($"\"{gameObjectName}\",\"{componentName}\",\"{propertyPath}\",\"{assetType}\",\"{assetPath}\"");
                    }
                }

                EditorUtility.DisplayDialog("Success", $"CSVファイルをエクスポートしました:\n{path}", "OK");
                EditorUtility.RevealInFinder(path);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"エクスポートに失敗しました:\n{e.Message}", "OK");
            }
        }
    }
}
