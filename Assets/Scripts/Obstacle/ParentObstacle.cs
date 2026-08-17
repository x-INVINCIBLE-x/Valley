// ParentObstacle.cs
using UnityEngine;

namespace Valley.Level.Obstacles
{
    public class ParentObstacle : ObstacleEntity
    {
        [SerializeField] private ObstacleEntity[] obstacles;
        [SerializeField] private ObstacleSpawnPlacement placement = new();

        private int remainingChildren;

        public override void BeginAnticipation()
        {
            placement.PlaceNearPlayer(transform, player);

            remainingChildren = obstacles.Length;

            for (int i = 0; i < obstacles.Length; i++)
            {
                ObstacleEntity obstacle = obstacles[i];
                obstacle.player = player;
                obstacle.IsPositionRoot = false; // parent owns the group's position

                obstacle.Despawned -= HandleChildDespawned;
                obstacle.Despawned += HandleChildDespawned;

                obstacle.BeginAnticipation();
            }
        }

        private void Update()
        {
            placement.UpdateFollow(transform, player);
        }

        private void HandleChildDespawned(ObstacleEntity child)
        {
            child.Despawned -= HandleChildDespawned;
            remainingChildren--;

            if (remainingChildren <= 0)
                RequestDespawn();
        }
    }
}