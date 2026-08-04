using UnityEngine;

public class Move : MonoBehaviour
{
    [SerializeField] private Vector3 velocity;

    private void Update()
    {
        transform.Translate(velocity * Time.deltaTime);
    }
}