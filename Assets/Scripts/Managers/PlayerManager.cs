using UnityEngine;

namespace Valley
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        [SerializeField] private Transform playerTransform;

        public Transform PlayerTransform => playerTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RegisterPlayer(Transform player)
        {
            playerTransform = player;
        }

        public void UnregisterPlayer(Transform player)
        {
            if (playerTransform == player)
                playerTransform = null;
        }
    }
}