using UnityEngine;

namespace Valley.Level.Spawning
{
    /// <summary>
    /// Optional companion component: fires Activate() on a SpawnedEntity when something on the
    /// configured layers enters this trigger collider. Use this when activation logic should live
    /// outside the spawned prefab itself; if the prefab handles its own activation, subclass
    /// SpawnedEntity directly instead and skip this component entirely.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpawnTrigger : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Entity to activate. Defaults to a SpawnedEntity found on this object or its parent if left empty.")]
        public SpawnedEntity target;

        [Header("Filter")]
        [Tooltip("Only colliders on these layers can activate this trigger.")]
        public LayerMask activatingLayers = ~0;

        void Awake()
        {
            if (target == null) target = GetComponentInParent<SpawnedEntity>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (target == null) return;
            if ((activatingLayers.value & (1 << other.gameObject.layer)) == 0) return;
            target.Activate();
        }
    }
}
