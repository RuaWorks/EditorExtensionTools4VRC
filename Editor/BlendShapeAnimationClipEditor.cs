using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
namespace RuaWorks.EditorExtensionTools
{
    public class BlendShapeAnimationClipEditor : EditorWindow
    {
        private SkinnedMeshRenderer targetRenderer;
        private AnimationClip sourceClip;
        private Vector2 scrollPosition;
        private string exportFileName = "BlendShapeClip";

        [MenuItem("Tools/RuaWorks/BlendShape Animation Clip Editor")]
        public static void ShowWindow()
        {
            GetWindow<BlendShapeAnimationClipEditor>("BlendShape Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("BlendShape Animation Clip Editor", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Export Section
            GUILayout.Label("Export BlendShape to Animation Clip", EditorStyles.boldLabel);
            targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Skinned Mesh Renderer", targetRenderer, typeof(SkinnedMeshRenderer), true);

            exportFileName = EditorGUILayout.TextField("Export File Name", exportFileName);

            if (GUILayout.Button("Export BlendShape Values"))
            {
                ExportBlendShapeToAnimationClip();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();

            // Import Section
            GUILayout.Label("Import Animation Clip to BlendShape", EditorStyles.boldLabel);
            sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
                "Source Animation Clip", sourceClip, typeof(AnimationClip), false);

            if (GUILayout.Button("Apply Animation Clip to BlendShape"))
            {
                ApplyAnimationClipToBlendShape();
            }

            EditorGUILayout.Space();

            // Current BlendShape Values Display
            if (targetRenderer != null && targetRenderer.sharedMesh != null)
            {
                GUILayout.Label("Current BlendShape Values", EditorStyles.boldLabel);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                Mesh mesh = targetRenderer.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string blendShapeName = mesh.GetBlendShapeName(i);
                    float currentValue = targetRenderer.GetBlendShapeWeight(i);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(blendShapeName, GUILayout.Width(200));

                    float newValue = EditorGUILayout.Slider(currentValue, 0f, 100f);
                    if (newValue != currentValue)
                    {
                        Undo.RecordObject(targetRenderer, "Change BlendShape Weight");
                        targetRenderer.SetBlendShapeWeight(i, newValue);
                        EditorUtility.SetDirty(targetRenderer);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ExportBlendShapeToAnimationClip()
        {
            if (targetRenderer == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a SkinnedMeshRenderer.", "OK");
                return;
            }

            if (targetRenderer.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("Error", "SkinnedMeshRenderer has no mesh.", "OK");
                return;
            }

            // Get the path from root to the target renderer
            string relativePath = GetRelativePathFromRoot(targetRenderer.transform);

            if (string.IsNullOrEmpty(relativePath))
            {
                EditorUtility.DisplayDialog("Error", "Could not determine path from root.", "OK");
                return;
            }

            // Create new Animation Clip
            AnimationClip clip = new AnimationClip();
            clip.name = exportFileName;

            Mesh mesh = targetRenderer.sharedMesh;

            // Create animation curves for each blend shape
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string blendShapeName = mesh.GetBlendShapeName(i);
                float weight = targetRenderer.GetBlendShapeWeight(i);

                // Create curve with constant value
                AnimationCurve curve = new AnimationCurve();
                curve.AddKey(0f, weight);
                curve.AddKey(1f, weight);

                // Set the curve to the clip
                string propertyPath = $"blendShape.{blendShapeName}";
                clip.SetCurve(relativePath, typeof(SkinnedMeshRenderer), propertyPath, curve);
            }

            // Save the animation clip
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Animation Clip",
                exportFileName,
                "anim",
                "Please enter a file name to save the animation clip to");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(clip, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success",
                    $"Animation clip saved to: {path}\n\nBlendShapes exported: {mesh.blendShapeCount}", "OK");
            }
        }

        private void ApplyAnimationClipToBlendShape()
        {
            if (targetRenderer == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a SkinnedMeshRenderer.", "OK");
                return;
            }

            if (sourceClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Animation Clip.", "OK");
                return;
            }

            // Get the path from root to the target renderer
            string relativePath = GetRelativePathFromRoot(targetRenderer.transform);

            if (string.IsNullOrEmpty(relativePath))
            {
                EditorUtility.DisplayDialog("Error", "Could not determine path from root.", "OK");
                return;
            }

            // Get all curve bindings from the animation clip
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);

            int appliedCount = 0;
            Mesh mesh = targetRenderer.sharedMesh;

            Undo.RecordObject(targetRenderer, "Apply Animation Clip to BlendShape");

            foreach (EditorCurveBinding binding in bindings)
            {
                // Check if this binding is for the target renderer and is a blend shape
                if (binding.path == relativePath &&
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    binding.propertyName.StartsWith("blendShape."))
                {
                    // Extract blend shape name
                    string blendShapeName = binding.propertyName.Substring("blendShape.".Length);

                    // Find the blend shape index
                    int blendShapeIndex = -1;
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        if (mesh.GetBlendShapeName(i) == blendShapeName)
                        {
                            blendShapeIndex = i;
                            break;
                        }
                    }

                    if (blendShapeIndex >= 0)
                    {
                        // Get the curve and evaluate at time 0
                        AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                        if (curve != null && curve.length > 0)
                        {
                            float value = curve.Evaluate(0f);
                            targetRenderer.SetBlendShapeWeight(blendShapeIndex, value);
                            appliedCount++;
                        }
                    }
                }
            }

            EditorUtility.SetDirty(targetRenderer);

            if (appliedCount > 0)
            {
                EditorUtility.DisplayDialog("Success",
                    $"Applied {appliedCount} blend shape values from animation clip.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning",
                    "No matching blend shape data found in the animation clip.\n\n" +
                    "Make sure the animation clip was created for the same object hierarchy.", "OK");
            }
        }

        private string GetRelativePathFromRoot(Transform target)
        {
            // Find the root transform (either the scene root or the prefab root)
            Transform root = target;
            while (root.parent != null)
            {
                root = root.parent;
            }

            // Build the relative path from root to target
            List<string> pathElements = new List<string>();
            Transform current = target;

            while (current != root)
            {
                pathElements.Insert(0, current.name);
                current = current.parent;
            }

            // If target is the root itself, return empty string
            if (pathElements.Count == 0)
            {
                return "";
            }

            return string.Join("/", pathElements.ToArray());
        }
    }
}

