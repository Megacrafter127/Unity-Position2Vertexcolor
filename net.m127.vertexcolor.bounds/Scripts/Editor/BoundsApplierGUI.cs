using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace M127
{
    [CustomEditor(typeof(BoundsApplier))]
    public class BoundsApplierGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            BoundsApplier trg = (BoundsApplier)base.target;
            EditorGUILayout.Vector3Field("Bounds Minimum", trg.bounds.min);
            EditorGUILayout.Vector3Field("Bounds Maximum", trg.bounds.max);
            if (GUILayout.Button("Apply")) trg.Apply();
        }
    }
}