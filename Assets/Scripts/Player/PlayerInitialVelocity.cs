using UnityEngine;

namespace Valley.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerInitialVelocity : MonoBehaviour
    {
        [SerializeField] private float initialSpeed = 5f;

        private void Start()
        {
            GetComponent<Rigidbody>().linearVelocity = Vector3.right * initialSpeed;
        }
    }
}
