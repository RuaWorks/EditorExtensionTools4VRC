using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RuaWorks.EditorExtensionTools
{
    public class DeepCopyTool : EditorWindow
    {
        private GameObject targetObject;
        private string destinationFolder = "Assets/DeepCopy";
        private Vector2 scrollPos;
        private Dictionary<string, bool> assetSelections = new Dictionary<string, bool>();
        private List<string> foundAssets = new List<string>();
        private Dictionary<Object, Object> assetMapping = new Dictionary<Object, Object>();
        private bool isScanned = false;
        private bool addSuffix = false;
        private string suffix = "_Copy";

        [MenuItem("Tools/RuaWorks/Deep Copy Tool")]
        static void Init()
        {
            DeepCopyTool window = (DeepCopyTool)EditorWindow.GetWindow(typeof(DeepCopyTool));
            window.titleContent = new GUIContent("Deep Copy Tool");
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Deep Copy Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // �I�u�W�F�N�g�I��
            targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);

            EditorGUILayout.Space();

            // �R�s�[��t�H���_
            EditorGUILayout.BeginHorizontal();
            destinationFolder = EditorGUILayout.TextField("Destination Folder", destinationFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Destination Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        destinationFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Please select a folder inside the Assets directory", "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // �ڔ���I�v�V����
            addSuffix = EditorGUILayout.Toggle("Add Suffix to Assets", addSuffix);
            if (addSuffix)
            {
                EditorGUI.indentLevel++;
                suffix = EditorGUILayout.TextField("Suffix", suffix);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // �X�L�����{�^��
            GUI.enabled = targetObject != null;
            if (GUILayout.Button("Scan Dependencies", GUILayout.Height(30)))
            {
                ScanDependencies();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();

            // �A�Z�b�g�ꗗ�\��
            if (isScanned && foundAssets.Count > 0)
            {
                GUILayout.Label($"Found {foundAssets.Count} asset(s)", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All"))
                {
                    foreach (var key in foundAssets)
                    {
                        assetSelections[key] = true;
                    }
                }
                if (GUILayout.Button("Deselect All"))
                {
                    foreach (var key in foundAssets)
                    {
                        assetSelections[key] = false;
                    }
                }
                if (GUILayout.Button("Select Materials"))
                {
                    foreach (var key in foundAssets)
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath<Object>(key);
                        assetSelections[key] = asset is Material;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                foreach (var assetPath in foundAssets)
                {
                    EditorGUILayout.BeginHorizontal();
                    assetSelections[assetPath] = EditorGUILayout.ToggleLeft("", assetSelections[assetPath], GUILayout.Width(20));

                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    EditorGUILayout.ObjectField(asset, typeof(Object), false);

                    GUILayout.Label(assetPath, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                // �R�s�[���s�{�^��
                if (GUILayout.Button("Execute Deep Copy", GUILayout.Height(40)))
                {
                    ExecuteDeepCopy();
                }
            }
            else if (isScanned)
            {
                EditorGUILayout.HelpBox("No dependencies found.", MessageType.Info);
            }
        }

        void ScanDependencies()
        {
            foundAssets.Clear();
            assetSelections.Clear();
            isScanned = false;

            HashSet<string> assetPaths = new HashSet<string>();

            // �^�[�Q�b�g�I�u�W�F�N�g�Ƃ��̎q�����ׂĎ擾
            List<GameObject> allObjects = new List<GameObject> { targetObject };
            allObjects.AddRange(targetObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));

            foreach (var obj in allObjects)
            {
                // ���ׂẴR���|�[�l���g���擾
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
                                string path = AssetDatabase.GetAssetPath(prop.objectReferenceValue);

                                // �v���W�F�N�g�t�H���_���̃A�Z�b�g�̂�
                                if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
                                {
                                    assetPaths.Add(path);
                                }
                            }
                        }
                    }
                }
            }

            foundAssets = assetPaths.OrderBy(x => x).ToList();

            foreach (var path in foundAssets)
            {
                assetSelections[path] = true;
            }

            isScanned = true;
            Debug.Log($"Scan completed. Found {foundAssets.Count} asset(s).");
        }

        void ExecuteDeepCopy()
        {
            if (!AssetDatabase.IsValidFolder(destinationFolder))
            {
                // �t�H���_���쐬
                string[] folders = destinationFolder.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            assetMapping.Clear();
            int copiedCount = 0;

            // �I�����ꂽ�A�Z�b�g���R�s�[
            foreach (var kvp in assetSelections)
            {
                if (kvp.Value)
                {
                    string sourcePath = kvp.Key;
                    string fileName = Path.GetFileName(sourcePath);
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
                    string extension = Path.GetExtension(sourcePath);

                    // �ڔ����ǉ�
                    if (addSuffix && !string.IsNullOrEmpty(suffix))
                    {
                        fileName = fileNameWithoutExt + suffix + extension;
                    }

                    string destPath = Path.Combine(destinationFolder, fileName);

                    // �����t�@�C��������ꍇ�̓��j�[�N�Ȗ��O�𐶐�
                    destPath = AssetDatabase.GenerateUniqueAssetPath(destPath);

                    if (AssetDatabase.CopyAsset(sourcePath, destPath))
                    {
                        Object originalAsset = AssetDatabase.LoadAssetAtPath<Object>(sourcePath);
                        Object copiedAsset = AssetDatabase.LoadAssetAtPath<Object>(destPath);

                        assetMapping[originalAsset] = copiedAsset;
                        copiedCount++;
                        Debug.Log($"Copied: {sourcePath} -> {destPath}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // �Q�Ƃ�u������
            ReplaceReferences();

            EditorUtility.DisplayDialog("Success", $"Deep copy completed!\nCopied {copiedCount} asset(s).", "OK");
        }

        void ReplaceReferences()
        {
            List<GameObject> allObjects = new List<GameObject> { targetObject };
            allObjects.AddRange(targetObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject));

            foreach (var obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();

                foreach (var component in components)
                {
                    if (component == null) continue;

                    SerializedObject so = new SerializedObject(component);
                    SerializedProperty prop = so.GetIterator();

                    bool modified = false;

                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (prop.objectReferenceValue != null && assetMapping.ContainsKey(prop.objectReferenceValue))
                            {
                                prop.objectReferenceValue = assetMapping[prop.objectReferenceValue];
                                modified = true;
                            }
                        }
                    }

                    if (modified)
                    {
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(component);
                    }
                }
            }

            if (PrefabUtility.IsPartOfPrefabInstance(targetObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetObject);
            }

            Debug.Log("References updated.");
        }
    }
}
