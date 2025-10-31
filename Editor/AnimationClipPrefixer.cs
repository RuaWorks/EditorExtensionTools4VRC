using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace RuaWorks.EditorExtensionTools
{
    /// <summary>
    /// エディタ拡張：AnimationClipのパスに接頭辞を追加するツール
    /// </summary>
    public class AnimationPathPrefixTool : EditorWindow
    {
        private AnimationClip targetClip;
        private string prefixToAdd = "";
        private Vector2 scrollPosition;
        private bool showPreview = false;
        private List<string> pathsPreview = new List<string>();
        private List<string> newPathsPreview = new List<string>();

        [MenuItem("Tools/RuaWorks/Animation Path Prefix Tool")]
        public static void ShowWindow()
        {
            GetWindow<AnimationPathPrefixTool>("Animation Path Prefix Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Path Prefix Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("このツールはAnimationClipのすべてのパスの先頭に指定した文字列を追加します。", MessageType.Info);
            EditorGUILayout.Space();

            // AnimationClipの選択
            targetClip = EditorGUILayout.ObjectField("Target Animation Clip", targetClip, typeof(AnimationClip), false) as AnimationClip;

            // 接頭辞の入力
            prefixToAdd = EditorGUILayout.TextField("追加する接頭辞", prefixToAdd);

            EditorGUILayout.Space();

            if (targetClip != null)
            {
                // プレビューボタン
                if (GUILayout.Button("パスをプレビュー"))
                {
                    GeneratePathsPreview();
                    showPreview = true;
                }

                // プレビュー表示
                if (showPreview && pathsPreview.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("プレビュー:", EditorStyles.boldLabel);

                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    for (int i = 0; i < pathsPreview.Count; i++)
                    {
                        EditorGUILayout.LabelField("元のパス: " + pathsPreview[i]);
                        EditorGUILayout.LabelField("新しいパス: " + newPathsPreview[i], EditorStyles.boldLabel);

                        if (i < pathsPreview.Count - 1)
                            EditorGUILayout.Space();
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.Space();

                // 実行ボタン
                GUI.enabled = !string.IsNullOrEmpty(prefixToAdd);
                if (GUILayout.Button("接頭辞を追加"))
                {
                    AddPrefixToAnimationPaths();
                }
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox("AnimationClipを選択してください。", MessageType.Warning);
            }
        }

        /// <summary>
        /// パスのプレビューを生成
        /// </summary>
        private void GeneratePathsPreview()
        {
            pathsPreview.Clear();
            newPathsPreview.Clear();

            if (targetClip == null)
                return;

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(targetClip);
            foreach (var binding in curveBindings)
            {
                pathsPreview.Add(binding.path);
                newPathsPreview.Add(prefixToAdd + binding.path);
            }

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(targetClip);
            foreach (var binding in objectBindings)
            {
                pathsPreview.Add(binding.path);
                newPathsPreview.Add(prefixToAdd + binding.path);
            }
        }

        /// <summary>
        /// AnimationClipのパスに接頭辞を追加
        /// </summary>
        private void AddPrefixToAnimationPaths()
        {
            if (targetClip == null || string.IsNullOrEmpty(prefixToAdd))
                return;

            // 変更前にUndo登録
            Undo.RecordObject(targetClip, "Add prefix to animation paths");

            // アニメーションカーブを処理
            ProcessAnimationCurves();

            // オブジェクト参照カーブを処理
            ProcessObjectReferenceCurves();

            // 変更を適用
            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();

            Debug.Log("AnimationClipのパスに接頭辞を追加しました: " + targetClip.name);

            // プレビューを更新
            if (showPreview)
            {
                GeneratePathsPreview();
            }
        }

        /// <summary>
        /// アニメーションカーブのパスを処理
        /// </summary>
        private void ProcessAnimationCurves()
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(targetClip);

            foreach (var binding in curveBindings)
            {
                // 現在のカーブを取得
                AnimationCurve curve = AnimationUtility.GetEditorCurve(targetClip, binding);

                // 元のバインディングを削除
                AnimationUtility.SetEditorCurve(targetClip, binding, null);

                // 新しいパスでバインディングを作成
                EditorCurveBinding newBinding = binding;
                newBinding.path = prefixToAdd + binding.path;

                // 新しいバインディングにカーブを設定
                AnimationUtility.SetEditorCurve(targetClip, newBinding, curve);
            }
        }

        /// <summary>
        /// オブジェクト参照カーブのパスを処理
        /// </summary>
        private void ProcessObjectReferenceCurves()
        {
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(targetClip);

            foreach (var binding in objectBindings)
            {
                // 現在のオブジェクト参照カーブを取得
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(targetClip, binding);

                // 元のバインディングを削除
                AnimationUtility.SetObjectReferenceCurve(targetClip, binding, null);

                // 新しいパスでバインディングを作成
                EditorCurveBinding newBinding = binding;
                newBinding.path = prefixToAdd + binding.path;

                // 新しいバインディングにキーフレームを設定
                AnimationUtility.SetObjectReferenceCurve(targetClip, newBinding, keyframes);
            }
        }
    }
}
