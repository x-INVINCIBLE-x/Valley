using UnityEngine;

namespace Valley.Level.Generation
{
    /// <summary>
    /// Optional companion component for PlatformBlock - add it to the same GameObject as PlatformBlock.
    /// Its custom editor (PlatformBlockAnchorToolEditor) adds an "Auto-Detect Anchors" button that
    /// creates/repositions PlatformBlock's leftAnchor/rightAnchor/surfaceAnchor as real child Transforms
    /// sitting on the current boundary box. PlatformBlock.cs itself is never modified - this only reads
    /// its public boundsCenter/boundsSize and writes back to its public anchor fields.
    /// </summary>
    [RequireComponent(typeof(PlatformBlock))]
    [DisallowMultipleComponent]
    public class PlatformBlockAnchorTool : MonoBehaviour
    {
        PlatformBlock block;
        public PlatformBlock Block => block != null ? block : (block = GetComponent<PlatformBlock>());

        /// <summary>
        /// Creates (or repositions, if they already exist) Block.leftAnchor/rightAnchor/surfaceAnchor as
        /// child Transforms sitting exactly on the current boundary box's left edge, right edge and top
        /// center. Because they're ordinary child Transforms, manual adjustment afterwards is saved the
        /// normal Unity way (scene save / Apply to Prefab) - no extra step needed.
        /// </summary>
        public void AutoDetectAnchors()
        {
            PlatformBlock b = Block;
            b.leftAnchor = EnsureAnchor(b.leftAnchor, "LeftAnchor",
                b.boundsCenter + new Vector3(-b.boundsSize.x * 0.5f, b.boundsSize.y * 0.5f, 0f));
            b.rightAnchor = EnsureAnchor(b.rightAnchor, "RightAnchor",
                b.boundsCenter + new Vector3(b.boundsSize.x * 0.5f, b.boundsSize.y * 0.5f, 0f));
            b.surfaceAnchor = EnsureAnchor(b.surfaceAnchor, "SurfaceAnchor",
                b.boundsCenter + new Vector3(0f, b.boundsSize.y * 0.5f, 0f));
        }

        Transform EnsureAnchor(Transform existing, string childName, Vector3 localPosition)
        {
            Transform anchor = existing;
            if (anchor == null)
            {
                anchor = new GameObject(childName).transform;
                anchor.SetParent(transform, false);
            }
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            return anchor;
        }
    }
}