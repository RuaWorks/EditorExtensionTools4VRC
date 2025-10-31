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
            EditorGUILayout.LabelField("Target - Source��Target�ɏ㏑�����܂�");
            EditorGUILayout.Space();

            // �\�[�X�N���b�v�p�̃h���b�O���h���b�v�G���A
            EditorGUILayout.LabelField("Source Clip (A)");
            DrawSourceDropArea();
            sourceClip = EditorGUILayout.ObjectField(sourceClip, typeof(AnimationClip), false) as AnimationClip;

            EditorGUILayout.Space();

            // �^�[�Q�b�g�N���b�v�p�̃h���b�O���h���b�v�G���A
            EditorGUILayout.LabelField("Target Clips (B) - �h���b�O���h���b�v�ŕ����ǉ��\");
            DrawTargetDropArea();

            // Target Clips ���X�g�\���p�̐܂肽���݃Z�N�V����
            foldoutTargets = EditorGUILayout.Foldout(foldoutTargets, $"Target Clips ({targetClips.Count})");
            if (foldoutTargets)
            {
                EditorGUI.indentLevel++;

                // �X�N���[���r���[�̊J�n
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

                // �^�[�Q�b�g�N���b�v�̃��X�g��\��
                for (int i = 0; i < targetClips.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    targetClips[i] = EditorGUILayout.ObjectField($"Target Clip {i + 1}", targetClips[i], typeof(AnimationClip), false) as AnimationClip;

                    if (GUILayout.Button("�~", GUILayout.Width(25)))
                    {
                        targetClips.RemoveAt(i);
                        GUIUtility.ExitGUI(); // GUI�X�V�̂��߂ɃC�x���g�𒆒f
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                // �ǉ��{�^��
                if (GUILayout.Button("Add Target Clip"))
                {
                    targetClips.Add(null);
                }

                // ���ׂăN���A�{�^��
                if (targetClips.Count > 0 && GUILayout.Button("Clear All"))
                {
                    targetClips.Clear();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // �������s�{�^��
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
            GUI.Box(dropAreaSource = EditorGUILayout.GetControlRect(GUILayout.Height(35)), "�h���b�O���h���b�v��Source�N���b�v���w��", EditorStyles.helpBox);

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
                                break; // �ŏ��̗L���ȃN���b�v�������g�p
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
            GUI.Box(dropAreaTargets = EditorGUILayout.GetControlRect(GUILayout.Height(35)), "�h���b�O���h���b�v�ŕ�����Target�N���b�v���w��", EditorStyles.helpBox);

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
            // sourceClip�̃o�C���f�B���O���擾
            var sourceBindings = AnimationUtility.GetCurveBindings(sourceClip);
            int totalRemovedProperties = 0;

            // �e�^�[�Q�b�g�N���b�v�ɑ΂��ď��������s
            foreach (var targetClip in targetClips)
            {
                var targetBindings = AnimationUtility.GetCurveBindings(targetClip).ToList();

                // �폜����K�v�̂���o�C���f�B���O�����W
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

                // �d������v���p�e�B���폜
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
