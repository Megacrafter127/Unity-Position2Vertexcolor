using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace M127
{
    public class VertexColorBaker : UnityEditor.EditorWindow
    {
        [MenuItem("Tools/M127/Vertex Color Baker")]
        public static void Open()
        {
            EditorWindow wnd = GetWindow<VertexColorBaker>();
            wnd.titleContent = new GUIContent("Bounds Calculator");
        }

        private static event Action<Bounds, Renderer> BoundTransferral;

        private static IEnumerable<string> parseParametersIndividually(IEnumerable<ParameterInfo> parameters)
        {
            foreach (ParameterInfo info in parameters)
            {
                yield return $"{info.ParameterType} {info.Name}";
            }
        }

        private static string parseParameters(IEnumerable<ParameterInfo> parameters)
        {
            return string.Join(", ", parseParametersIndividually(parameters));
        }

        public static void RegisterForBoundTransferral(Action<Bounds, Renderer> listener)
        {
            Debug.Log($"Registered Listener {listener.Method.DeclaringType}.{listener.Method.Name}({parseParameters(listener.Method.GetParameters())})");
            BoundTransferral += listener;
        }

        public static void UnregisterForBoundTransferral(Action<Bounds, Renderer> listener)
        {
            Debug.Log($"UnRegistered Listener {listener.Method.DeclaringType}.{listener.Method.Name}({parseParameters(listener.Method.GetParameters())})");
            BoundTransferral -= listener;
        }

        bool overwrite = true;

        public void OnGUI()
        {
            GameObject oldAvatar = avatar;
            avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);
            if (oldAvatar != avatar)
            {
                selection.Clear();
                hasPbound = false;
            }
            if (avatar is null)
            {
                EditorGUILayout.HelpBox("More Options will be listed once an object is selected.", MessageType.Info);
                return;
            }
            bool hasMesh = false;
            foreach (Renderer r in avatar.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    hasMesh = true;
                    bool sel = EditorGUILayout.ToggleLeft("Selected", selection.Contains(r));
                    EditorGUILayout.ObjectField(r, typeof(Renderer), true);
                    if (sel) selection.Add(r);
                    else selection.Remove(r);
                }
            }
            if (!hasMesh) EditorGUILayout.HelpBox("Selected object does not contain any recognized meshes.", MessageType.Warning);
            EditorGUILayout.Separator();
            if (hasPbound)
            {
                EditorGUILayout.Vector3Field("Minimum Bound", pbound.min);
                EditorGUILayout.Vector3Field("Maximum Bound", pbound.max);
            }
            else
            {
                Bounds bound = new Bounds();
                foreach (Renderer r in selection)
                {
                    bound.Encapsulate(r.bounds);
                }

                EditorGUILayout.Vector3Field("Minimum Bound", bound.min);
                EditorGUILayout.Vector3Field("Maximum Bound", bound.max);
            }
            EditorGUILayout.Separator();

            overwrite = EditorGUILayout.ToggleLeft("Overwrite already baked meshes", overwrite);
            if (GUILayout.Button("Bake Vertex Colors")) bakeMeshes(overwrite);
            if (hasPbound)
            {
                if (GUILayout.Button("")) transferBounds();
            }
            else EditorGUILayout.HelpBox("Cannot transfer without baking first. If you have already baked, <b>BoundsApplier</b> components will be attached to each baked renderer", MessageType.Info);
        }

        public const string BB_MIN = "_BoundingBoxMin", BB_MAX = "_BoundingBoxMax";

        public void transferBounds()
        {
            if (!hasPbound)
            {
                Debug.LogError("Cannot transfer unknown bounds");
                return;
            }
            foreach(Renderer r in selection)
            {
                BoundTransferral?.Invoke(pbound, r);
            }
        }

        public GameObject avatar;

        public readonly ISet<Renderer> selection = new HashSet<Renderer>();

        public Bounds pbound;
        public bool hasPbound;

        public const string UNDO_NAME = "Vertex Color Baker";

        public void bakeMeshes(bool reuse)
        {
            Queue<Vector3> positions = new Queue<Vector3>(1);
            Queue<Quaternion> rotations = new Queue<Quaternion>(1);
            Queue<Vector3> scales = new Queue<Vector3>(1);
            for (Transform t = avatar.transform; t; t = t.parent)
            {
                positions.Enqueue(t.localPosition);
                t.localPosition = Vector3.zero;
                rotations.Enqueue(t.localRotation);
                t.localRotation = Quaternion.identity;
                scales.Enqueue(t.localScale);
                t.localScale = Vector3.one;
            }
            pbound = new Bounds();
            hasPbound = true;
            foreach (Renderer r in selection)
            {
                pbound.Encapsulate(r.bounds);
                Mesh n = null;
                Vector3[] verts;
                Color[] colors;
                Matrix4x4 transformer = r.transform.localToWorldMatrix;
                string path = null;
                bool create = false;
                switch (r)
                {
                    case SkinnedMeshRenderer smr:
                        path = AssetDatabase.GetAssetPath(smr.sharedMesh);
                        n = smr.sharedMesh;
                        if (!reuse || !path.EndsWith("_vcol_pos.mesh"))
                        {
                            if(!AssetDatabase.IsMainAsset(n)) path += "_" + n.name;
                            n = Instantiate(n);
                            create = true;
                        }
                        else Undo.RecordObject(n, UNDO_NAME);
                        Mesh bm = new Mesh();
                        smr.BakeMesh(bm);
                        Vector3 lscl = smr.transform.localScale;
                        lscl.x = 1 / lscl.x;
                        lscl.y = 1 / lscl.y;
                        lscl.z = 1 / lscl.z;
                        transformer = transformer * Matrix4x4.Scale(lscl);
                        verts = bm.vertices;
                        colors = new Color[verts.Length];
                        for (int i = 0; i < n.vertexCount; i++)
                        {
                            Vector3 v = transformer.MultiplyPoint3x4(verts[i]);
                            colors[i] = new Color(v.x, v.y, v.z);
                        }
                        n.colors = colors;
                        break;
                    case MeshRenderer mr:
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        n = mf?.sharedMesh;
                        if (n is null)
                        {
                            Debug.LogError($"Mesh Renderer <b>{mr.gameObject.name}</b> has invalid mesh", mr);
                            continue;
                        }
                        path = AssetDatabase.GetAssetPath(n);
                        if (!reuse || !path.EndsWith("_vcol_pos.mesh"))
                        {
                            if (!AssetDatabase.IsMainAsset(n)) path += "_" + n.name;
                            n = Instantiate(n);
                            create = true;
                        }
                        verts = n.vertices;
                        colors = new Color[verts.Length];
                        for (int i = 0; i < n.vertexCount; i++)
                        {
                            Vector3 v = transformer.MultiplyPoint3x4(verts[i]);
                            colors[i] = new Color(v.x, v.y, v.z);
                        }
                        n.colors = colors;
                        break;
                }
                if (n is null)
                {
                    Debug.LogError($"Renderer <b>{r.gameObject.name}</b> is of unknown type", r);
                    continue;
                }
                if (create)
                {
                    string pdir = Path.GetDirectoryName(path);
                    if (pdir.StartsWith("Library")) pdir = "Assets/Rebaked Default Assets/" + pdir;
                    if (!pdir.EndsWith("Baked")) pdir += "/Baked";
                    path = $"{pdir}/{Path.GetFileNameWithoutExtension(path)}_vcol_pos.mesh";
                    Stack<string> dcreate = new Stack<string>();
                    string p;
                    for (p = pdir; !AssetDatabase.IsValidFolder(p); p = Path.GetDirectoryName(p))
                    {
                        dcreate.Push(Path.GetFileName(p));
                    }
                    while (dcreate.Count > 0)
                    {
                        string elem = dcreate.Pop();
                        AssetDatabase.CreateFolder(p, elem);
                        p = Path.Combine(p, elem);
                    }
                    path = AssetDatabase.GenerateUniqueAssetPath(path);
                    AssetDatabase.CreateAsset(n, path);
                    n = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (n is null)
                    {
                        Debug.LogError($"Error creating mesh asset: <b>{path}</b>.", r);
                        continue;
                    }
                    Undo.RecordObject(r, UNDO_NAME);
                    switch (r)
                    {
                        case SkinnedMeshRenderer smr:
                            smr.sharedMesh = n;
                            break;
                        case MeshRenderer mr:
                            mr.GetComponent<MeshFilter>().sharedMesh = n;
                            break;
                    }

                }
                else EditorUtility.SetDirty(n);
            }
            for (Transform t = avatar.transform; t; t = t.parent)
            {
                t.localPosition = positions.Dequeue();
                t.localRotation = rotations.Dequeue();
                t.localScale = scales.Dequeue();
            }
            transferBounds();
        }
    }
}