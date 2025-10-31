using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class AnimationClipOrMerger : EditorWindow
{
    private AnimationClip clipA;
    private AnimationClip clipB;
    private string outputPath = "Assets/MergedAnimations";
    private string outputFileName = "MergedClip";

    [MenuItem("Ruavel/Animation/Animation Clip Or Merger")]
    public static void ShowWindow()
    {
        GetWindow<AnimationClipOrMerger>("Animation Merger");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animation Clip Or Merger", EditorStyles.boldLabel);

        GUILayout.Label("ClipA + ClipBのClipを作成します。重複する値はClipAが優先されます");

        clipA = EditorGUILayout.ObjectField("Clip A (Source)", clipA, typeof(AnimationClip), false) as AnimationClip;
        clipB = EditorGUILayout.ObjectField("Clip B (Additive)", clipB, typeof(AnimationClip), false) as AnimationClip;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        outputPath = EditorGUILayout.TextField("Output Folder", outputPath);
        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

        if (GUILayout.Button("Browse Output Folder"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Unityプロジェクトのパスに対する相対パスに変換
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
        // 出力フォルダが存在しない場合は作成
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // 新しいAnimationClipを作成
        AnimationClip clipC = new AnimationClip();
        clipC.name = outputFileName;

        // クリップAとBのすべてのカーブを取得
        EditorCurveBinding[] bindingsA = AnimationUtility.GetCurveBindings(clipA);
        EditorCurveBinding[] bindingsB = AnimationUtility.GetCurveBindings(clipB);

        // クリップAのバインディングをディクショナリに格納
        Dictionary<string, EditorCurveBinding> bindingDictA = new Dictionary<string, EditorCurveBinding>();
        foreach (var binding in bindingsA)
        {
            string key = $"{binding.path}/{binding.propertyName}";
            bindingDictA[key] = binding;
        }

        // クリップBの各カーブでAに存在しないものを追加
        foreach (var bindingB in bindingsB)
        {
            string keyB = $"{bindingB.path}/{bindingB.propertyName}";

            if (!bindingDictA.ContainsKey(keyB))
            {
                AnimationCurve curveB = AnimationUtility.GetEditorCurve(clipB, bindingB);
                AnimationUtility.SetEditorCurve(clipC, bindingB, curveB);
            }
        }

        //格納したバインディングをコピー
        foreach(var binding in bindingDictA)
        {
            AnimationCurve curveA = AnimationUtility.GetEditorCurve(clipA, binding.Value);
            AnimationUtility.SetEditorCurve(clipC, binding.Value, curveA);
        }

        // アニメーションの設定をコピー
        clipC.frameRate = clipB.frameRate;
        clipC.wrapMode = clipB.wrapMode;

        // ファイルパスを生成
        string filePath = Path.Combine(outputPath, $"{outputFileName}.anim");

        // アセットとして保存
        AssetDatabase.CreateAsset(clipC, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 保存完了メッセージ
        EditorUtility.DisplayDialog("Success",
            $"Merged animation has been saved to:\n{filePath}", "OK");

        // 保存したアセットを選択状態にする
        Selection.activeObject = clipC;
    }
}