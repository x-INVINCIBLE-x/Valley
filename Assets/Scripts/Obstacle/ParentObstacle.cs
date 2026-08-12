using UnityEngine;
using Valley.Level.Obstacles;

public class ParentObstacle : ObstacleEntity
{
    [SerializeField] private ObstacleEntity[] obstacles;

    public override void BeginAnticipation()
    {
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].player = player;
            obstacles[i].BeginAnticipation();
        }
    }
}
