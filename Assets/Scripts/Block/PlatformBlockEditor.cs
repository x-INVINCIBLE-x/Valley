using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Valley.Level.Generation.EditorTools
{
    [CustomEditor(typeof(PlatformBlock))]
    public class PlatformBlockEditor : Editor
    {
        readonly BoxBoundsHandle boundsHandle = new BoxBoundsHandle();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var block = (PlatformBlock)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Auto-Detect Bounds From Renderers"))
            {
                Undo.RecordObject(block, "Auto-Detect Platform Bounds");
                block.RecalculateBoundsFromRenderers();
                EditorUtility.SetDirty(block);
            }
            EditorGUILayout.HelpBox(
                "Auto-Detect sets a starting box from this platform's child renderers. Drag the cyan " +
                "handles in the Scene view to fine-tune it afterwards.",
                MessageType.Info);
        }

        void OnSceneGUI()
        {
            var block = (PlatformBlock)target;

            boundsHandle.center = block.boundsCenter;
            boundsHandle.size = block.boundsSize;

            using (new Handles.DrawingScope(Matrix4x4.TRS(block.transform.position, block.transform.rotation, Vector3.one)))
            {
                EditorGUI.BeginChangeCheck();
                boundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(block, "Adjust Platform Bounds");
                    block.boundsCenter = boundsHandle.center;
                    block.boundsSize = boundsHandle.size;
                    EditorUtility.SetDirty(block);
                }
            }
        }
    }
}