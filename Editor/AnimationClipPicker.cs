using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace RuaWorks.EditorExtensionTools
{
    public class AnimationClipPicker : EditorWindow
    {
        private AnimationClip clipA;
        private AnimationClip clipB;
        private string outputPath = "Assets/AnimationClipPicker";
        private string outputFileName = "MergedClip";

        [MenuItem("Tools/RuaWorks/Animation Clip Merger")]
        public static void ShowWindow()
        {
            GetWindow<AnimationClipPicker>("AnimationClipPicker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Clip Picker", EditorStyles.boldLabel);
            GUILayout.Label("ClipB�̂����AClipA�ɑ��݂��镨�݂̂𒊏o", EditorStyles.label);

            clipA = EditorGUILayout.ObjectField("Clip A (Reference)", clipA, typeof(AnimationClip), false) as AnimationClip;
            clipB = EditorGUILayout.ObjectField("Clip B (Source)", clipB, typeof(AnimationClip), false) as AnimationClip;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField("Output Folder", outputPath);
            outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

            if (GUILayout.Button("Browse Output Folder"))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Unity�v���W�F�N�g�̃p�X�ɑ΂��鑊�΃p�X�ɕϊ�
                    outputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
            }

            EditorGUILayout.Space();

            GUI.enabled = clipA != null && clipB != null && !string.IsNullOrEmpty(outputPath);
            if (GUILayout.Button("Generate Merged Animation"))
            {
                CreateMergedAnimation();
            }
            GUI.enabled = true;
        }

        private void CreateMergedAnimation()
        {
            // �o�̓t�H���_�����݂��Ȃ��ꍇ�͍쐬
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // �V����AnimationClip���쐬
            AnimationClip clipC = new AnimationClip();
            clipC.name = outputFileName;

            // �N���b�vA��B�̂��ׂẴJ�[�u���擾
            EditorCurveBinding[] bindingsA = AnimationUtility.GetCurveBindings(clipA);
            EditorCurveBinding[] bindingsB = AnimationUtility.GetCurveBindings(clipB);

            // �N���b�vA�̃o�C���f�B���O���f�B�N�V���i���Ɋi�[
            Dictionary<string, EditorCurveBinding> bindingDictA = new Dictionary<string, EditorCurveBinding>();
            foreach (var binding in bindingsA)
            {
                string key = $"{binding.path}/{binding.propertyName}";
                bindingDictA[key] = binding;
            }

            // �N���b�vB�̊e�J�[�u���`�F�b�N���ăR�s�[
            foreach (var bindingB in bindingsB)
            {
                string keyB = $"{bindingB.path}/{bindingB.propertyName}";

                if (bindingDictA.ContainsKey(keyB))
                {
                    AnimationCurve curveB = AnimationUtility.GetEditorCurve(clipB, bindingB);
                    AnimationUtility.SetEditorCurve(clipC, bindingB, curveB);
                }
            }

            // �A�j���[�V�����̐ݒ���R�s�[
            clipC.frameRate = clipB.frameRate;
            clipC.wrapMode = clipB.wrapMode;

            // �t�@�C���p�X�𐶐�
            string filePath = Path.Combine(outputPath, $"{outputFileName}.anim");

            // �A�Z�b�g�Ƃ��ĕۑ�
            AssetDatabase.CreateAsset(clipC, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // �ۑ��������b�Z�[�W
            EditorUtility.DisplayDialog("Success",
                $"Merged animation has been saved to:\n{filePath}", "OK");

            // �ۑ������A�Z�b�g��I����Ԃɂ���
            Selection.activeObject = clipC;
        }
    }
}
