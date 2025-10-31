using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace RuaWorks.EditorExtensionTools
{
    public class BlendShapeExporter : EditorWindow
    {
        private SkinnedMeshRenderer targetRenderer;
        private string fileName = "BlendShapeList";
        private bool includeIndex = true;
        private bool includeWeights = false;
        private Vector2 scrollPosition;

        [MenuItem("Tools/RuaWorks/BlendShape Exporter")]
        public static void ShowWindow()
        {
            GetWindow<BlendShapeExporter>("BlendShape Exporter");
        }

        private void OnGUI()
        {
            GUILayout.Label("BlendShape名前エクスポーター", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // SkinnedMeshRendererの選択
            targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Target Renderer",
                targetRenderer,
                typeof(SkinnedMeshRenderer),
                true
            );

            EditorGUILayout.Space();

            // オプション設定
            GUILayout.Label("エクスポート設定", EditorStyles.boldLabel);
            fileName = EditorGUILayout.TextField("ファイル名", fileName);
            includeIndex = EditorGUILayout.Toggle("インデックスを含める", includeIndex);
            includeWeights = EditorGUILayout.Toggle("現在のウェイトを含める", includeWeights);

            EditorGUILayout.Space();

            // プレビュー表示
            if (targetRenderer != null && targetRenderer.sharedMesh != null)
            {
                Mesh mesh = targetRenderer.sharedMesh;
                int blendShapeCount = mesh.blendShapeCount;

                GUILayout.Label($"BlendShape数: {blendShapeCount}", EditorStyles.helpBox);
                EditorGUILayout.Space();

                // プレビューエリア
                GUILayout.Label("プレビュー", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                for (int i = 0; i < blendShapeCount; i++)
                {
                    string blendShapeName = mesh.GetBlendShapeName(i);
                    string preview = GenerateLineText(i, blendShapeName, targetRenderer.GetBlendShapeWeight(i));
                    EditorGUILayout.LabelField(preview);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("SkinnedMeshRendererを選択してください", MessageType.Info);
            }

            EditorGUILayout.Space();

            // エクスポートボタン
            GUI.enabled = targetRenderer != null && targetRenderer.sharedMesh != null;

            if (GUILayout.Button("テキストファイルにエクスポート", GUILayout.Height(30)))
            {
                ExportBlendShapes();
            }

            GUI.enabled = true;
        }

        private string GenerateLineText(int index, string name, float weight)
        {
            StringBuilder sb = new StringBuilder();

            if (includeIndex)
            {
                sb.Append($"[{index}] ");
            }

            sb.Append(name);

            if (includeWeights)
            {
                sb.Append($" : {weight:F2}");
            }

            return sb.ToString();
        }

        private void ExportBlendShapes()
        {
            if (targetRenderer == null || targetRenderer.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("エラー", "有効なSkinnedMeshRendererが選択されていません", "OK");
                return;
            }

            // 保存先の選択
            string path = EditorUtility.SaveFilePanel(
                "BlendShape名前リストを保存",
                "Assets",
                fileName,
                "txt"
            );

            if (string.IsNullOrEmpty(path))
            {
                return; // キャンセルされた
            }

            try
            {
                Mesh mesh = targetRenderer.sharedMesh;
                int blendShapeCount = mesh.blendShapeCount;

                using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    // ヘッダー情報
                    writer.WriteLine($"# BlendShape List");
                    writer.WriteLine($"# Mesh: {mesh.name}");
                    writer.WriteLine($"# GameObject: {targetRenderer.gameObject.name}");
                    writer.WriteLine($"# Total Count: {blendShapeCount}");
                    writer.WriteLine($"# Export Date: {System.DateTime.Now}");
                    writer.WriteLine();

                    // BlendShape名前の出力
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        string blendShapeName = mesh.GetBlendShapeName(i);
                        float weight = targetRenderer.GetBlendShapeWeight(i);
                        string line = GenerateLineText(i, blendShapeName, weight);
                        writer.WriteLine(line);
                    }
                }

                EditorUtility.DisplayDialog(
                    "エクスポート完了",
                    $"{blendShapeCount}個のBlendShape名前を出力しました\n\n保存先: {path}",
                    "OK"
                );

                // ファイルを開く
                System.Diagnostics.Process.Start(path);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"ファイルの書き込みに失敗しました\n\n{e.Message}", "OK");
            }
        }
    }
}

