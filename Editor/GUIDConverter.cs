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
            GUILayout.Label("GUID �� �p�X �ϊ��c�[��", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // GUID �� �p�X�ϊ�
            DrawGuidToPathSection();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // �p�X �� GUID�ϊ�
            DrawPathToGuidSection();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // �I�u�W�F�N�g �� GUID�ϊ�
            DrawObjectToGuidSection();

            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);

            // �o�b�`�ϊ�
            DrawBatchConverterSection();

            EditorGUILayout.Space(10);

            // ���ʕ\��
            if (!string.IsNullOrEmpty(convertedResult))
            {
                EditorGUILayout.HelpBox(convertedResult, resultMessageType);
            }
        }

        private void DrawGuidToPathSection()
        {
            GUILayout.Label("GUID �� �p�X�ϊ�", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            guidInput = EditorGUILayout.TextField("GUID", guidInput);

            if (GUILayout.Button("�N���A", GUILayout.Width(60)))
            {
                guidInput = "";
                convertedResult = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("�p�X�ɕϊ�", GUILayout.Height(25)))
            {
                ConvertGuidToPath(guidInput);
            }

            if (GUILayout.Button("�N���b�v�{�[�h����擾", GUILayout.Height(25)))
            {
                guidInput = GUIUtility.systemCopyBuffer;
                ConvertGuidToPath(guidInput);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPathToGuidSection()
        {
            GUILayout.Label("�p�X �� GUID�ϊ�", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            pathInput = EditorGUILayout.TextField("�A�Z�b�g�p�X", pathInput);

            if (GUILayout.Button("�N���A", GUILayout.Width(60)))
            {
                pathInput = "";
                convertedResult = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("GUID�ɕϊ�", GUILayout.Height(25)))
            {
                ConvertPathToGuid(pathInput);
            }

            if (GUILayout.Button("�I�𒆂̃A�Z�b�g", GUILayout.Height(25)))
            {
                if (Selection.activeObject != null)
                {
                    pathInput = AssetDatabase.GetAssetPath(Selection.activeObject);
                    ConvertPathToGuid(pathInput);
                }
                else
                {
                    ShowResult("�A�Z�b�g���I������Ă��܂���", MessageType.Warning);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawObjectToGuidSection()
        {
            GUILayout.Label("�I�u�W�F�N�g �� GUID�ϊ�", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            objectInput = EditorGUILayout.ObjectField("�A�Z�b�g", objectInput, typeof(Object), false);

            if (GUILayout.Button("�N���A", GUILayout.Width(60)))
            {
                objectInput = null;
                convertedResult = "";
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("GUID�ɕϊ�", GUILayout.Height(25)))
            {
                if (objectInput != null)
                {
                    string path = AssetDatabase.GetAssetPath(objectInput);
                    ConvertPathToGuid(path);
                }
                else
                {
                    ShowResult("�I�u�W�F�N�g���ݒ肳��Ă��܂���", MessageType.Warning);
                }
            }
        }

        private void DrawBatchConverterSection()
        {
            showBatchConverter = EditorGUILayout.Foldout(showBatchConverter, "�o�b�`�ϊ��i����GUID�ꊇ�ϊ��j", true);

            if (showBatchConverter)
            {
                EditorGUI.indentLevel++;

                GUILayout.Label("GUID�i1�s��1�j", EditorStyles.miniLabel);
                batchGuids = EditorGUILayout.TextArea(batchGuids, GUILayout.Height(100));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("�ꊇ�ϊ�", GUILayout.Height(25)))
                {
                    BatchConvertGuids();
                }

                if (GUILayout.Button("�N���A", GUILayout.Height(25)))
                {
                    batchGuids = "";
                    batchResults = "";
                }
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(batchResults))
                {
                    EditorGUILayout.Space(5);
                    GUILayout.Label("����", EditorStyles.miniLabel);
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                    EditorGUILayout.TextArea(batchResults, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();

                    if (GUILayout.Button("���ʂ��N���b�v�{�[�h�ɃR�s�["))
                    {
                        GUIUtility.systemCopyBuffer = batchResults;
                        ShowResult("�N���b�v�{�[�h�ɃR�s�[���܂���", MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void ConvertGuidToPath(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                ShowResult("GUID����͂��Ă�������", MessageType.Warning);
                return;
            }

            guid = guid.Trim();
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
            {
                ShowResult($"GUID '{guid}' �ɑΉ�����A�Z�b�g��������܂���", MessageType.Error);
            }
            else
            {
                GUIUtility.systemCopyBuffer = path;

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                string assetType = asset != null ? asset.GetType().Name : "Unknown";

                ShowResult($"�p�X: {path}\n�^�C�v: {assetType}\n\n���N���b�v�{�[�h�ɃR�s�[���܂���", MessageType.Info);

                // Project�E�B���h�E�Ńn�C���C�g
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
                ShowResult("�p�X����͂��Ă�������", MessageType.Warning);
                return;
            }

            path = path.Trim();

            // ���΃p�X�ɕϊ�
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            string guid = AssetDatabase.AssetPathToGUID(path);

            if (string.IsNullOrEmpty(guid))
            {
                ShowResult($"�p�X '{path}' �ɑΉ�����GUID��������܂���", MessageType.Error);
            }
            else
            {
                GUIUtility.systemCopyBuffer = guid;
                ShowResult($"GUID: {guid}\n�p�X: {path}\n\n���N���b�v�{�[�h�ɃR�s�[���܂���", MessageType.Info);
            }
        }

        private void BatchConvertGuids()
        {
            if (string.IsNullOrWhiteSpace(batchGuids))
            {
                ShowResult("GUID����͂��Ă�������", MessageType.Warning);
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
                    results.AppendLine($"[���s] {trimmedGuid} �� ������܂���");
                    failCount++;
                }
                else
                {
                    results.AppendLine($"{trimmedGuid} �� {path}");
                    successCount++;
                }
            }

            batchResults = results.ToString();
            ShowResult($"�ϊ�����: ���� {successCount}��, ���s {failCount}��",
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
