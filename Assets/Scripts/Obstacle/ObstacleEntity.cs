using System;
using UnityEngine;

public abstract class ObstacleEntity : MonoBehaviour
{
    [HideInInspector] public Transform player;

    [Tooltip("True if this instance places/follows itself relative to the player. " +
             "A ParentObstacle sets this false on its children so only the parent moves " +
             "and children ride along as ordinary child transforms.")]
    [HideInInspector] public bool IsPositionRoot = true;

    public event Action<ObstacleEntity> Despawned;

    public abstract void BeginAnticipation();
    protected void RequestDespawn() => Despawned?.Invoke(this);
}