using UnityEngine;
using Valley.Combat;
using Valley.Player;

public class PlayerContext : MonoBehaviour
{
    public static PlayerContext Instance;
    [field: SerializeField] 
    public Health PlayerHealth { get; private set; }

    private PlayerContext _instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
