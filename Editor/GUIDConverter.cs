using UnityEngine;
using UnityEditor;
using System.IO;

namespace RuaWorks.EditorExtensionTools
{
    public class GUIDConverter : EditorWindow
    {
        private string guidInput = "";
        private string pathInput = "";
        private Object objectInput;

        private string convertedResult = "";
        private MessageType resultMessageType = MessageType.None;

        private Vector2 scrollPosition;
        private bool showBatchConverter = false;
        private string batchGuids = "";
        private string batchResults = "";

        [MenuItem("Tools/RuaWorks/GUID Converter")]
        public static void ShowWindow()
        {
            GetWindow<GUIDConverter>("GUID Converter");
        }

        private void OnGUI()
        {
            GUILayout.Label("GUID ⇔ パス 変換ツール", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // GUID → パス変換
            DrawGuidToPathSection();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // パス → GUID変換
            DrawPathToGuidSection();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // オブジェクト → GUID変換
            DrawObjectToGuidSection();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // バッチ変換
            DrawBatchConverterSection();

            EditorGUILayout.Space(10);

            // 結果表示
            if (!string.IsNullOrEmpty(convertedResult))
            {
                EditorGUILayout.HelpBox(convertedResult, resultMessageType);
            }
        }

        private void DrawGuidToPathSection()
        {
            GUILayout.Label("GUID → パス変換", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            guidInput = EditorGUILayout.TextField("GUID", guidInput);

            if (GUILayout.Button("クリア", GUILayout.Width(60)))
            {
                guidInput = "";
                convertedResult = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("パスに変換", GUILayout.Height(25)))
            {
                ConvertGuidToPath(guidInput);
            }

            if (GUILayout.Button("クリップボードから取得", GUILayout.Height(25)))
            {
                guidInput = GUIUtility.systemCopyBuffer;
                ConvertGuidToPath(guidInput);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPathToGuidSection()
        {
            GUILayout.Label("パス → GUID変換", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            pathInput = EditorGUILayout.TextField("アセットパス", pathInput);

            if (GUILayout.Button("クリア", GUILayout.Width(60)))
            {
                pathInput = "";
                convertedResult = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("GUIDに変換", GUILayout.Height(25)))
            {
                ConvertPathToGuid(pathInput);
            }

            if (GUILayout.Button("選択中のアセット", GUILayout.Height(25)))
            {
                if (Selection.activeObject != null)
                {
                    pathInput = AssetDatabase.GetAssetPath(Selection.activeObject);
                    ConvertPathToGuid(pathInput);
                }
                else
                {
                    ShowResult("アセットが選択されていません", MessageType.Warning);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectToGuidSection()
        {
            GUILayout.Label("オブジェクト → GUID変換", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            objectInput = EditorGUILayout.ObjectField("アセット", objectInput, typeof(Object), false);

            if (GUILayout.Button("クリア", GUILayout.Width(60)))
            {
                objectInput = null;
                convertedResult = "";
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("GUIDに変換", GUILayout.Height(25)))
            {
                if (objectInput != null)
                {
                    string path = AssetDatabase.GetAssetPath(objectInput);
                    ConvertPathToGuid(path);
                }
                else
                {
                    ShowResult("オブジェクトが設定されていません", MessageType.Warning);
                }
            }
        }

        private void DrawBatchConverterSection()
        {
            showBatchConverter = EditorGUILayout.Foldout(showBatchConverter, "バッチ変換（複数GUID一括変換）", true);

            if (showBatchConverter)
            {
                EditorGUI.indentLevel++;

                GUILayout.Label("GUID（1行に1つ）", EditorStyles.miniLabel);
                batchGuids = EditorGUILayout.TextArea(batchGuids, GUILayout.Height(100));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("一括変換", GUILayout.Height(25)))
                {
                    BatchConvertGuids();
                }

                if (GUILayout.Button("クリア", GUILayout.Height(25)))
                {
                    batchGuids = "";
                    batchResults = "";
                }
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(batchResults))
                {
                    EditorGUILayout.Space(5);
                    GUILayout.Label("結果", EditorStyles.miniLabel);
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                    EditorGUILayout.TextArea(batchResults, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();

                    if (GUILayout.Button("結果をクリップボードにコピー"))
                    {
                        GUIUtility.systemCopyBuffer = batchResults;
                        ShowResult("クリップボードにコピーしました", MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void ConvertGuidToPath(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                ShowResult("GUIDを入力してください", MessageType.Warning);
                return;
            }

            guid = guid.Trim();
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
            {
                ShowResult($"GUID '{guid}' に対応するアセットが見つかりません", MessageType.Error);
            }
            else
            {
                GUIUtility.systemCopyBuffer = path;

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                string assetType = asset != null ? asset.GetType().Name : "Unknown";

                ShowResult($"パス: {path}\nタイプ: {assetType}\n\n※クリップボードにコピーしました", MessageType.Info);

                // Projectウィンドウでハイライト
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                }
            }
        }

        private void ConvertPathToGuid(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                ShowResult("パスを入力してください", MessageType.Warning);
                return;
            }

            path = path.Trim();

            // 相対パスに変換
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            string guid = AssetDatabase.AssetPathToGUID(path);

            if (string.IsNullOrEmpty(guid))
            {
                ShowResult($"パス '{path}' に対応するGUIDが見つかりません", MessageType.Error);
            }
            else
            {
                GUIUtility.systemCopyBuffer = guid;
                ShowResult($"GUID: {guid}\nパス: {path}\n\n※クリップボードにコピーしました", MessageType.Info);
            }
        }

        private void BatchConvertGuids()
        {
            if (string.IsNullOrWhiteSpace(batchGuids))
            {
                ShowResult("GUIDを入力してください", MessageType.Warning);
                return;
            }

            string[] guids = batchGuids.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            System.Text.StringBuilder results = new System.Text.StringBuilder();
            int successCount = 0;
            int failCount = 0;

            foreach (string guid in guids)
            {
                string trimmedGuid = guid.Trim();
                if (string.IsNullOrEmpty(trimmedGuid)) continue;

                string path = AssetDatabase.GUIDToAssetPath(trimmedGuid);

                if (string.IsNullOrEmpty(path))
                {
                    results.AppendLine($"[失敗] {trimmedGuid} → 見つかりません");
                    failCount++;
                }
                else
                {
                    results.AppendLine($"{trimmedGuid} → {path}");
                    successCount++;
                }
            }

            batchResults = results.ToString();
            ShowResult($"変換完了: 成功 {successCount}件, 失敗 {failCount}件",
                       failCount > 0 ? MessageType.Warning : MessageType.Info);
        }

        private void ShowResult(string message, MessageType type)
        {
            convertedResult = message;
            resultMessageType = type;
            Repaint();
        }

        private void DrawSeparator()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
    }
}
