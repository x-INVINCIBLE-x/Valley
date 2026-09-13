using UnityEngine;

public class MaterialVelocityScroll : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float multiplier = 0.1f;

    private Material material;
    private Vector2 offset;

    private void Awake()
    {
        material = targetRenderer.material;
    }

    private void Update()
    {
        Vector3 velocity = rb.linearVelocity;

        offset.x += velocity.x * multiplier * Time.deltaTime;
        offset.y += velocity.z * multiplier * Time.deltaTime;

        material.mainTextureOffset = offset;
    }
}