#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Valley.Level.Generation.EditorTools
{
    [CustomEditor(typeof(PlatformBlockAnchorTool))]
    public class PlatformBlockAnchorToolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var tool = (PlatformBlockAnchorTool)target;
            var block = tool.Block;
            EditorGUILayout.Space();

            if (GUILayout.Button("Auto-Detect Anchors"))
            {
                // Snapshot which anchors already existed so Undo knows whether an anchor was newly
                // created (needs RegisterCreatedObjectUndo) or just moved (needs RecordObject).
                Transform prevLeft = block.leftAnchor;
                Transform prevRight = block.rightAnchor;
                Transform prevSurface = block.surfaceAnchor;

                Undo.RecordObject(block, "Auto-Detect Platform Anchors");
                if (prevLeft != null) Undo.RecordObject(prevLeft, "Auto-Detect Platform Anchors");
                if (prevRight != null) Undo.RecordObject(prevRight, "Auto-Detect Platform Anchors");
                if (prevSurface != null) Undo.RecordObject(prevSurface, "Auto-Detect Platform Anchors");

                tool.AutoDetectAnchors();

                if (prevLeft == null && block.leftAnchor != null)
                    Undo.RegisterCreatedObjectUndo(block.leftAnchor.gameObject, "Auto-Detect Platform Anchors");
                if (prevRight == null && block.rightAnchor != null)
                    Undo.RegisterCreatedObjectUndo(block.rightAnchor.gameObject, "Auto-Detect Platform Anchors");
                if (prevSurface == null && block.surfaceAnchor != null)
                    Undo.RegisterCreatedObjectUndo(block.surfaceAnchor.gameObject, "Auto-Detect Platform Anchors");

                EditorUtility.SetDirty(block);
            }

            EditorGUILayout.HelpBox(
                "Auto-Detect creates/repositions LeftAnchor, RightAnchor and SurfaceAnchor as child " +
                "objects on PlatformBlock's current boundary box. Drag them with the position handles " +
                "below (or pick the child objects directly) to fine-tune - they're saved the normal " +
                "Unity way, no extra step needed.",
                MessageType.Info);
        }

        void OnSceneGUI()
        {
            var tool = (PlatformBlockAnchorTool)target;
            var block = tool.Block;
            if (block == null) return;

            DrawAnchorHandle(block.leftAnchor, "Left Anchor");
            DrawAnchorHandle(block.rightAnchor, "Right Anchor");
            DrawAnchorHandle(block.surfaceAnchor, "Surface Anchor");
        }

        void DrawAnchorHandle(Transform anchor, string undoLabel)
        {
            if (anchor == null) return;

            EditorGUI.BeginChangeCheck();
            Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? anchor.rotation : Quaternion.identity;
            Vector3 newPosition = Handles.PositionHandle(anchor.position, handleRotation);
            Handles.Label(anchor.position + Vector3.up * 0.15f, anchor.name);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(anchor, "Adjust " + undoLabel);
                anchor.position = newPosition;
                EditorUtility.SetDirty(anchor);
            }
        }
    }
}
#endif