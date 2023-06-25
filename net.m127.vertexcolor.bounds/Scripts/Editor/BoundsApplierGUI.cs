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

        private static void ApplyBounds(Bounds bounds, Renderer renderer)
        {
            BoundsApplier ba;
            if (!renderer.TryGetComponent(out ba)) {
                ba = renderer.gameObject.AddComponent<BoundsApplier>();
            }
            ba.bounds = bounds;
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            VertexColorBaker.RegisterForBoundTransferral(ApplyBounds);
        }
    }
}