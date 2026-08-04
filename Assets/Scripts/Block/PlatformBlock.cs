using UnityEngine;

namespace Valley.Level.Generation
{
    /// <summary>
    /// Authoring component for a single platform prefab. Defines its boundary box (auto-detected or
    /// hand-adjusted via the custom inspector/gizmos), whether it is allowed to flush-attach to a
    /// neighboring block on each side and how often that succeeds, and an optional rotation clamp.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlatformBlock : MonoBehaviour
    {
        [Header("Boundary")]
        [Tooltip("Local-space center of the platform's boundary box, relative to this transform.")]
        public Vector3 boundsCenter = Vector3.zero;

        [Tooltip("Local-space size of the platform's boundary box.")]
        public Vector3 boundsSize = new Vector3(2f, 0.5f, 1f);

        [Header("Optional Precise Anchors")]
        [Tooltip("Overrides the boundary box's top-left corner as the flush-attach / edge point. Leave empty to use the box.")]
        public Transform leftAnchor;
        [Tooltip("Overrides the boundary box's top-right corner as the flush-attach / edge point. Leave empty to use the box.")]
        public Transform rightAnchor;
        [Tooltip("Overrides the boundary box's top-center as the landing surface reference. Leave empty to use the box.")]
        public Transform surfaceAnchor;

        [Header("Attachment")]
        [Tooltip("Whether another block may flush-attach to this block's LEFT edge, and how often that attempt succeeds.")]
        public AttachSettings leftAttach = new AttachSettings { allowed = true, successRate = 0.5f };
        [Tooltip("Whether another block may flush-attach to this block's RIGHT edge, and how often that attempt succeeds.")]
        public AttachSettings rightAttach = new AttachSettings { allowed = true, successRate = 0.5f };

        [Header("Rotation")]
        [Tooltip("If disabled, this block always spawns unrotated. If enabled, spawn rotation (Z axis) is randomized within the min/max clamp.")]
        public RotationClamp rotation = new RotationClamp { allowRotation = false, minAngleDegrees = -15f, maxAngleDegrees = 15f };

        [Header("Spawn Weighting")]
        [Tooltip("Relative chance this prefab is picked among the spawner's platform list.")]
        [Range(0.01f, 10f)] public float spawnWeight = 1f;

        [Tooltip("Independent chance (0-1) that this platform actually spawns once picked. Unlike spawnWeight, which only affects likelihood relative to other candidates, this can make it not spawn at all - the space it would have taken becomes extra gap instead. Meant to be driven by a score-threshold controller for gating rarer content.")]
        [Range(0f, 1f)] public float spawnChance = 1f;

        [System.Serializable]
        public struct AttachSettings
        {
            public bool allowed;
            [Range(0f, 1f)] public float successRate;
        }

        [System.Serializable]
        public struct RotationClamp
        {
            public bool allowRotation;
            public float minAngleDegrees;
            public float maxAngleDegrees;
        }

        public float Width => boundsSize.x;

        /// <summary>
        /// Recomputes boundsCenter/boundsSize (in this transform's local space) from every Renderer
        /// underneath it. Hooked up to the "Auto-Detect Bounds" button in the custom inspector - run it
        /// once to get a starting box, then drag the Scene-view handles to adjust it by hand.
        /// </summary>
        public void RecalculateBoundsFromRenderers()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            boundsCenter = transform.InverseTransformPoint(worldBounds.center);
            boundsSize = new Vector3(
                worldBounds.size.x / Mathf.Max(transform.lossyScale.x, 0.0001f),
                worldBounds.size.y / Mathf.Max(transform.lossyScale.y, 0.0001f),
                worldBounds.size.z / Mathf.Max(transform.lossyScale.z, 0.0001f));
        }

        public Vector3 GetLeftEdgeWorld() =>
            leftAnchor != null
                ? leftAnchor.position
                : transform.TransformPoint(boundsCenter + new Vector3(-boundsSize.x * 0.5f, boundsSize.y * 0.5f, 0f));

        public Vector3 GetRightEdgeWorld() =>
            rightAnchor != null
                ? rightAnchor.position
                : transform.TransformPoint(boundsCenter + new Vector3(boundsSize.x * 0.5f, boundsSize.y * 0.5f, 0f));

        public Vector3 GetSurfaceWorld() =>
            surfaceAnchor != null
                ? surfaceAnchor.position
                : transform.TransformPoint(boundsCenter + new Vector3(0f, boundsSize.y * 0.5f, 0f));

        void OnDrawGizmos()
        {
            Color prevColor = Gizmos.color;
            Matrix4x4 prevMatrix = Gizmos.matrix;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);

            Gizmos.matrix = prevMatrix;
            Gizmos.color = leftAttach.allowed ? Color.green : Color.red;
            Gizmos.DrawSphere(GetLeftEdgeWorld(), 0.08f);
            Gizmos.color = rightAttach.allowed ? Color.green : Color.red;
            Gizmos.DrawSphere(GetRightEdgeWorld(), 0.08f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(GetSurfaceWorld(), 0.08f);

            Gizmos.color = prevColor;
        }
    }
}