using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RuaWorks.EditorExtensionTools
{
    public class MaterialComparisonTool : EditorWindow
    {
        [SerializeField]
        private SkinnedMeshRenderer targetRenderer;

        [SerializeField]
        private List<Material> materialList = new List<Material>();



        private Vector2 scrollPosition;
        private int currentMaterialIndex = -1;
        private Material originalMaterial;
        private bool hasBackup = false;

        [MenuItem("Tools/RuaWorks/Material Comparison Tool")]
        public static void ShowWindow()
        {
            GetWindow<MaterialComparisonTool>("Material Comparison");
        }

        private void OnEnable()
        {
            // ウィンドウが開かれた時の初期化
            if (materialList == null)
                materialList = new List<Material>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Material Comparison Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // ドラッグアンドドロップエリア
            EditorGUILayout.LabelField("Drag & Drop Materials", EditorStyles.boldLabel);
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag Materials Here to Add Multiple");

            HandleDragAndDrop(dropArea);

            EditorGUILayout.Space();

            // SkinnedMeshRenderer選択
            EditorGUILayout.LabelField("Target SkinnedMeshRenderer", EditorStyles.boldLabel);
            SkinnedMeshRenderer newRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Target Renderer", targetRenderer, typeof(SkinnedMeshRenderer), true);

            if (newRenderer != targetRenderer)
            {
                targetRenderer = newRenderer;
                BackupOriginalMaterial();
            }

            EditorGUILayout.Space();

            // マテリアル登録セクション
            EditorGUILayout.LabelField("Material Registration", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Material Slot"))
            {
                materialList.Add(null);
            }

            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear All Materials",
                    "Are you sure you want to clear all registered materials?", "Yes", "No"))
                {
                    materialList.Clear();
                    currentMaterialIndex = -1;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // マテリアルリスト表示
            if (materialList.Count > 0)
            {
                EditorGUILayout.LabelField("Registered Materials", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                for (int i = 0; i < materialList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    // インデックス表示
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));

                    // マテリアル選択
                    materialList[i] = (Material)EditorGUILayout.ObjectField(
                        materialList[i], typeof(Material), false, GUILayout.ExpandWidth(true));

                    // 適用ボタン
                    GUI.enabled = targetRenderer != null && materialList[i] != null;
                    if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    {
                        ApplyMaterial(i);
                    }
                    GUI.enabled = true;

                    // 削除ボタン
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        materialList.RemoveAt(i);
                        if (currentMaterialIndex == i)
                            currentMaterialIndex = -1;
                        else if (currentMaterialIndex > i)
                            currentMaterialIndex--;
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    // 現在適用中のマテリアルをハイライト
                    if (currentMaterialIndex == i)
                    {
                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        lastRect.x = 0;
                        lastRect.width = position.width;
                        EditorGUI.DrawRect(lastRect, new Color(0.3f, 0.7f, 1f, 0.3f));
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();

            // 操作ボタン
            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // 前のマテリアル
            GUI.enabled = targetRenderer != null && materialList.Count > 0 && HasValidMaterials();
            if (GUILayout.Button("◀ Previous Material"))
            {
                ApplyPreviousMaterial();
            }

            // 次のマテリアル
            if (GUILayout.Button("Next Material ▶"))
            {
                ApplyNextMaterial();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // オリジナルマテリアルに戻す
            GUI.enabled = targetRenderer != null && hasBackup;
            if (GUILayout.Button("Restore Original"))
            {
                RestoreOriginalMaterial();
            }
            GUI.enabled = true;

            // 現在のマテリアルをバックアップとして保存
            GUI.enabled = targetRenderer != null;
            if (GUILayout.Button("Backup Current"))
            {
                BackupOriginalMaterial();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 現在の状態表示
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
            if (targetRenderer != null)
            {
                string currentMaterialName = targetRenderer.sharedMaterial != null ?
                    targetRenderer.sharedMaterial.name : "None";
                EditorGUILayout.LabelField($"Current Material: {currentMaterialName}");

                if (currentMaterialIndex >= 0 && currentMaterialIndex < materialList.Count)
                {
                    EditorGUILayout.LabelField($"Active Slot: [{currentMaterialIndex}]");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Please select a SkinnedMeshRenderer to begin comparison.", MessageType.Info);
            }
        }

        private void ApplyMaterial(int index)
        {
            if (targetRenderer == null || index < 0 || index >= materialList.Count || materialList[index] == null)
                return;

            Undo.RecordObject(targetRenderer, "Apply Material");
            targetRenderer.sharedMaterial = materialList[index];
            currentMaterialIndex = index;

            // シーンビューを更新
            SceneView.RepaintAll();

            Debug.Log($"Applied material: {materialList[index].name}");
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    bool validDrag = false;
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is Material)
                        {
                            validDrag = true;
                            break;
                        }
                    }

                    if (validDrag)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            foreach (Object draggedObject in DragAndDrop.objectReferences)
                            {
                                if (draggedObject is Material material)
                                {
                                    materialList.Add(material);
                                }
                            }

                            Debug.Log($"Added {DragAndDrop.objectReferences.Length} materials via drag and drop");
                        }
                    }

                    Event.current.Use();
                    break;
            }
        }

        private void ApplyNextMaterial()
        {
            if (materialList.Count == 0) return;

            int nextIndex = currentMaterialIndex + 1;

            // 有効なマテリアルを探す
            for (int i = 0; i < materialList.Count; i++)
            {
                int checkIndex = (nextIndex + i) % materialList.Count;
                if (materialList[checkIndex] != null)
                {
                    ApplyMaterial(checkIndex);
                    return;
                }
            }
        }

        private void ApplyPreviousMaterial()
        {
            if (materialList.Count == 0) return;

            int prevIndex = currentMaterialIndex - 1;
            if (prevIndex < 0) prevIndex = materialList.Count - 1;

            // 有効なマテリアルを探す
            for (int i = 0; i < materialList.Count; i++)
            {
                int checkIndex = (prevIndex - i + materialList.Count) % materialList.Count;
                if (materialList[checkIndex] != null)
                {
                    ApplyMaterial(checkIndex);
                    return;
                }
            }
        }

        private void BackupOriginalMaterial()
        {
            if (targetRenderer != null)
            {
                originalMaterial = targetRenderer.sharedMaterial;
                hasBackup = true;
            }
        }

        private void RestoreOriginalMaterial()
        {
            if (targetRenderer != null && hasBackup)
            {
                Undo.RecordObject(targetRenderer, "Restore Original Material");
                targetRenderer.sharedMaterial = originalMaterial;
                currentMaterialIndex = -1;
                SceneView.RepaintAll();

                Debug.Log("Restored original material");
            }
        }

        private bool HasValidMaterials()
        {
            foreach (Material mat in materialList)
            {
                if (mat != null) return true;
            }
            return false;
        }

        private void OnSelectionChange()
        {
            // 選択変更時の処理（現在は無効化）
            Repaint();
        }
    }
}
