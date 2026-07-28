using UnityEditor;
using UnityEngine;

namespace Valley.Level.Spawning.EditorTools
{
    /// <summary>
    /// Custom inspector + Scene-view handles for PlatformSpawnPointGenerator.
    /// IMPORTANT: place this script inside a folder named "Editor" anywhere under Assets
    /// (e.g. Assets/Scripts/Level/Editor/) so Unity excludes it from player builds.
    /// </summary>
    [CustomEditor(typeof(PlatformSpawnPointGenerator))]
    public class PlatformSpawnPointGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var generator = (PlatformSpawnPointGenerator)target;
            EditorGUILayout.Space();

            if (GUILayout.Button("Auto-Fill Anchors From PlatformBlock"))
            {
                Undo.RecordObject(generator, "Auto-Fill Spawn Anchors");
                generator.AutoFillAnchorsFromPlatformBlock();
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Auto-Generate Spawn Points"))
            {
                Undo.RecordObject(generator, "Auto-Generate Spawn Points");
                generator.AutoGenerateSpawnPoints();
                EditorUtility.SetDirty(generator);
            }

            EditorGUILayout.HelpBox(
                "Auto-Generate lays out each category's points evenly between the two anchors, with jitter/Z applied. " +
                "Drag the handles in the Scene view, or edit a category's Points list above, to adjust individual points afterward.",
                MessageType.Info);
        }

        void OnSceneGUI()
        {
            var generator = (PlatformSpawnPointGenerator)target;
            if (generator.categories == null) return;

            foreach (var category in generator.categories)
            {
                if (category?.points == null) continue;

                Handles.color = category.gizmoColor;
                foreach (var point in category.points)
                {
                    Vector3 worldPos = generator.transform.TransformPoint(point.localPosition);

                    EditorGUI.BeginChangeCheck();
                    Vector3 moved = Handles.PositionHandle(worldPos, generator.transform.rotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(generator, "Move Spawn Point");
                        point.localPosition = generator.transform.InverseTransformPoint(moved);
                        EditorUtility.SetDirty(generator);
                    }

                    Handles.Label(worldPos, category.categoryName);
                }
            }
        }
    }
}
