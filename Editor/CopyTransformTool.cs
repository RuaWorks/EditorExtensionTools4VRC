using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RuaWorks.EditorExtensionTools
{
    public class CopyTransformTool : EditorWindow
    {
        private GameObject _source;
        private GameObject _target;

        [MenuItem("Tools/RuaWorks/CopyTransformTool")]
        public static void ShowWindow()
        {
            GetWindow<CopyTransformTool>("CopyTransform");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            _source = EditorGUILayout.ObjectField(_source, typeof(GameObject), true) as GameObject;
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            _target = EditorGUILayout.ObjectField(_target, typeof(GameObject), true) as GameObject;

            EditorGUILayout.Space();

            if (GUILayout.Button("Copy"))
            {
                if (_source == null || _target == null)
                {
                    EditorUtility.DisplayDialog("Error", "Must Not null ", "OK");
                    return;
                }

                Fix();
            }
        }

        private void Fix()
        {
            Undo.RecordObject(_target.transform, "Copy Transform");

            var source = _source.GetComponentsInChildren<Transform>(true).ToList();
            var target = _target.GetComponentsInChildren<Transform>(true).ToList();

            foreach (var t in target)
            {
                var s = source.Find(x => x.name == t.name);
                if (s == null) { continue; }

                t.position = s.position;
                t.rotation = s.rotation;
                t.localScale = s.localScale;
            }
        }
    }

}
