using UnityEngine;

namespace Valley.Level.Spawning
{
    /// <summary>
    /// Base class for anything a PlatformSpawnPointGenerator can spawn. Subclass it directly for
    /// object-specific behavior, or leave it as-is and pair the prefab with a SpawnTrigger for
    /// activation - whichever fits a given prefab; neither is required for a spawn point to work.
    /// </summary>
    public class SpawnedEntity : MonoBehaviour
    {
        /// <summary>Called right after this instance is placed and passes the overlap check.</summary>
        public virtual void OnSpawned() { }

        /// <summary>Called right before this instance is released back to the pool.</summary>
        public virtual void OnDespawned() { }

        /// <summary>Called by a SpawnTrigger (or anything else) to arm/activate this entity.</summary>
        public virtual void Activate() { }
    }
}
