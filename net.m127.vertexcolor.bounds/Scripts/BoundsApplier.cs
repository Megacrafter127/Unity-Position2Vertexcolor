using UnityEngine;
using VRC.SDKBase;

namespace M127
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class BoundsApplier : MonoBehaviour, IEditorOnly
    {
        public const string BBMIN_NAME = "_BoundingBoxMin",
            BBMAX_NAME = "_BoundingBoxMax";

        [HideInInspector]
        [SerializeField]
        public Bounds bounds;

        public void Apply()
        {
            foreach (Renderer re in GetComponents<Renderer>())
            {
                foreach (Material m in re.sharedMaterials)
                {
                    m.SetVector(BBMIN_NAME, bounds.min);
                    m.SetVector(BBMAX_NAME, bounds.max);
                }
            }
        }
    }
}