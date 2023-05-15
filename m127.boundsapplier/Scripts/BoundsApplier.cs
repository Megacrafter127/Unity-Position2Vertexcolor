#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

namespace M127
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class BoundsApplier : MonoBehaviour
    {
        public const string BBMIN_NAME = "_BoundingBoxMin",
            BBMAX_NAME = "_BoundingBoxMax";

        private static void ApplyBounds(Bounds bounds, Renderer renderer)
        {
            BoundsApplier ba;
            if(!renderer.TryGetComponent(out ba))
            {
                ba = renderer.gameObject.AddComponent<BoundsApplier>();
            }
            ba.bounds = bounds;
        }

        [InitializeOnLoadMethod]
        private static void Register() {
            VertexColorBaker.RegisterForBoundTransferral(ApplyBounds);
        }

        [HideInInspector]
        [SerializeField]
        public Bounds bounds;

        private void Awake()
        {
            base.hideFlags |= HideFlags.DontSaveInBuild;
        }

        void Reset()
        {
            base.hideFlags |= HideFlags.DontSaveInBuild;
        }

        public void Apply()
        {
            foreach (Renderer re in GetComponents<Renderer>())
            {
                foreach (Material m in re.sharedMaterials)
                {
                    Undo.RecordObject(m,"Bounds Applied");
                    m.SetVector(BBMIN_NAME, bounds.min);
                    m.SetVector(BBMAX_NAME, bounds.max);
                }
            }
        }
    }
}
#endif