#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Valley.Level.Generation.Editor
{
    /// <summary>
    /// Play-mode testing window for PlatformChunkSpawner. Pick a spawner in the scene, then:
    ///  - apply any PlatformGenerationProfile to it with one click (no need to wire up a
    ///    PlatformProgressionStage just to try a data set),
    ///  - queue a premade level to be inserted as the very next mid-layer platform,
    ///  - push a runtime weight override onto a specific prefab,
    /// all while watching CurrentDistance and progression state update live.
    ///
    /// This is a manual, on-demand complement to PlatformChunkSpawner.testProfile (which keeps a single
    /// assigned profile continuously re-applied as you edit it) - use whichever fits how you like to test.
    ///
    /// Must live under a folder named "Editor" (anywhere under Assets) per Unity convention, though the
    /// #if UNITY_EDITOR guard alone is enough to keep it out of player builds either way.
    /// </summary>
    public class PlatformChunkSpawnerTesterWindow : EditorWindow
    {
        PlatformChunkSpawner spawner;
        PlatformGenerationProfile profileToApply;
        PlatformBlock premadeLevelToQueue;
        PlatformBlock prefabToWeight;
        float weightToSet = 1f;
        Vector2 scroll;

        [MenuItem("Window/Valley/Platform Chunk Spawner Tester")]
        static void Open() => GetWindow<PlatformChunkSpawnerTesterWindow>("Chunk Spawner Tester");

        // Live fields (CurrentDistance etc.) only change while the game is running, so keep repainting
        // while this window is open rather than only on the usual GUI-event cadence.
        void OnEnable() => EditorApplication.update += Repaint;
        void OnDisable() => EditorApplication.update -= Repaint;

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.HelpBox(
                "Actions below only do anything in Play Mode. Assign a spawner, then apply profiles, " +
                "queue a premade level, or push a weight override and watch generation react live.",
                MessageType.Info);

            EditorGUILayout.Space();
            spawner = (PlatformChunkSpawner)EditorGUILayout.ObjectField("Spawner", spawner, typeof(PlatformChunkSpawner), true);
            // FindObjectOfType for broad Unity version compatibility - swap for FindFirstObjectByType/
            // FindAnyObjectByType if your project is on Unity 2023.1+ and you'd rather avoid the warning.
            if (spawner == null && GUILayout.Button("Find In Scene"))
                spawner = FindObjectOfType<PlatformChunkSpawner>();

            if (spawner == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawLiveState();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                DrawApplyProfileSection();
                EditorGUILayout.Space();
                DrawPremadeLevelSection();
                EditorGUILayout.Space();
                DrawWeightOverrideSection();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to use the controls above.", MessageType.Warning);

            EditorGUILayout.EndScrollView();
        }

        void DrawLiveState()
        {
            EditorGUILayout.LabelField("Live State", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Current Distance", Application.isPlaying ? spawner.CurrentDistance.ToString("F1") : "-");

                string stageLabel = "-";
                if (Application.isPlaying && spawner.progressionStages != null)
                {
                    int next = spawner.NextProgressionStageIndex;
                    stageLabel = next >= spawner.progressionStages.Length
                        ? $"{next} / {spawner.progressionStages.Length} (all triggered)"
                        : $"{next} / {spawner.progressionStages.Length} (next at {spawner.progressionStages[next].distanceThreshold})";
                }
                EditorGUILayout.TextField("Progression Stage", stageLabel);

                EditorGUILayout.TextField("Mid Prefab Pool Size", spawner.platformPrefabs != null ? spawner.platformPrefabs.Length.ToString() : "0");
            }
        }

        void DrawApplyProfileSection()
        {
            EditorGUILayout.LabelField("Apply A Profile", EditorStyles.boldLabel);
            profileToApply = (PlatformGenerationProfile)EditorGUILayout.ObjectField("Profile", profileToApply, typeof(PlatformGenerationProfile), false);
            using (new EditorGUI.DisabledScope(profileToApply == null))
            {
                if (GUILayout.Button("Apply Now"))
                    spawner.ApplyProfile(profileToApply);
            }
        }

        void DrawPremadeLevelSection()
        {
            EditorGUILayout.LabelField("Queue A Premade Level", EditorStyles.boldLabel);
            premadeLevelToQueue = (PlatformBlock)EditorGUILayout.ObjectField("Premade Block", premadeLevelToQueue, typeof(PlatformBlock), false);
            using (new EditorGUI.DisabledScope(premadeLevelToQueue == null))
            {
                if (GUILayout.Button("Queue As Next Mid Platform"))
                    spawner.QueuePremadeLevel(premadeLevelToQueue);
            }
        }

        void DrawWeightOverrideSection()
        {
            EditorGUILayout.LabelField("Prefab Weight Override", EditorStyles.boldLabel);
            prefabToWeight = (PlatformBlock)EditorGUILayout.ObjectField("Prefab", prefabToWeight, typeof(PlatformBlock), false);
            weightToSet = EditorGUILayout.Slider("Weight", weightToSet, 0f, 10f);

            using (new EditorGUI.DisabledScope(prefabToWeight == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Set"))
                    spawner.SetPrefabWeight(prefabToWeight, weightToSet);
                if (GUILayout.Button("Clear Override"))
                    spawner.ClearPrefabWeight(prefabToWeight);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif