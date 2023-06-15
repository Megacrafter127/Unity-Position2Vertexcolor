using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class PointEncoder : EditorWindow
{
    [MenuItem("Tools/M127/PointApplier")]
    public static void Display()
    {
        PointEncoder wnd = GetWindow<PointEncoder>();
    }

    private GameObject avatar;

    private Transform point;

    private ISet<Renderer> selection = new HashSet<Renderer>();

    private string bindingSelection = null;

    private AnimationClip clip;

    void OnGUI()
    {
        GameObject oldAvatar = avatar;
        avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);
        if (oldAvatar != avatar)
        {
            selection.Clear();
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
        else if (selection.Count == 0) EditorGUILayout.HelpBox("No Renderers selected: Nothing will be done", MessageType.Warning);
        EditorGUILayout.Separator();
        point = (Transform)EditorGUILayout.ObjectField("Point", point, typeof(Transform), true);
        clip = (AnimationClip)EditorGUILayout.ObjectField("Animation", clip, typeof(AnimationClip), true);
        if(clip is null)
        {
            EditorGUILayout.HelpBox("You need to specify an animation clip to edit.", MessageType.Info);
            return;
        }
        EditorGUILayout.Separator();
        IList<string> bindings = selection.SelectMany(r => r.sharedMaterials)
            .Select(m => m.shader)
            .Distinct()
            .SelectMany(s => Enumerable.Range(0, s.GetPropertyCount())
                .Where(p => s.GetPropertyType(p) == ShaderPropertyType.Vector)
                .Select(p => s.GetPropertyName(p))
            ).Distinct()
            .ToList();
        GenericMenu menu = new GenericMenu();
        bool clearBinding = true;
        foreach (string binding in bindings) {
            menu.AddItem(new GUIContent(binding), bindingSelection == binding, () => bindingSelection = binding);
            if (binding.Equals(bindingSelection)) clearBinding = false;
        }
        if (clearBinding) bindingSelection = null;
        if (EditorGUILayout.DropdownButton(new GUIContent(bindingSelection ?? "", "The Shader property to fill with the point"), FocusType.Keyboard))
        {
            Rect rect = EditorGUILayout.GetControlRect(false);
            rect.yMax = rect.yMin + rect.height * bindings.Count;
            menu.DropDown(rect);
        }
        if (bindingSelection == null) EditorGUILayout.HelpBox("No Binding selected", MessageType.Warning);
        else if (GUILayout.Button("Encode")) Encode();
    }

    public void Encode()
    {
        Vector3 pt = avatar.transform.InverseTransformPoint(point.position);
        Keyframe kx = new Keyframe(0, pt.x);
        Keyframe ky = new Keyframe(0, pt.y);
        Keyframe kz = new Keyframe(0, pt.z);
        Undo.RecordObject(clip, "Generate Keyframe");
        foreach (Renderer r in selection)
        {
            string path = AnimationUtility.CalculateTransformPath(r.transform, avatar.transform);
            Type type = r.GetType();
            EditorCurveBinding bindingX = EditorCurveBinding.FloatCurve(path, type, bindingSelection + ".x");
            EditorCurveBinding bindingY = EditorCurveBinding.FloatCurve(path, type, bindingSelection + ".y");
            EditorCurveBinding bindingZ = EditorCurveBinding.FloatCurve(path, type, bindingSelection + ".z");
            AnimationCurve curveX = AnimationUtility.GetEditorCurve(clip, bindingX) ?? new AnimationCurve();
            AnimationCurve curveY = AnimationUtility.GetEditorCurve(clip, bindingY) ?? new AnimationCurve();
            AnimationCurve curveZ = AnimationUtility.GetEditorCurve(clip, bindingZ) ?? new AnimationCurve();
            curveX.AddKey(kx);
            curveY.AddKey(ky);
            curveZ.AddKey(kz);
            AnimationUtility.SetEditorCurve(clip, bindingX, curveX);
            AnimationUtility.SetEditorCurve(clip, bindingY, curveY);
            AnimationUtility.SetEditorCurve(clip, bindingZ, curveZ);
        }
    }
}
