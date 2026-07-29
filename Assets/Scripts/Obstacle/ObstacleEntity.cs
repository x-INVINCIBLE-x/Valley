using System;
using UnityEngine;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// Base class for obstacles spawned by UniversalObstacleSpawner rather than tied to a specific
    /// platform (lasers, missiles, etc). The spawner assigns <see cref="player"/> and calls
    /// BeginAnticipation() once an instance is placed; from there the obstacle drives its own
    /// anticipation -> action -> recovery lifecycle and calls RequestDespawn() when it's done, which the
    /// spawner listens for to release it back to the pool and free up its slot in the global limit.
    /// </summary>
    public abstract class ObstacleEntity : MonoBehaviour
    {
        [Tooltip("Assigned automatically by the spawner right before BeginAnticipation() is called.")]
        public Transform player;

        public event Action<ObstacleEntity> Despawned;

        /// <summary>Called by the spawner right after this instance is retrieved from the pool and positioned.</summary>
        public abstract void BeginAnticipation();

        protected void RequestDespawn() => Despawned?.Invoke(this);
    }
}