using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace RuaWorks.EditorExtensionTools
{
    /// <summary>
    /// �G�f�B�^�g���FAnimationClip�̃p�X�ɐړ�����ǉ�����c�[��
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

            EditorGUILayout.HelpBox("���̃c�[����AnimationClip�̂��ׂẴp�X�̐擪�Ɏw�肵���������ǉ����܂��B", MessageType.Info);
            EditorGUILayout.Space();

            // AnimationClip�̑I��
            targetClip = EditorGUILayout.ObjectField("Target Animation Clip", targetClip, typeof(AnimationClip), false) as AnimationClip;

            // �ړ����̓���
            prefixToAdd = EditorGUILayout.TextField("�ǉ�����ړ���", prefixToAdd);

            EditorGUILayout.Space();

            if (targetClip != null)
            {
                // �v���r���[�{�^��
                if (GUILayout.Button("�p�X���v���r���["))
                {
                    GeneratePathsPreview();
                    showPreview = true;
                }

                // �v���r���[�\��
                if (showPreview && pathsPreview.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("�v���r���[:", EditorStyles.boldLabel);

                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    for (int i = 0; i < pathsPreview.Count; i++)
                    {
                        EditorGUILayout.LabelField("���̃p�X: " + pathsPreview[i]);
                        EditorGUILayout.LabelField("�V�����p�X: " + newPathsPreview[i], EditorStyles.boldLabel);

                        if (i < pathsPreview.Count - 1)
                            EditorGUILayout.Space();
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.Space();

                // ���s�{�^��
                GUI.enabled = !string.IsNullOrEmpty(prefixToAdd);
                if (GUILayout.Button("�ړ�����ǉ�"))
                {
                    AddPrefixToAnimationPaths();
                }
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox("AnimationClip��I�����Ă��������B", MessageType.Warning);
            }
        }

        /// <summary>
        /// �p�X�̃v���r���[�𐶐�
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
        /// AnimationClip�̃p�X�ɐړ�����ǉ�
        /// </summary>
        private void AddPrefixToAnimationPaths()
        {
            if (targetClip == null || string.IsNullOrEmpty(prefixToAdd))
                return;

            // �ύX�O��Undo�o�^
            Undo.RecordObject(targetClip, "Add prefix to animation paths");

            // �A�j���[�V�����J�[�u������
            ProcessAnimationCurves();

            // �I�u�W�F�N�g�Q�ƃJ�[�u������
            ProcessObjectReferenceCurves();

            // �ύX��K�p
            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();

            Debug.Log("AnimationClip�̃p�X�ɐړ�����ǉ����܂���: " + targetClip.name);

            // �v���r���[���X�V
            if (showPreview)
            {
                GeneratePathsPreview();
            }
        }

        /// <summary>
        /// �A�j���[�V�����J�[�u�̃p�X������
        /// </summary>
        private void ProcessAnimationCurves()
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(targetClip);

            foreach (var binding in curveBindings)
            {
                // ���݂̃J�[�u���擾
                AnimationCurve curve = AnimationUtility.GetEditorCurve(targetClip, binding);

                // ���̃o�C���f�B���O���폜
                AnimationUtility.SetEditorCurve(targetClip, binding, null);

                // �V�����p�X�Ńo�C���f�B���O���쐬
                EditorCurveBinding newBinding = binding;
                newBinding.path = prefixToAdd + binding.path;

                // �V�����o�C���f�B���O�ɃJ�[�u��ݒ�
                AnimationUtility.SetEditorCurve(targetClip, newBinding, curve);
            }
        }

        /// <summary>
        /// �I�u�W�F�N�g�Q�ƃJ�[�u�̃p�X������
        /// </summary>
        private void ProcessObjectReferenceCurves()
        {
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(targetClip);

            foreach (var binding in objectBindings)
            {
                // ���݂̃I�u�W�F�N�g�Q�ƃJ�[�u���擾
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(targetClip, binding);

                // ���̃o�C���f�B���O���폜
                AnimationUtility.SetObjectReferenceCurve(targetClip, binding, null);

                // �V�����p�X�Ńo�C���f�B���O���쐬
                EditorCurveBinding newBinding = binding;
                newBinding.path = prefixToAdd + binding.path;

                // �V�����o�C���f�B���O�ɃL�[�t���[����ݒ�
                AnimationUtility.SetObjectReferenceCurve(targetClip, newBinding, keyframes);
            }
        }
    }
}
