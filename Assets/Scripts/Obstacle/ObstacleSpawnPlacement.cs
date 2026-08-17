using UnityEngine;

[System.Serializable]
public class ObstacleSpawnPlacement
{
    [SerializeField] private Vector2 spawnXOffsetRange = new(6f, 6f);
    [SerializeField] private Vector2 spawnYOffsetRange = new(0f, 0f);
    [SerializeField] private float followDuration = 0f;

    private float spawnYOffset;
    private float followTimer;
    private Vector3 previousPlayerPosition;

    public void PlaceNearPlayer(Transform self, Transform player)
    {
        if (player == null) return;

        float spawnXOffset = Random.Range(spawnXOffsetRange.x, spawnXOffsetRange.y);
        spawnYOffset = Random.Range(spawnYOffsetRange.x, spawnYOffsetRange.y);
        followTimer = followDuration;
        previousPlayerPosition = player.position;

        self.position = new Vector3(
            player.position.x + spawnXOffset,
            player.position.y + spawnYOffset,
            self.position.z);
    }

    public void UpdateFollow(Transform self, Transform player)
    {
        if (player == null) return;

        float deltaX = player.position.x - previousPlayerPosition.x;
        Vector3 pos = self.position;
        pos.x += deltaX;

        if (followTimer > 0f)
        {
            followTimer -= Time.deltaTime;
            pos.y = player.position.y + spawnYOffset;
        }

        self.position = pos;
        previousPlayerPosition = player.position;
    }
}