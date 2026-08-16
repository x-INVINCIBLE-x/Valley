using UnityEngine;

namespace Valley.Level.Generation
{
    /// <summary>
    /// Optional parallax scroller for background/foreground decoration. Drop it on any object (a
    /// background sprite, a cloud layer, etc.) and point Reference Object at the same transform your
    /// PlatformSequenceSpawner uses (usually the player/camera). Each frame the layer follows a fraction
    /// - or multiple - of the reference object's movement, controlled per-axis by Parallax Speed:
    /// 0 = stays put, 1 = matches the reference exactly (no visible parallax), less than 1 = drifts
    /// behind (typical background layer), greater than 1 = drifts ahead (foreground layer). Negative
    /// values scroll opposite the reference. Completely independent of PlatformSequenceSpawner - add it
    /// only to the objects you want the effect on.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Reference")]
        [Tooltip("The object whose movement drives the parallax - typically the same reference object used by PlatformSequenceSpawner.")]
        public Transform referenceObject;

        [Header("Parallax Speed")]
        [Tooltip("Multiplier applied to the reference object's per-axis movement each frame. 0 = static, 1 = moves in lockstep (no parallax), <1 = background (drifts behind), >1 = foreground (drifts ahead), negative = scrolls opposite the reference.")]
        public Vector3 parallaxSpeed = new Vector3(0.5f, 0f, 0f);

        [Header("Looping (optional)")]
        [Tooltip("Treats this layer as one tile in a repeating strip along Loop Axis, snapping it forward/back by Loop Width once the reference scrolls that far past it - gives an endless tiling background when a few copies of this component are placed evenly across Loop Width.")]
        public bool loop = false;
        public StreamAxis loopAxis = StreamAxis.X;
        [Tooltip("World-space span one full wrap covers - typically the combined width of all tiled copies. Only used when Loop is enabled.")]
        [Min(0.01f)] public float loopWidth = 20f;

        Vector3 lastReferencePosition;
        bool initialized;

        void OnEnable()
        {
            initialized = false;
        }

        void LateUpdate()
        {
            if (referenceObject == null) return;

            if (!initialized)
            {
                lastReferencePosition = referenceObject.position;
                initialized = true;
                return;
            }

            Vector3 delta = referenceObject.position - lastReferencePosition;
            transform.position += Vector3.Scale(delta, parallaxSpeed);
            lastReferencePosition = referenceObject.position;

            if (loop) HandleLoop();
        }

        void HandleLoop()
        {
            float layerPos = GetAxisValue(transform.position, loopAxis);
            float refPos = GetAxisValue(referenceObject.position, loopAxis);
            float behindBy = refPos - layerPos;

            if (behindBy > loopWidth)
            {
                Shift(loopAxis, Mathf.Floor(behindBy / loopWidth) * loopWidth);
            }
            else if (-behindBy > loopWidth)
            {
                Shift(loopAxis, -Mathf.Floor(-behindBy / loopWidth) * loopWidth);
            }
        }

        void Shift(StreamAxis axis, float amount)
        {
            Vector3 pos = transform.position;
            switch (axis)
            {
                case StreamAxis.X: pos.x += amount; break;
                case StreamAxis.Y: pos.y += amount; break;
                default: pos.z += amount; break;
            }
            transform.position = pos;
        }

        float GetAxisValue(Vector3 v, StreamAxis axis)
        {
            switch (axis)
            {
                case StreamAxis.X: return v.x;
                case StreamAxis.Y: return v.y;
                default: return v.z;
            }
        }
    }
}
