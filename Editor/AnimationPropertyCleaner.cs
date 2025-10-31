using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace RuaWorks.EditorExtensionTools
{
    public class AnimationPropertyCleaner : EditorWindow
    {
        private AnimationClip sourceClip;
        private List<AnimationClip> targetClips = new List<AnimationClip>();
        private Vector2 scrollPosition;
        private bool foldoutTargets = true;
        private Rect dropAreaSource;
        private Rect dropAreaTargets;

        [MenuItem("Tools/RuaWorks/Animation Property Cleaner")]
        public static void ShowWindow()
        {
            GetWindow<AnimationPropertyCleaner>("Animation Property Cleaner");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Animation Property Cleaner", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Target - SourceをTargetに上書きします");
            EditorGUILayout.Space();

            // ソースクリップ用のドラッグ＆ドロップエリア
            EditorGUILayout.LabelField("Source Clip (A)");
            DrawSourceDropArea();
            sourceClip = EditorGUILayout.ObjectField(sourceClip, typeof(AnimationClip), false) as AnimationClip;

            EditorGUILayout.Space();

            // ターゲットクリップ用のドラッグ＆ドロップエリア
            EditorGUILayout.LabelField("Target Clips (B) - ドラッグ＆ドロップで複数追加可能");
            DrawTargetDropArea();

            // Target Clips リスト表示用の折りたたみセクション
            foldoutTargets = EditorGUILayout.Foldout(foldoutTargets, $"Target Clips ({targetClips.Count})");
            if (foldoutTargets)
            {
                EditorGUI.indentLevel++;

                // スクロールビューの開始
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

                // ターゲットクリップのリストを表示
                for (int i = 0; i < targetClips.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    targetClips[i] = EditorGUILayout.ObjectField($"Target Clip {i + 1}", targetClips[i], typeof(AnimationClip), false) as AnimationClip;

                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        targetClips.RemoveAt(i);
                        GUIUtility.ExitGUI(); // GUI更新のためにイベントを中断
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                // 追加ボタン
                if (GUILayout.Button("Add Target Clip"))
                {
                    targetClips.Add(null);
                }

                // すべてクリアボタン
                if (targetClips.Count > 0 && GUILayout.Button("Clear All"))
                {
                    targetClips.Clear();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // 処理実行ボタン
            GUI.enabled = sourceClip != null && targetClips.Count > 0 && !targetClips.Contains(null);
            if (GUILayout.Button("Remove Duplicate Properties"))
            {
                if (sourceClip == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign source animation clip.", "OK");
                    return;
                }

                if (targetClips.Count == 0 || targetClips.Contains(null))
                {
                    EditorUtility.DisplayDialog("Error", "Please assign at least one target animation clip.", "OK");
                    return;
                }

                RemoveDuplicateProperties();
            }
            GUI.enabled = true;
        }

        private void DrawSourceDropArea()
        {
            Event evt = Event.current;
            GUI.Box(dropAreaSource = EditorGUILayout.GetControlRect(GUILayout.Height(35)), "ドラッグ＆ドロップでSourceクリップを指定", EditorStyles.helpBox);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropAreaSource.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            AnimationClip clip = draggedObject as AnimationClip;
                            if (clip != null)
                            {
                                sourceClip = clip;
                                break; // 最初の有効なクリップだけを使用
                            }
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        private void DrawTargetDropArea()
        {
            Event evt = Event.current;
            GUI.Box(dropAreaTargets = EditorGUILayout.GetControlRect(GUILayout.Height(35)), "ドラッグ＆ドロップで複数のTargetクリップを指定", EditorStyles.helpBox);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropAreaTargets.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            AnimationClip clip = draggedObject as AnimationClip;
                            if (clip != null && !targetClips.Contains(clip))
                            {
                                targetClips.Add(clip);
                            }
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        private void RemoveDuplicateProperties()
        {
            // sourceClipのバインディングを取得
            var sourceBindings = AnimationUtility.GetCurveBindings(sourceClip);
            int totalRemovedProperties = 0;

            // 各ターゲットクリップに対して処理を実行
            foreach (var targetClip in targetClips)
            {
                var targetBindings = AnimationUtility.GetCurveBindings(targetClip).ToList();

                // 削除する必要のあるバインディングを収集
                var bindingsToRemove = new List<EditorCurveBinding>();
                foreach (var sourceBinding in sourceBindings)
                {
                    var matchingBinding = targetBindings.FirstOrDefault(tb =>
                        tb.path == sourceBinding.path &&
                        tb.propertyName == sourceBinding.propertyName &&
                        tb.type == sourceBinding.type);

                    if (matchingBinding.path != null)
                    {
                        bindingsToRemove.Add(matchingBinding);
                    }
                }

                // 重複するプロパティを削除
                Undo.RecordObject(targetClip, "Remove Duplicate Animation Properties");
                foreach (var binding in bindingsToRemove)
                {
                    AnimationUtility.SetEditorCurve(targetClip, binding, null);
                }

                totalRemovedProperties += bindingsToRemove.Count;
            }

            EditorUtility.DisplayDialog("Complete",
                $"Removed {totalRemovedProperties} duplicate properties from {targetClips.Count} target clips.",
                "OK");
        }
    }
}
