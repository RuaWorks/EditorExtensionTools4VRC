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
            GUILayout.Label("BlendShape���O�G�N�X�|�[�^�[", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // SkinnedMeshRenderer�̑I��
            targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Target Renderer",
                targetRenderer,
                typeof(SkinnedMeshRenderer),
                true
            );

            EditorGUILayout.Space();

            // �I�v�V�����ݒ�
            GUILayout.Label("�G�N�X�|�[�g�ݒ�", EditorStyles.boldLabel);
            fileName = EditorGUILayout.TextField("�t�@�C����", fileName);
            includeIndex = EditorGUILayout.Toggle("�C���f�b�N�X���܂߂�", includeIndex);
            includeWeights = EditorGUILayout.Toggle("���݂̃E�F�C�g���܂߂�", includeWeights);

            EditorGUILayout.Space();

            // �v���r���[�\��
            if (targetRenderer != null && targetRenderer.sharedMesh != null)
            {
                Mesh mesh = targetRenderer.sharedMesh;
                int blendShapeCount = mesh.blendShapeCount;

                GUILayout.Label($"BlendShape��: {blendShapeCount}", EditorStyles.helpBox);
                EditorGUILayout.Space();

                // �v���r���[�G���A
                GUILayout.Label("�v���r���[", EditorStyles.boldLabel);
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
                EditorGUILayout.HelpBox("SkinnedMeshRenderer��I�����Ă�������", MessageType.Info);
            }

            EditorGUILayout.Space();

            // �G�N�X�|�[�g�{�^��
            GUI.enabled = targetRenderer != null && targetRenderer.sharedMesh != null;

            if (GUILayout.Button("�e�L�X�g�t�@�C���ɃG�N�X�|�[�g", GUILayout.Height(30)))
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
                EditorUtility.DisplayDialog("�G���[", "�L����SkinnedMeshRenderer���I������Ă��܂���", "OK");
                return;
            }

            // �ۑ���̑I��
            string path = EditorUtility.SaveFilePanel(
                "BlendShape���O���X�g��ۑ�",
                "Assets",
                fileName,
                "txt"
            );

            if (string.IsNullOrEmpty(path))
            {
                return; // �L�����Z�����ꂽ
            }

            try
            {
                Mesh mesh = targetRenderer.sharedMesh;
                int blendShapeCount = mesh.blendShapeCount;

                using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    // �w�b�_�[���
                    writer.WriteLine($"# BlendShape List");
                    writer.WriteLine($"# Mesh: {mesh.name}");
                    writer.WriteLine($"# GameObject: {targetRenderer.gameObject.name}");
                    writer.WriteLine($"# Total Count: {blendShapeCount}");
                    writer.WriteLine($"# Export Date: {System.DateTime.Now}");
                    writer.WriteLine();

                    // BlendShape���O�̏o��
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        string blendShapeName = mesh.GetBlendShapeName(i);
                        float weight = targetRenderer.GetBlendShapeWeight(i);
                        string line = GenerateLineText(i, blendShapeName, weight);
                        writer.WriteLine(line);
                    }
                }

                EditorUtility.DisplayDialog(
                    "�G�N�X�|�[�g����",
                    $"{blendShapeCount}��BlendShape���O���o�͂��܂���\n\n�ۑ���: {path}",
                    "OK"
                );

                // �t�@�C�����J��
                System.Diagnostics.Process.Start(path);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("�G���[", $"�t�@�C���̏������݂Ɏ��s���܂���\n\n{e.Message}", "OK");
            }
        }
    }
}

